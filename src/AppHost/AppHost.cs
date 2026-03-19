using System.Net.Sockets;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithDataVolume()
                      .WithDbGate();

var database = postgres.AddDatabase("keycloak-db");

var keycloakDbUrl = ReferenceExpression.Create($"jdbc:postgresql://{postgres.Resource.Host}/{database.Resource.DatabaseName}");

var keycloak = builder.AddKeycloak("keycloak")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithDataVolume()
                      .WithRealmImport("./Realms")
                      .WithReference(postgres)
                      .WaitFor(postgres)
                      .WithEnvironment("KC_DB", "postgres") // Database "Type" not Name
                      .WithEnvironment("KC_DB_USERNAME", postgres.Resource.UserNameReference)
                      .WithEnvironment("KC_DB_PASSWORD", postgres.Resource.PasswordParameter)
                      .WithEnvironment("KC_DB_URL", keycloakDbUrl)
                      .WithEnvironment("KC_HTTP_ENABLED", "false")
                      .WithEndpoint("keycloak-https", e =>
                      {
                          e.Port = 9999;
                          e.TargetPort = 8443;
                          e.Protocol = ProtocolType.Udp;
                          e.UriScheme = "https";
                      })
                      // Could not determine host address and port for container port
                      // .WithHttpsEndpoint(name: "keycloak-https", port: 9999, targetPort: 8443, env: "KC_HTTPS_PORT") 
                      .WithOtlpExporter();


var server = builder.AddProject<Projects.GatewayHost>("gatewayhost")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithReference(cache)
    .WaitFor(cache)
    //.WithReference(keycloak)
    // KEYCLOAK_HTTPS
    .WithEnvironment("KEYCLOAK_HTTPS", "https://localhost:9999")
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
