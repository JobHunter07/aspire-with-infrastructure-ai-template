# Design — Establish Base Template Architecture

## Architecture Overview
- Single Aspire `AppHost` that orchestrates all runtime components (Keycloak, PostgreSQL, GatewayHost, React frontend static server, and optional worker).
- `GatewayHost` is a .NET project that combines:
  - BFF module: server-side UI API endpoints and authentication bridging.
  - API modules: logically separated folders representing features/domain boundaries.
- `YARP` runs as a separate reverse-proxy container managed by Aspire using the Aspire YARP integration (https://aspire.dev/integrations/reverse-proxies/yarp/). YARP performs external routing and proxying and discovers backend services via Aspire's service-discovery integration (https://aspire.dev/integrations/reverse-proxies/yarp/#service-discovery-integration).
- `Keycloak` runs as a container managed by Aspire; configured with a realm, client(s) for BFF and frontend, and Google social identity provider.
- `PostgreSQL` with DBGate for schema provisioning and migrations; EF Core for runtime ORM access.
- `React` frontend served via GatewayHost (or static host) and communicates only with the BFF — no client tokens stored.

## Components & Responsibilities
- AppHost
  - Bootstraps Aspire configuration and registered modules.
  - Starts Keycloak, Postgres (DBGate), GatewayHost, and the standalone YARP reverse-proxy container as hosted services.
- GatewayHost
  - `Modules/Bff`: Authentication endpoints, session management, cookie-based secure session, server-side API aggregation.
  - `Modules/Api`: Example API with minimal protected endpoint for demo.
    - API modules and endpoints will follow a Minimal API per-endpoint pattern: each endpoint is implemented as a Minimal API endpoint class in its own file under a feature folder. Example:
      - `GatewayHost/Modules/Api/BookFeature/CreateBook/CreateBookEndpoint.cs` (see example: https://github.com/JobHunter07/api.jobhunter07.com/blob/master/JobHunter07.API/Features/BookFeature/CreateBook/CreateBookEndpoint.cs)
      - Feature folder layout follows: `Features/<FeatureName>/<Action>/<EndpointFile>.cs` (see: https://github.com/JobHunter07/api.jobhunter07.com/tree/master/JobHunter07.API/Features/BookFeature)
- YARP (standalone reverse-proxy)
  - Configured via Aspire and runs as its own container; routes external traffic to `GatewayHost` BFF and API modules.
  - Uses Aspire service-discovery so `GatewayHost` modules register endpoints and YARP discovers them for routing.
- Keycloak
  - Realm: `aspire-template-dev`
  - Clients: `gateway-bff` (confidential), `react-frontend` (public with redirect to frontend host)
  - Identity Provider: Google (OIDC)
- PostgreSQL + DBGate
  - DBGate provisioning script to create schema and seed demo data.
  - EF Core migrations included in GatewayHost or a migration project.
- React Frontend
  - Minimal app with a protected demo page.
  - Uses cookie-authenticated requests to BFF endpoints; BFF issues/validates session cookies.

## Security & Auth Flow
1. User visits React app and clicks “Demo” (protected).
2. React requests `/bff/identity` (or `/bff/login`) — BFF redirects to Keycloak if no session.
3. Keycloak handles Google social login and returns code to BFF client.
4. BFF exchanges code for tokens, creates a secure HttpOnly cookie session, and redirects back to the React app.
5. React calls BFF-proxied APIs; BFF attaches user context server-side.

## Configuration & Secrets
- All secrets (Keycloak admin creds, DB connection strings) stored in Aspire resource configuration files with safe local defaults and documented env var overrides.
- Local dev uses ephemeral passwords and local Keycloak container; production-ready overrides are documented but not included.

## Local Developer Experience
- Single command (`AppHost`) boots Keycloak, PostgreSQL (DBGate), `GatewayHost`, and a standalone YARP container (via the Aspire YARP extension); the YARP instance discovers routes from services started by Aspire.
- DBGate runs initial provisioning automatically during startup.

## Extension Points
- Add new API modules under `GatewayHost/Modules/Api/<feature>`.
- Extract a module as microservice by creating a new project and updating YARP routes and AppHost orchestration.

## Minimal File Layout (suggested)
- AppHost/
- GatewayHost/
  - Modules/
    - Bff/
    - Api/
      - Features/
        - <FeatureName>/
          - <Action>/
            - <FeatureAction>Endpoint.cs
- yarp/ (Aspire YARP container config / route templates)
- frontend/ (React)
- db/ (DBGate provisioning scripts)

## Notes
- Keep domain logic out of the template; include only minimal demo endpoints.
- Prefer explicit wiring and small, readable configurations to keep the template approachable.
