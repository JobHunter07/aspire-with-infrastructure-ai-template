var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithDataVolume();
                      //.WithDbGate();

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
                      .WithOtlpExporter();

builder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
{
    keycloak.WithEndpoint("https", ep => ep.Port = 9999);
    return Task.CompletedTask;
});

// GatewayHost (BFF + YARP + OIDC)
var server = builder.AddProject<Projects.GatewayHost>("gateway")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("KEYCLOAK_HTTPS", "https://localhost:9999")
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

builder.Build().Run();
