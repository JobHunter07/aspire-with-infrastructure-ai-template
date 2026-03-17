GatewayHost BFF module

Responsibilities:
- Handle auth bridging and session management
- Expose server-side endpoints consumed by the React frontend
- Provide endpoints that aggregate or proxy to internal API modules

Implement as a minimal .NET project; register endpoints and service discovery metadata so YARP can discover routes.
