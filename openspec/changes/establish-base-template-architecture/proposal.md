# Proposal: Establish Base Template Architecture for Aspire + BFF + Gateway + Keycloak

## Summary
Create the initial foundation for a reusable, batteries‑included project template that provides a fully configured, production‑grade architecture using .NET Aspire, Keycloak authentication, YARP Gateway, BFF pattern, React frontend, and modular monolith structure. This template enables developers to immediately begin feature development without manually wiring infra, authentication, routing, or DB plumbing.

## Goals
- Provide a base repository template including essential infrastructure and security best practices.
- Use .NET Aspire as the orchestrator for all services.
 - Keep the **BFF** in the `GatewayHost` for server-side UI/API bridging, and run **YARP** as a separate reverse-proxy integration using Aspire's YARP integration (https://aspire.dev/integrations/reverse-proxies/yarp/).
	 - Routes will be configured via Aspire's service-discovery integration for YARP (https://aspire.dev/integrations/reverse-proxies/yarp/#service-discovery-integration) so backend modules register services and YARP discovers them automatically.
- Integrate Keycloak (with Google social login) via Aspire's Keycloak extension.
- Integrate PostgreSQL via EF Core + PostgreSQL extension and provision schema with DBGate.
- Include a React frontend wired to the BFF for secure, token‑free authentication.
- Establish a modular monolith layout that can be split into microservices later.
 - Require API endpoints to be implemented as Minimal API controllers, each endpoint in its own class and file (per-feature folder). Follow the project conventions demonstrated in these examples:
	 - https://github.com/JobHunter07/api.jobhunter07.com/blob/master/JobHunter07.API/Features/BookFeature/CreateBook/CreateBookEndpoint.cs
	 - https://github.com/JobHunter07/api.jobhunter07.com/tree/master/JobHunter07.API/Features/BookFeature

## Non-Goals
- Implementing business features beyond a minimal demo page.
- Full CI/CD, cloud infra, or deployment pipelines.

## High-Level Deliverables
- Aspire AppHost project orchestrating Keycloak, PostgreSQL (DBGate), GatewayHost, React frontend, and a worker placeholder.
- GatewayHost project containing the BFF and API module folder structure.
 - GatewayHost project containing the BFF and API module folder structure; API modules will use per-endpoint Minimal API controller classes and files.
- A separate YARP reverse-proxy integration (managed by Aspire) that handles routing and proxying; YARP will use Aspire's service-discovery integration to obtain routes from registered services.
- Minimal React app with a protected demo page.
- Documentation describing architecture and how to extend the template.

## Acceptance Criteria
- `AppHost` starts all services without manual configuration.
- Visiting the React app allows navigation to a protected page.
- Unauthenticated users are redirected to Keycloak's Google login.
- Authenticated users can access the protected page.
- GatewayHost compiles and runs with BFF + YARP + API modules in place.
 - GatewayHost compiles and runs with BFF and API modules in place; API endpoints use per-file Minimal API controller classes. YARP reverse-proxy is configured and discovered via Aspire's YARP service-discovery integration.
- PostgreSQL is provisioned automatically using DBGate.
- Configuration is handled via Aspire resources (not Dockerfiles).

## Motivation
Developers waste time re-creating the same infra and wiring. A standardized, secure, and scalable template reduces onboarding time and lets teams focus on features.
