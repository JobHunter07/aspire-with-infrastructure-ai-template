namespace GatewayHost.Modules.Bff;

public record BffUserResponse(string Username, string Email);

public static class BffEndpoints
{
        private const string CookieName = "bff_auth";
        private const string KeycloakGoogleEndpoint = "https://localhost:60566/realms/aspire-template-dev/broker/google/endpoint";

        public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder endpoints)
        {
            // Returns user info when authenticated, 401 when not
            endpoints.MapGet("/bff/user", (HttpContext http) =>
            {
                if (http.Request.Cookies.TryGetValue(CookieName, out var val) && !string.IsNullOrEmpty(val))
                {
                    // Minimal demo user. In a real implementation, validate server-side session and return real claims.
                    var user = new BffUserResponse("demo.user", "demo.user@example.com");
                    return Results.Ok(user);
                }

                return Results.Unauthorized();
            })
            .WithName("BffGetUser")
            .WithTags("BFF");

            // Initiates login by redirecting to Keycloak (Google broker). Accepts returnUrl query param.
            endpoints.MapGet("/bff/login", (HttpContext http) =>
            {
                var req = http.Request;
                var qs = req.Query;
                var returnUrl = qs.TryGetValue("returnUrl", out var rv) ? rv.ToString() : "/";

                // Build absolute callback URL so Keycloak can redirect back to us
                var scheme = req.Scheme;
                var host = req.Host.Value;
                var callbackUrl = $"{scheme}://{host}/bff/callback?returnUrl={Uri.EscapeDataString(returnUrl)}";

                var redirectTo = KeycloakGoogleEndpoint + "?redirect_uri=" + Uri.EscapeDataString(callbackUrl);

                return Results.Redirect(redirectTo);
            })
            .WithName("BffLogin")
            .WithTags("BFF");

            // Callback endpoint that Keycloak redirects to after successful login.
            // This minimal demo stores a secure HTTP-only cookie and redirects back to the original page.
            endpoints.MapGet("/bff/callback", (HttpContext http) =>
            {
                var req = http.Request;
                var qs = req.Query;
                var returnUrl = qs.TryGetValue("returnUrl", out var rv) ? rv.ToString() : "/";

                // In a real implementation: validate the incoming code/state, exchange code for tokens server-side,
                // store tokens in a server-side session or secure store and issue a session cookie that maps to that server state.
                // Here we issue a simple HTTP-only cookie to simulate an authenticated session.
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddHours(8)
                };

                // Create a simple session value. Real code should use a secure session id.
                var sessionValue = Guid.NewGuid().ToString();
                http.Response.Cookies.Append(CookieName, sessionValue, cookieOptions);

                return Results.Redirect(returnUrl);
            })
            .WithName("BffCallback")
            .WithTags("BFF");

            // Optional: single endpoint to log out (clear cookie)
            endpoints.MapPost("/bff/logout", (HttpContext http) =>
            {
                if (http.Request.Cookies.ContainsKey(CookieName))
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddDays(-1)
                    };

                    http.Response.Cookies.Append(CookieName, string.Empty, cookieOptions);
                }

                return Results.Ok();
            })
            .WithName("BffLogout")
            .WithTags("BFF");

            return endpoints;
        }
    }

