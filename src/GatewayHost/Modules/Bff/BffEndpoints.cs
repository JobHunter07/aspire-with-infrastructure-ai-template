using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GatewayHost.Modules.Bff;

// Production-ready BFF endpoints: server-side code exchange with Keycloak, id_token validation,
// server-side session storage in IDistributedCache, secure HTTP-only session cookie.
public static class BffEndpoints
{
    private const string SessionCookieName = "bff_session";
    private const string StateCachePrefix = "bff_state:";
    private const string SessionCachePrefix = "bff_session:";

    // Configuration keys
    private const string ConfigKey_KeycloakRealm = "Bff:Keycloak:RealmBase"; // e.g. https://localhost:8080/realms/aspire-template-dev
    private const string ConfigKey_ClientId = "Bff:Keycloak:ClientId";
    private const string ConfigKey_ClientSecret = "Bff:Keycloak:ClientSecret";

    public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Returns user info when authenticated, 401 when not
        endpoints.MapGet("/bff/user", async (HttpContext http) =>
        {
            var cache = http.RequestServices.GetRequiredService<IDistributedCache>();

            if (!http.Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) || string.IsNullOrEmpty(sessionId))
            {
                return Results.Unauthorized();
            }

            var cacheKey = SessionCachePrefix + sessionId;
            var sessionJson = await cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(sessionJson))
            {
                return Results.Unauthorized();
            }

            var session = JsonSerializer.Deserialize<SessionData>(sessionJson);
            if (session == null)
            {
                return Results.Unauthorized();
            }

