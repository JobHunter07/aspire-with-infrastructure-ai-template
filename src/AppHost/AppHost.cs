var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithDataVolume()
                      .WithDbGate();

var keycloak = builder.AddKeycloak("keycloak", 8080)
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithDataVolume()
                      .WithRealmImport("./Realms");

var server = builder.AddProject<Projects.GatewayHost>("gatewayhost")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithOtlpExporter();

#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server)
    .WithHttpsEndpoint(env: "PORT", port: 54955)
    .WithHttpsDeveloperCertificate();
#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

server.PublishWithContainerFiles(webfrontend, "wwwroot");


var gateway = builder.AddYarp("gateway")
                     .WithConfiguration(yarp =>
                     {
                         // Add catch-all route for frontend service
                         yarp.AddRoute(server);

                         // Service discovery automatically resolves server endpoints
                         // yarp.AddRoute("/api/{**catch-all}", server);
                     });


builder.Build().Run();
