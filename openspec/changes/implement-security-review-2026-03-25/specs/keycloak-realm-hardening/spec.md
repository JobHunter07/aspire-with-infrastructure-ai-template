## ADDED Requirements

### Requirement: Brute force protection is enabled
The Keycloak realm configuration SHALL have `bruteForceProtected` set to `true` to defend against password spraying and credential stuffing attacks.

#### Scenario: Brute force protection configured
- **WHEN** the realm export JSON is imported
- **THEN** `bruteForceProtected` is `true`, `permanentLockout` is `false`, `failureFactor` is `10`, `waitIncrementSeconds` is `60`, and `maxFailureWaitSeconds` is `900`

#### Scenario: Account lockout after threshold
- **WHEN** a user exceeds `failureFactor` failed login attempts
- **THEN** the account is temporarily locked for `waitIncrementSeconds` seconds

---

### Requirement: Refresh token rotation is enabled
The Keycloak realm configuration SHALL have `revokeRefreshToken` set to `true` so that each refresh token can only be used once, preventing replay attacks.

#### Scenario: Refresh token rotation configured
- **WHEN** the realm export JSON is imported
- **THEN** `revokeRefreshToken` is `true`

---

### Requirement: TLS is required for all Keycloak connections
The Keycloak realm configuration SHALL have `sslRequired` set to `"all"` to ensure TLS is enforced for all connections, not only external ones.

#### Scenario: SSL required for all traffic
- **WHEN** the realm export JSON is imported
- **THEN** `sslRequired` is `"all"`