            var user = new BffUserResponse(session.Username ?? string.Empty, session.Email ?? string.Empty);
            return Results.Ok(user);
        })
        .WithName("BffGetUser")
        .WithTags("BFF");

        // Initiates login: generate state+nonce, store in cache, redirect to Keycloak OIDC auth endpoint.
        endpoints.MapGet("/bff/login", async (HttpContext http) =>
        {
            var cfg = http.RequestServices.GetRequiredService<IConfiguration>();
            var cache = http.RequestServices.GetRequiredService<IDistributedCache>();

            var req = http.Request;
            var qs = req.Query;
            var returnUrl = qs.TryGetValue("returnUrl", out var rv) ? rv.ToString() : "/";

            var realmBase = $"{cfg["KEYCLOAK_HTTPS"]}/{cfg[ConfigKey_KeycloakRealm]}";
            var clientId = cfg[ConfigKey_ClientId];
            if (string.IsNullOrEmpty(realmBase) || string.IsNullOrEmpty(clientId))
            {
                return Results.Problem("Keycloak configuration not set on server.", statusCode: 500);
            }

            var scheme = req.Scheme;
            var host = req.Host.Value;
            var callbackUrl = $"{scheme}://{host}/bff/callback";

            var state = Guid.NewGuid().ToString("N");
            var nonce = Guid.NewGuid().ToString("N");

            // PKCE: generate code_verifier and code_challenge so Keycloak clients that require PKCE work.
            var codeVerifierBytes = RandomNumberGenerator.GetBytes(32);
            string Base64UrlEncode(byte[] input) => Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var codeVerifier = Base64UrlEncode(codeVerifierBytes);
            using var sha = SHA256.Create();
            var challengeBytes = sha.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            var codeChallenge = Base64UrlEncode(challengeBytes);

            var stateObj = new StateData { Nonce = nonce, ReturnUrl = returnUrl, CodeVerifier = codeVerifier };
            var stateCacheKey = StateCachePrefix + state;
            var stateJson = JsonSerializer.Serialize(stateObj);
            await cache.SetStringAsync(stateCacheKey, stateJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            var authUrl = new StringBuilder();
            authUrl.Append(realmBase.TrimEnd('/'))
                   .Append("/protocol/openid-connect/auth")
                   .Append("?client_id=").Append(Uri.EscapeDataString(clientId))
                   .Append("&response_type=code")
                   .Append("&scope=openid profile email")
                   .Append("&redirect_uri=").Append(Uri.EscapeDataString(callbackUrl))
                   .Append("&kc_idp_hint=google")
                   .Append("&state=").Append(Uri.EscapeDataString(state))
                   .Append("&nonce=").Append(Uri.EscapeDataString(nonce))
                   .Append("&code_challenge=").Append(Uri.EscapeDataString(codeChallenge))
                   .Append("&code_challenge_method=S256");

            return Results.Redirect(authUrl.ToString());
        })
        .WithName("BffLogin")
        .WithTags("BFF");

        // Callback: validate state, exchange code for tokens, validate id_token, create server-side session, set cookie.
        endpoints.MapGet("/bff/callback", async (HttpContext http) =>
        {
            var cfg = http.RequestServices.GetRequiredService<IConfiguration>();
            var cache = http.RequestServices.GetRequiredService<IDistributedCache>();
            var clientFactory = http.RequestServices.GetRequiredService<IHttpClientFactory>();

            var req = http.Request;
            var qs = req.Query;

            var incomingState = qs.TryGetValue("state", out var s) ? s.ToString() : null;
            var code = qs.TryGetValue("code", out var c) ? c.ToString() : null;

            if (string.IsNullOrEmpty(incomingState) || string.IsNullOrEmpty(code))
            {
                return Results.BadRequest("Missing state or code parameter in callback.");
            }

            var stateCacheKey = StateCachePrefix + incomingState;
            var stateJson = await cache.GetStringAsync(stateCacheKey);
            if (string.IsNullOrEmpty(stateJson))
            {
                return Results.BadRequest("Invalid or expired state.");
            }

            var stateObj = JsonSerializer.Deserialize<StateData>(stateJson);
            if (stateObj == null)
            {
                return Results.BadRequest("Invalid state payload.");
            }

            var realmBase = $"{cfg["KEYCLOAK_HTTPS"]}/{cfg[ConfigKey_KeycloakRealm]}";
            var clientId = cfg[ConfigKey_ClientId];
            var clientSecret = cfg[ConfigKey_ClientSecret];
            if (string.IsNullOrEmpty(realmBase) || string.IsNullOrEmpty(clientId))
            {
                return Results.Problem("Keycloak configuration not set on server.", statusCode: 500);
            }

            var tokenEndpoint = realmBase.TrimEnd('/') + "/protocol/openid-connect/token";
            var httpClient = clientFactory.CreateClient();
            var tokenRequestPairs = new List<KeyValuePair<string,string>>
            {
                new KeyValuePair<string,string>("grant_type","authorization_code"),
                new KeyValuePair<string,string>("code", code),
                new KeyValuePair<string,string>("redirect_uri", $"{req.Scheme}://{req.Host}/bff/callback"),
                new KeyValuePair<string,string>("client_id", clientId),
            };

            // Include client_secret when configured (confidential clients). Include PKCE code_verifier when present.
            if (!string.IsNullOrEmpty(clientSecret))
            {
                tokenRequestPairs.Add(new KeyValuePair<string,string>("client_secret", clientSecret));
            }

            if (!string.IsNullOrEmpty(stateObj.CodeVerifier))
            {
                tokenRequestPairs.Add(new KeyValuePair<string,string>("code_verifier", stateObj.CodeVerifier));
            }

            var tokenRequest = new FormUrlEncodedContent(tokenRequestPairs);

            var tokenResp = await httpClient.PostAsync(tokenEndpoint, tokenRequest);
            if (!tokenResp.IsSuccessStatusCode)
            {
                var body = await tokenResp.Content.ReadAsStringAsync();
                return Results.Problem($"Token endpoint returned error: {tokenResp.StatusCode} - {body}", statusCode: 500);
            }

            var tokenJson = await tokenResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(tokenJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("id_token", out var idTokenEl))
            {
                return Results.Problem("No id_token in token response", statusCode: 500);
            }

            var idToken = idTokenEl.GetString();

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                realmBase.TrimEnd('/') + "/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());

            var oidcConfig = await configManager.GetConfigurationAsync(CancellationToken.None);

            var tvp = new TokenValidationParameters
            {
                ValidIssuer = oidcConfig.Issuer,
                ValidAudiences = new[] { clientId },
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var principal = handler.ValidateToken(idToken, tvp, out var validatedToken);

                var nonceClaim = principal.FindFirst("nonce")?.Value;
                if (nonceClaim != stateObj.Nonce)
                {
                    return Results.BadRequest("Invalid nonce in id_token.");
                }

                var sessionId = Guid.NewGuid().ToString("N");
                var username = principal.FindFirst("preferred_username")?.Value ?? principal.FindFirst("email")?.Value;
                var email = principal.FindFirst("email")?.Value;
                var accessToken = root.GetProperty("access_token").GetString();
                var refreshToken = root.TryGetProperty("refresh_token", out var r) ? r.GetString() : null;

                var session = new SessionData
                {
                    Username = username,
                    Email = email,
                    IdToken = idToken,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                var sessionKey = SessionCachePrefix + sessionId;
                var sessionJson = JsonSerializer.Serialize(session);
                await cache.SetStringAsync(sessionKey, sessionJson, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
                });

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = session.ExpiresAt
                };
                http.Response.Cookies.Append(SessionCookieName, sessionId, cookieOptions);

                await cache.RemoveAsync(stateCacheKey);

                return Results.Redirect(stateObj.ReturnUrl ?? "/");
            }
            catch (SecurityTokenValidationException ex)
            {
                return Results.Problem($"Token validation failed: {ex.Message}", statusCode: 500);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Unexpected error during token processing: {ex.Message}", statusCode: 500);
            }
        })
        .WithName("BffCallback")
        .WithTags("BFF");

        // Logout: remove server-side session and clear cookie
        endpoints.MapPost("/bff/logout", async (HttpContext http) =>
        {
            var cache = http.RequestServices.GetRequiredService<IDistributedCache>();

            if (http.Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) && !string.IsNullOrEmpty(sessionId))
            {
                var sessionKey = SessionCachePrefix + sessionId;
                await cache.RemoveAsync(sessionKey);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(-1)
                };

                http.Response.Cookies.Append(SessionCookieName, string.Empty, cookieOptions);
            }

            return Results.Ok();
        })
        .WithName("BffLogout")
        .WithTags("BFF");

        return endpoints;
    }

    private record StateData
    {
        public string? Nonce { get; init; }
        public string? ReturnUrl { get; init; }
        public string? CodeVerifier { get; init; }
    }

    private record SessionData
    {
        public string? Username { get; init; }
        public string? Email { get; init; }
        public string? IdToken { get; init; }
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
    }
}

public record BffUserResponse(string Username, string Email);

