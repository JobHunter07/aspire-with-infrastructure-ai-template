# Implementation Tasks — Establish Base Template Architecture

1. Create change scaffold
   - `openspec new change "establish-base-template-architecture"`
   - Confirm `.openspec.yaml` is present under `openspec/changes/establish-base-template-architecture/`.

2. Prepare artifacts (proposal, design, tasks)
   - Create `proposal.md`, `design.md`, `tasks.md` (done).

3. Inspect `openspec status --change "establish-base-template-architecture" --json`
   - Parse `applyRequires` and artifact dependency graph.

4. Iterate artifacts until apply-ready
   - For each ready artifact, run `openspec instructions <artifact-id> --change "establish-base-template-architecture" --json` and follow the template.
   - Ensure `proposal`, `design`, and `tasks` are complete and reference each other where required.

5. Implement initial template scaffold (implementation phase — not part of this propose step)
   - Create `AppHost` project and `GatewayHost` project skeletons.
   - Add minimal `Keycloak` container configuration and local realm setup.
   - Add `PostgreSQL` + DBGate provisioning scripts.
   - Add minimal React app with protected demo page and BFF calls.
      - Create `AppHost` project and `GatewayHost` project skeletons.
      - Add minimal `Keycloak` container configuration and local realm setup.
      - Add `PostgreSQL` + DBGate provisioning scripts.
      - Add minimal React app with protected demo page and BFF calls.
      - Create a standalone YARP reverse-proxy container using the Aspire YARP extension and configure service-discovery integration for routes.
      - Wire Aspire orchestration to start components, including the YARP container, Keycloak, Postgres (DBGate), and GatewayHost.
      - Add EF Core migrations and DB provisioning integration.
      - Implement sample API endpoints using Minimal API per-endpoint classes (one class/file per endpoint). Create a sample feature following the pattern:
        - `GatewayHost/Modules/Api/Features/BookFeature/CreateBook/CreateBookEndpoint.cs` (see example: https://github.com/JobHunter07/api.jobhunter07.com/blob/master/JobHunter07.API/Features/BookFeature/CreateBook/CreateBookEndpoint.cs)

6. Verify acceptance criteria
   - Start AppHost and verify all services come up automatically, including the standalone YARP container.
   - Confirm YARP discovers registered routes from `GatewayHost` modules via Aspire service-discovery and routes external requests to the BFF/API.
   - Confirm React demo page redirects to Keycloak and authenticated access works.
   - Confirm DBGate provisioning ran and schema exists.
      - Confirm sample API endpoints are implemented as Minimal API classes, one per file, and registered with the service discovery system so YARP can route to them.

7. Documentation and cleanup
   - Add README snippets demonstrating `AppHost` startup and local dev flow.
   - Provide instructions for customizing Keycloak realm and client settings.

## Notes & Next Steps
- This propose step creates the artifacts required by `openspec` to mark the change ready for `/opsx:apply`.
- After you run `/opsx:apply`, implementation tasks listed in step 5 will be executed in the repository.
