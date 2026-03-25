# Code Review: Aspire + Keycloak + YARP Gateway
**Review Date:** 2026-03-25

**Ready for Production:** No

**Critical Issues:** 1 | **High Issues:** 4 | **Medium Issues:** 4

---

## Priority 1 — Must Fix ⛔

### 1. Brute Force Protection Disabled in Keycloak Realm
**File:** `AppHost/Realms/realm-export.json`

`"bruteForceProtected": false` means there is zero protection against password spraying or credential stuffing against your Keycloak login page. The `failureFactor: 30` threshold is irrelevant while this is off.

**Fix:**
```json
"bruteForceProtected": true,
"permanentLockout": false,
"failureFactor": 5,
"waitIncrementSeconds": 60,
"maxFailureWaitSeconds": 900
```

---

## Priority 2 — High ⚠️

### 2. Auth Cookie Secure Policy is `SameAsRequest`
**File:** `Gateway.API/Config/ServiceCollectionExtensions.Auth.cs`

```csharp
// VULNERABLE – cookie is transmitted over plain HTTP if the request was HTTP
cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
```

If any HTTP request ever reaches the gateway, the session cookie is sent in cleartext, making it interceptable. It should be `Always` in production. Use environment detection:

```csharp
cookie.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
```
(Pass `IWebHostEnvironment` into the extension method, or read it from `IHostEnvironment` via the service collection.)

---

### 3. Weather API Endpoint Has No Authorization Enforcement
**File:** `Weather.API/Program.cs`

```csharp
// VULNERABLE – no .RequireAuthorization() applied
api.MapGet("weatherforecast", () => { ... })
   .CacheOutput(...)
   .WithName("GetWeatherForecast");
```

