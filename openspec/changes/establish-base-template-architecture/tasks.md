# Implementation Tasks — Establish Base Template Architecture

- [x] Create change scaffold
   - `openspec new change "establish-base-template-architecture"`
   - Confirm `.openspec.yaml` is present under `openspec/changes/establish-base-template-architecture/`.

- [x] Prepare artifacts (proposal, design, tasks)
   - Create `proposal.md`, `design.md`, `tasks.md`.

- [x] Inspect `openspec status --change "establish-base-template-architecture" --json`
   - Parse `applyRequires` and artifact dependency graph.

- [x] Iterate artifacts until apply-ready
   - For each ready artifact, run `openspec instructions <artifact-id> --change "establish-base-template-architecture" --json` and follow the template.
   - Ensure `proposal`, `design`, and `tasks` are complete and reference each other where required.

 - [x] Implement initial template scaffold (implementation phase)
   - [x] Create `AppHost` project and `GatewayHost` project skeletons (scaffolded folders and READMEs).
   - [ ] Add minimal `Keycloak` container configuration and local realm setup.
   - [x] Add `PostgreSQL` + DBGate provisioning scripts (added sample `db/provision.sql`).
   - [x] Add minimal React app placeholder (frontend README created).
   - [x] Create a standalone YARP reverse-proxy container config placeholder using the Aspire YARP extension (yarp/README.md).
   - [ ] Wire Aspire orchestration to start components, including the YARP container, Keycloak, Postgres (DBGate), and GatewayHost.
   - [ ] Add EF Core migrations and DB provisioning integration.
   - [x] Implement sample API endpoints using Minimal API per-endpoint classes (one class/file per endpoint). Created sample:
      - `GatewayHost/Modules/Api/Features/BookFeature/CreateBook/CreateBookEndpoint.cs` (sample minimal endpoint added)
   - [x] Add AppHost orchestration placeholders (aspire-resources.yaml) and Keycloak realm export placeholder.
   - [x] Add EF Core migrations placeholder (GatewayHost/Migrations/README.md).

- [ ] Verify acceptance criteria
   - [ ] Start AppHost and verify all services come up automatically, including the standalone YARP container.
   - [ ] Confirm YARP discovers registered routes from `GatewayHost` modules via Aspire service-discovery and routes external requests to the BFF/API.
   - [ ] Confirm React demo page redirects to Keycloak and authenticated access works.
   - [ ] Confirm DBGate provisioning ran and schema exists.
   - [ ] Confirm sample API endpoints are implemented as Minimal API classes, one per file, and registered with the service discovery system so YARP can route to them.

- [ ] Documentation and cleanup
   - [ ] Add README snippets demonstrating `AppHost` startup and local dev flow.
   - [ ] Provide instructions for customizing Keycloak realm and client settings.

## Notes & Next Steps
- This propose step creates the artifacts required by `openspec` to mark the change ready for `/opsx:apply`.
- After you run `/opsx:apply`, implementation tasks listed above will be executed in the repository.
