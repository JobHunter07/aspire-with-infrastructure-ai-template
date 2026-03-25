## Why

The 2026-03-25 code review identified 9 actionable security issues across the Aspire + Keycloak + YARP Gateway template: 1 critical, 4 high, and 4 medium severity. These vulnerabilities expose the template to brute-force credential attacks, CSRF session hijacking, unauthenticated API access, sensitive information disclosure, and misconfigured TLS/CORS policies. They must be resolved before the template is used as a production baseline.

## What Changes

- **Keycloak realm**: Enable `bruteForceProtected`, lower `failureFactor` to 5, set `waitIncrementSeconds`/`maxFailureWaitSeconds`, enable `revokeRefreshToken`, and set `sslRequired` to `all`
- **Auth cookie**: Change `SecurePolicy` from `SameAsRequest` to `Always` unconditionally in all environments; explicitly set `HttpOnly = true` and `SameSite = Strict`
- **Weather API authorization**: Add `.RequireAuthorization()` to the `GET /weatherforecast` endpoint so direct callers without a valid token are rejected
- **Logout endpoint**: **BREAKING** — Change `/api/account/logout` from `MapGet` to `MapPost` to prevent CSRF logout attacks
- **Account info endpoint**: Restrict `/account/info` to return only `username`, `email`, and `roles` instead of all raw claims
- **RequireHttpsMetadata**: Set to `true` unconditionally in all environments — HTTPS is always required end-to-end (both Weather.API and Gateway.API)
- **Refresh token persistence**: Store the new `refresh_token` back into cookie properties after a silent OIDC refresh in `CookieOidcRefresher`
- **Hardcoded URLs**: Replace all hardcoded `https://localhost:...` strings with `IConfiguration`-sourced values; fail fast if missing **BREAKING** (affects `FrontendUrl`, CORS origins, OIDC authority/issuer)
- **CORS policy**: Replace `AllowAnyMethod()`/`AllowAnyHeader()` with an explicit allow-list (`GET`, `POST`, `OPTIONS`; `Content-Type`, `Authorization`)

## Capabilities

### New Capabilities

- `keycloak-realm-hardening`: Security settings for the Keycloak realm export — brute-force protection, refresh token rotation, and TLS enforcement
- `gateway-auth-hardening`: Auth cookie security policy, SameSite/HttpOnly, OIDC HTTPS metadata, refresh token persistence, claims filtering on account info, CSRF-safe logout, CORS restriction, and configuration-driven URLs
- `weather-api-authorization`: Direct-access authorization enforcement on the Weather API forecast endpoint

### Modified Capabilities

<!-- No existing specs — all capabilities are new -->

## Impact

- `AppHost/Realms/realm-export.json` — Keycloak realm config
- `Gateway.API/Config/ServiceCollectionExtensions.Auth.cs` — Cookie security, OIDC metadata
- `Gateway.API/Account/Account.EndPoints.cs` — Logout method change, claims filtering, FrontendUrl config
- `Gateway.API/Program.cs` — CORS policy
- `Gateway.API/appsettings.json` / `appsettings.Development.json` — New config keys for FrontendUrl, CORS origins
- `Weather.API/Program.cs` — Authorization enforcement, OIDC metadata
- `Gateway.API/Auth/CookieOidcRefresher.cs` — Refresh token storage
- Frontend logout integration — must be updated to POST to `/api/account/logout`
