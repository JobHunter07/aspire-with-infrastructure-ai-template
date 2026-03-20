using Yarp.ReverseProxy.Configuration;

namespace GatewayHost.Extensions;

public static class YarpExtensions
{
    public static IServiceCollection AddGatewayYarp(
        this IServiceCollection services,
        string webUiAddress = "http://webui/",
        string apiAddress = "http://api/")
    {
        services.AddReverseProxy()
            .LoadFromMemory(
                routes: new[]
                {
                    // Protect SPA root
                    new RouteConfig
                    {
                        RouteId = "webui-root",
                        ClusterId = "webui",
                        Match = new RouteMatch { Path = "/" },
                        AuthorizationPolicy = "default"
                    },

                    // Protect all SPA routes
                    new RouteConfig
                    {
                        RouteId = "webui-catchall",
                        ClusterId = "webui",
                        Match = new RouteMatch { Path = "/{**catch-all}" },
                        AuthorizationPolicy = "default"
                    },

                    // Protect API
                    new RouteConfig
                    {
                        RouteId = "api",
                        ClusterId = "api",
                        Match = new RouteMatch { Path = "/api/{**catch-all}" },
                        AuthorizationPolicy = "default"
                    }
                },
                clusters: new[]
                {
                    new ClusterConfig
                    {
                        ClusterId = "webui",
                        Destinations = new Dictionary<string, DestinationConfig>
                        {
                            ["d1"] = new DestinationConfig
                            {
                                Address = webUiAddress
                            }
                        }
                    },
                    new ClusterConfig
                    {
                        ClusterId = "api",
                        Destinations = new Dictionary<string, DestinationConfig>
                        {
                            ["d1"] = new DestinationConfig
                            {
                                Address = apiAddress
                            }
                        }
                    }
                });

        return services;
    }
}