Authorization is only enforced at the YARP gateway level. Anyone who can reach the Weather API directly (e.g., in a cloud deployment where pod-to-pod traffic isn't network-restricted) gets weather data without a token.

**Fix:**
```csharp
api.MapGet("weatherforecast", () => { ... })
   .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(5)))
   .WithName("GetWeatherForecast")
   .RequireAuthorization();
```

---

### 4. Logout via HTTP GET — CSRF Vulnerability
**File:** `Gateway.API/Account/Account.EndPoints.cs`

```csharp
// VULNERABLE – an attacker can embed <img src="/api/account/logout"> in any page
api.MapGet("/account/logout", async (HttpContext http) => { ... })
   .RequireAuthorization();
```

State-changing operations (sign-out) should never be on a `GET`. Change to `POST` and include anti-forgery or SameSite protection:

```csharp
api.MapPost("/api/account/logout", async (HttpContext http) =>
{
    var prop = new AuthenticationProperties { RedirectUri = "/api/account/public" };
    await http.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, prop);
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
})
.WithName("AccountLogout")
.WithTags("Account")
.RequireAuthorization();
```

---

### 5. `/account/info` Returns All Claims — Information Disclosure (A01/A02)
**File:** `Gateway.API/Account/Account.EndPoints.cs`

```csharp
// VULNERABLE – exposes all internal token claims to the browser client
var claims = http.User?.Claims
    .Select(c => (object)new { c.Type, c.Value })
    .ToList() ?? new List<object>();
return Results.Ok(claims);
```

This leaks internal claim types, token metadata, session IDs, and any PII that was mapped from Keycloak. You should project only what the UI actually needs:

```csharp
var user = http.User;
if (user?.Identity?.IsAuthenticated != true)
    return Results.Unauthorized();

return Results.Ok(new
{
    username = user.FindFirst("preferred_username")?.Value,
    email = user.FindFirst(ClaimTypes.Email)?.Value,
    roles = user.FindAll("roles").Select(c => c.Value)
});
```

---

## Priority 3 — Medium 📋

### 6. `RequireHttpsMetadata = false` in Production Code Paths
**Files:** `Weather.API/Program.cs`, `Gateway.API/Config/ServiceCollectionExtensions.Auth.cs`

Both are unconditionally `false`. This disables HTTPS verification on the OIDC discovery/token requests in environments where you'd expect it to be enforced. Make it environment-conditional:
```csharp
options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
```

---

### 7. Refresh Token Not Stored Back After Silent Refresh
**File:** `Gateway.API/Auth/CookieOidcRefresher.cs`

```csharp
validateContext.Properties.StoreTokens([
    new() { Name = "access_token", Value = message.AccessToken },
    new() { Name = "id_token", Value = message.IdToken },
    new() { Name = "token_type", Value = message.TokenType },
    new() { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) },
    // ← refresh_token is missing!
]);
```

If you ever enable `revokeRefreshToken: true` in Keycloak (single-use rotation), the old refresh token is consumed and not replaced, silently logging users out on the next cycle. Add:
```csharp
new() { Name = "refresh_token", Value = message.RefreshToken ?? validateContext.Properties.GetTokenValue("refresh_token")! },
```

---

### 8. Hardcoded Localhost URLs Throughout the Solution
Several files use `"https://localhost:..."` strings that must be replaced by configuration before any non-local deployment:

| File | Value |
|---|---|
| `Weather.API/Program.cs` | `Authority` and `ValidIssuers` |
| `Gateway.API/Account/Account.EndPoints.cs` | `FrontendUrl` const |
| `Gateway.API/Program.cs` | CORS `WithOrigins(...)` |

The `//ToDo:kbdavis07` comments acknowledge this. Use `IConfiguration`/environment variables and fail fast with a clear error if the values are missing.

---

### 9. CORS Allows All Methods and Headers
**File:** `Gateway.API/Program.cs`

```csharp
.AllowAnyMethod()   // allows DELETE, PUT, PATCH, etc.
.AllowAnyHeader()
```

Restrict to the methods the gateway actually supports:
```csharp
.WithMethods("GET", "POST", "OPTIONS")
.WithHeaders("Content-Type", "Authorization")
```

---

## Low / Informational ℹ️

- **`AppHost/Realms/realm-export.json`:** `"revokeRefreshToken": false` — refresh tokens can be replayed. Enable token rotation for better session security in production.
- **`AppHost/Realms/realm-export.json`:** `"sslRequired": "external"` — internal pod-to-pod traffic is not TLS-enforced by Keycloak. Use `"all"` in production.
- **`Gateway.API/Config/ServiceCollectionExtensions.cs`:** `services.BuildServiceProvider()` creates a second DI container scope (the "service locator" anti-pattern). Replace with `IOptions<T>` injection or `IConfiguration` directly.
- **`ServiceDefaults/Extensions.cs`:** Health check endpoints are only mapped in `Development`. Ensure a protected, authenticated `/health` is available in staging/production for orchestrators.
- **`Gateway.API/Config/ServiceCollectionExtensions.Auth.cs`:** `HttpOnly` and `SameSite` are not explicitly set on the auth cookie. ASP.NET Core defaults to `HttpOnly = true`, but explicit configuration is safer: `cookie.Cookie.HttpOnly = true; cookie.Cookie.SameSite = SameSiteMode.Strict;`

---

## Summary

| # | Issue | Severity | File |
|---|---|---|---|
| 1 | Brute force protection disabled in Keycloak | **Critical** | realm-export.json |
| 2 | Cookie secure policy `SameAsRequest` | **High** | ServiceCollectionExtensions.Auth.cs |
| 3 | Weather endpoint unauthenticated | **High** | Weather.API/Program.cs |
| 4 | Logout on GET (CSRF) | **High** | Account.EndPoints.cs |
| 5 | All claims exposed via `/account/info` | **High** | Account.EndPoints.cs |
| 6 | `RequireHttpsMetadata = false` always | Medium | Program.cs (both) |
| 7 | Refresh token not persisted after rotation | Medium | CookieOidcRefresher.cs |
| 8 | Hardcoded localhost URLs | Medium | Multiple |
| 9 | Overly permissive CORS (any method/header) | Medium | Gateway.API/Program.cs |

Items 1–5 should be resolved before staging/production deployment. Items 6–9 should be addressed before any public-facing release.
