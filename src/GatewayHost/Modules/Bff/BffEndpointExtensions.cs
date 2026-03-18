namespace GatewayHost.Modules.Bff;

// Simple WebApplication-level extension that registers BFF endpoints from the feature implementation.
public static class BffEndpointExtensions
{
    public static WebApplication MapBffEndpoints(this WebApplication app)
    {
        // Call the feature-level extension to register endpoints on the app's endpoint route builder.
        BffEndpoints.MapBffEndpoints(app);
        return app;
    }
}
