## 1. Keycloak Realm Hardening

- [x] 1.1 Set `bruteForceProtected` to `true` in `AppHost/Realms/realm-export.json`
- [x] 1.2 Set `permanentLockout` to `false`, `failureFactor` to `10`, `waitIncrementSeconds` to `60`, and `maxFailureWaitSeconds` to `900` in realm-export.json
- [x] 1.3 Set `revokeRefreshToken` to `true` in realm-export.json
- [x] 1.4 Set `sslRequired` to `"all"` in realm-export.json

## 2. Weather API Authorization

- [x] 2.1 Add `.RequireAuthorization()` to the `MapGet("weatherforecast", ...)` endpoint in `Weather.API/Program.cs`
- [x] 2.2 Verify that a request without a bearer token to `GET /weatherforecast` returns HTTP 401

## 3. Configuration-Driven URLs (Gateway.API)

- [x] 3.1 Add `FrontendUrl` key to `Gateway.API/appsettings.Development.json` with the current localhost value
- [x] 3.2 Add `FrontendUrl` placeholder comment to `Gateway.API/appsettings.json` (no default — must be set per environment)
- [x] 3.3 Replace the hardcoded `FrontendUrl` const in `Gateway.API/Account/Account.EndPoints.cs` with `IConfiguration.GetRequiredSection("FrontendUrl").Value` (or equivalent fail-fast read)
- [x] 3.4 Replace the hardcoded `WithOrigins(...)` value in `Gateway.API/Program.cs` with the config-sourced `FrontendUrl` value

## 4. Configuration-Driven OIDC Settings (Weather.API)

- [x] 4.1 Add `Oidc:Authority` and `Oidc:ValidIssuers` keys to `Weather.API/appsettings.Development.json` with current localhost values
- [x] 4.2 Add `Oidc:Authority` and `Oidc:ValidIssuers` placeholder comments to `Weather.API/appsettings.json`
- [x] 4.3 Replace hardcoded `Authority` and `ValidIssuers` values in `Weather.API/Program.cs` with config-sourced values using `IConfiguration.GetRequiredSection("Oidc:Authority").Value`
- [x] 4.4 Verify the app throws a descriptive error on startup if `Oidc:Authority` is missing

## 5. Auth Cookie Security (Gateway.API)

- [x] 5.1 In `Gateway.API/Config/ServiceCollectionExtensions.Auth.cs`, replace `CookieSecurePolicy.SameAsRequest` with `CookieSecurePolicy.Always` unconditionally (no `IsDevelopment()` check, no `IWebHostEnvironment` parameter needed)
- [x] 5.2 Explicitly set `cookie.Cookie.HttpOnly = true` in the same method
- [x] 5.3 Explicitly set `cookie.Cookie.SameSite = SameSiteMode.Strict` in the same method

## 6. OIDC RequireHttpsMetadata — Always Enabled

- [x] 6.1 In `Gateway.API/Config/ServiceCollectionExtensions.Auth.cs`, change `RequireHttpsMetadata = false` to `RequireHttpsMetadata = true` unconditionally (no `IsDevelopment()` check)
- [x] 6.2 In `Weather.API/Program.cs`, change `RequireHttpsMetadata = false` to `RequireHttpsMetadata = true` unconditionally (no `IsDevelopment()` check)

## 7. Refresh Token Persistence (CookieOidcRefresher)

- [x] 7.1 In `Gateway.API/Auth/CookieOidcRefresher.cs`, add `new() { Name = "refresh_token", Value = message.RefreshToken ?? validateContext.Properties.GetTokenValue("refresh_token")! }` to the `StoreTokens` call

## 8. Logout Endpoint — CSRF Fix (Gateway.API)

- [x] 8.1 Change `api.MapGet("/api/account/logout", ...)` to `api.MapPost("/api/account/logout", ...)` in `Gateway.API/Account/Account.EndPoints.cs`
- [x] 8.2 Update the frontend logout button/action in `src/frontend/src/` to send a `POST` request to `/api/account/logout` instead of navigating via GET

## 9. Account Info — Claims Allowlist (Gateway.API)

- [x] 9.1 In `Gateway.API/Account/Account.EndPoints.cs`, replace the `Select(c => new { c.Type, c.Value })` projection in `/account/info` with a projection returning only `username` (`preferred_username` claim), `email` (`ClaimTypes.Email`), and `roles`
- [x] 9.2 Add a guard to return `Results.Unauthorized()` if `user?.Identity?.IsAuthenticated != true`

## 10. CORS Policy Restriction (Gateway.API)

- [x] 10.1 In `Gateway.API/Program.cs`, replace `AllowAnyMethod()` with `.WithMethods("GET", "POST", "OPTIONS")`
- [x] 10.2 Replace `AllowAnyHeader()` with `.WithHeaders("Content-Type", "Authorization")`

## 11. Verification

- [ ] 11.1 Run `aspire run` locally and verify login, weather forecast fetch, and logout all function correctly
- [ ] 11.2 Verify direct access to `Weather.API` without a token returns 401
- [ ] 11.3 Verify `GET /api/account/logout` returns 405
- [ ] 11.4 Verify `/account/info` does not expose raw token claims
- [ ] 11.5 Run pre-PR check script and confirm no regressions
