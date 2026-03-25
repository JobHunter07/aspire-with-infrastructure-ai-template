## Context

The Aspire + Keycloak + YARP Gateway template is a production-baseline scaffold. A security review dated 2026-03-25 found 9 issues across 3 tiers (critical / high / medium). Fixes must not break developer experience (local `aspire run` with Docker should require zero extra setup beyond existing `.env` / `appsettings.Development.json` values).

Affected subsystems:
- **Keycloak realm export** (`AppHost/Realms/realm-export.json`) — declarative seed config imported at startup
- **Gateway.API** — YARP reverse proxy with OIDC/cookie auth, account endpoints, CORS
- **Weather.API** — upstream service with JWT bearer auth
- **CookieOidcRefresher** — background token-refresh middleware
- **Frontend** — React/Vite SPA that calls `/api/account/logout`

## Goals / Non-Goals

**Goals:**
- Resolve all 9 issues from the 2026-03-25 security review before this template is promoted to a production baseline
- Maintain full offline / Airplane Mode development with zero new required secrets for local runs
- Keep changes self-contained per vertical slice — no new shared utilities or global helpers
- Dev environment is identical to production in security posture — no security feature is relaxed or gated by environment; Dev IS Prod

**Non-Goals:**
- Upgrading Keycloak, YARP, or .NET versions
- Introducing new infrastructure (e.g., WAF, rate-limit middleware beyond Keycloak)
- Fixing the `openspec/config.yaml` YAML lint warning (separate concern)
- End-to-end integration test automation (manual verification is sufficient for this template)

## Decisions

### D1 — Dev == Prod: unconditional security, no environment gates
**Decision:** All security settings (cookie secure policy, OIDC RequireHttpsMetadata, HTTPS enforcement) are hardcoded to their most secure values with no `IsDevelopment()` or environment checks. Local development runs with full production-grade security.

**Rationale:** Environment-conditional security creates a class of bugs where relaxed dev settings are accidentally promoted to production, or where dev-only code paths hide real security regressions. Treating Dev as Prod eliminates that entire category. The Aspire local dev stack uses HTTPS certificates (via `dotnet dev-certs`) and Keycloak with TLS from day one, so there is no practical barrier to full security in development.

**Alternative Considered:** `IsDevelopment()` guards to allow HTTP in local dev — rejected because it creates divergence between environments that is invisible until deployment, and the Aspire toolchain already provides HTTPS locally with no extra effort.

---

### D2 — Logout endpoint: GET → POST (breaking change)
**Decision:** Change `MapGet("/api/account/logout")` to `MapPost("/api/account/logout")`.

**Rationale:** RFC 7231 reserves GET for safe, idempotent operations. A sign-out is a state-changing operation. Keeping it on GET makes CSRF trivially achievable (`<img src="/api/account/logout">`). The frontend must be updated to use a form POST or `fetch` with `method: 'POST'`.

**Alternative Considered:** Adding an `X-CSRF-Token` header check on the GET — rejected because it introduces custom header validation infrastructure; moving to POST is simpler and correct.

---

### D3 — Claims on `/account/info`: allowlist vs. denylist
**Decision:** Allowlist — return only `preferred_username`, `email`, and `roles`.

**Rationale:** A denylist requires keeping a maintained list of private claim types; the set of Keycloak-injected claims can grow. An allowlist is safe by default and requires explicit intent to expose new fields.

---

### D4 — Configuration-driven URLs: IConfiguration vs. IOptions\<T\>
**Decision:** Use `IConfiguration` directly in the extension methods (read via `GetRequiredValue` which throws if missing).

**Rationale:** These values are infrastructure URLs, not feature settings — they don't benefit from strong typing at this stage. Using `IConfiguration.GetRequiredValue(key)` provides fail-fast behavior with a clear error message, and avoids creating a new `Options` class just to hold 2–3 URL strings (over-engineering for a template).

---

### D5 — Refresh token persistence: store new value with fallback
**Decision:** Store `message.RefreshToken ?? existingRefreshToken` so that single-use rotation and multi-use are both handled.

**Rationale:** If Keycloak returns a new refresh token (rotation enabled), use it. If it doesn't (rotation disabled), fall back to the current stored token to avoid losing it. This is defensive and correct in both modes.

## Risks / Trade-offs

- **Frontend regression** → The logout method change (D2) requires the frontend to stop using `window.location.href = '/api/account/logout'` and instead use `fetch('/api/account/logout', { method: 'POST', credentials: 'include' })`. If missed, the logout button silently 405s.
  *Mitigation:* Update the frontend and document the change in tasks.

- **Hardcoded URL removal** → If a consumer forks the template without setting the new config keys, the app will throw on startup.
  *Mitigation:* Provide sensible `appsettings.Development.json` default values for localhost; document required production env vars in README.

- **Keycloak brute-force lockout in dev** → Enabling `bruteForceProtected` with `failureFactor: 5` means developers who mistype a password 5 times will be locked out of the local dev realm.
  *Mitigation:* Set `failureFactor: 10` in the dev realm export; production deployments should lower it further. Document in realm-export comments.

- **CORS restriction** → Restricting to `GET, POST, OPTIONS` may break any future endpoints that use `PUT`/`PATCH`/`DELETE` via the gateway.
  *Mitigation:* This template currently has no such endpoints. Document the allowed method list so future contributors know to update it.
