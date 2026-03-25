## ADDED Requirements

### Requirement: Auth cookie always uses Secure policy
The authentication cookie SecurePolicy SHALL be `CookieSecurePolicy.Always` unconditionally in all environments, including local development, preventing cookies from ever being transmitted over plain HTTP.

#### Scenario: Cookie is Always-Secure in every environment
- **WHEN** the application starts in any environment (development, staging, or production)
- **THEN** `SecurePolicy` is `CookieSecurePolicy.Always`

---

### Requirement: Auth cookie has explicit HttpOnly and SameSite settings
The authentication cookie SHALL explicitly set `HttpOnly = true` and `SameSite = SameSiteMode.Strict` to prevent JavaScript access and cross-site request forgery.

#### Scenario: Cookie configuration is explicit
- **WHEN** the authentication cookie is created
- **THEN** `HttpOnly` is `true` and `SameSite` is `Strict`

---

### Requirement: OIDC RequireHttpsMetadata is always enabled
Both Gateway.API and Weather.API SHALL set `RequireHttpsMetadata = true` unconditionally in all environments, including local development. HTTPS is required end-to-end with no exceptions.

#### Scenario: HTTPS metadata required in every environment
- **WHEN** the application starts in any environment (development, staging, or production)
- **THEN** `RequireHttpsMetadata` is `true`

---

### Requirement: Refresh token is persisted after silent OIDC refresh
After a successful silent token refresh, the CookieOidcRefresher SHALL store the new refresh token (or retain the existing one if the provider does not return a new one) in the cookie properties.

#### Scenario: New refresh token stored after rotation
- **WHEN** a silent token refresh returns a new `refresh_token` from the OIDC provider
- **THEN** the new `refresh_token` is stored in `validateContext.Properties`

#### Scenario: Existing refresh token retained when none returned
- **WHEN** a silent token refresh does not return a new `refresh_token`
- **THEN** the existing stored `refresh_token` is preserved in `validateContext.Properties`

---

### Requirement: Logout endpoint accepts only POST requests
The `/api/account/logout` endpoint SHALL be registered as `MapPost` (not `MapGet`) to prevent CSRF-based forced logout via GET requests.

#### Scenario: POST logout succeeds
- **WHEN** an authenticated user sends a `POST` request to `/api/account/logout`
- **THEN** the user is signed out of both cookie and OIDC schemes and redirected appropriately

#### Scenario: GET logout returns 405
- **WHEN** any caller sends a `GET` request to `/api/account/logout`
- **THEN** the server returns HTTP 405 Method Not Allowed

---

### Requirement: Account info endpoint returns only required fields
The `/account/info` endpoint SHALL return only `username`, `email`, and `roles` fields and SHALL NOT expose raw token claims or internal metadata.

#### Scenario: Authenticated user receives minimal profile
- **WHEN** an authenticated user sends a request to `/account/info`
- **THEN** the response contains only `username`, `email`, and `roles`

#### Scenario: Internal claims are not included in response
- **WHEN** the OIDC token contains internal claim types (e.g., session IDs, nonce, aud)
- **THEN** those claims are NOT present in the `/account/info` response

---

### Requirement: CORS policy is restricted to an explicit method and header allowlist
The Gateway.API CORS policy SHALL use an explicit allowlist of HTTP methods (`GET`, `POST`, `OPTIONS`) and headers (`Content-Type`, `Authorization`) instead of `AllowAnyMethod()` / `AllowAnyHeader()`.

#### Scenario: Allowed methods are accepted
- **WHEN** a CORS preflight request specifies `GET`, `POST`, or `OPTIONS`
- **THEN** the server returns the appropriate `Access-Control-Allow-Methods` header

#### Scenario: Disallowed methods are rejected
- **WHEN** a CORS preflight request specifies `DELETE`, `PUT`, or `PATCH`
- **THEN** the server does not include those methods in `Access-Control-Allow-Methods`

---

### Requirement: Frontend URL and CORS origins are sourced from configuration
The `FrontendUrl` constant and CORS `WithOrigins(...)` value in Gateway.API SHALL be read from `IConfiguration` with a required key, not hardcoded as `https://localhost:...` strings.

#### Scenario: App starts with config present
- **WHEN** the configuration key for FrontendUrl is set
- **THEN** the application starts successfully and uses the configured URL for CORS and redirects

#### Scenario: App fails fast when config missing
- **WHEN** the required configuration key for FrontendUrl is absent
- **THEN** the application throws a descriptive configuration error on startup

---

### Requirement: OIDC Authority and ValidIssuers are sourced from configuration
Weather.API's OIDC `Authority` and `ValidIssuers` values SHALL be read from `IConfiguration` rather than hardcoded as `https://localhost:...` strings.

#### Scenario: Weather API starts with OIDC config present
- **WHEN** the OIDC Authority config key is set
- **THEN** the application starts and validates JWTs using the configured authority

#### Scenario: Weather API fails fast when OIDC config missing
- **WHEN** the required OIDC Authority configuration key is absent
- **THEN** the application throws a descriptive configuration error on startup
