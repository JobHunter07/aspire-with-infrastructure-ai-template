var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithDataVolume()
                      .WithDbGate();

var database = postgres.AddDatabase("keycloak-db");

var keycloakDbUrl = ReferenceExpression.Create($"jdbc:postgresql://{postgres.Resource.Host}/{database.Resource.DatabaseName}");

var keycloak2 = builder.AddKeycloak("keycloak2", 9999)
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithDataVolume()
                      .WithRealmImport("./Realms")
                      //.WithReference(postgres)
                      //.WaitFor(postgres)
                      //.WithEnvironment("KC_DB", "postgres") // Database "Type" not Name
                      //.WithEnvironment("KC_DB_USERNAME", postgres.Resource.UserNameReference)
                      //.WithEnvironment("KC_DB_PASSWORD", postgres.Resource.PasswordParameter)
                      //.WithEnvironment("KC_DB_URL", keycloakDbUrl)
                      .WithEnvironment("KC_HTTP_ENABLED", "false")
                      .WithOtlpExporter();

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak:26.4")
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_USERNAME", postgres.Resource.UserNameReference)
    .WithEnvironment("KC_DB_PASSWORD", postgres.Resource.PasswordParameter)
    .WithEnvironment("KC_DB_URL", keycloakDbUrl)
    .WithEnvironment("KC_HTTP_ENABLED", "false")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "N}Hh)k_PgT7ArDh))1nT2B")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_HTTPS_CERTIFICATE_FILE", "/usr/lib/ssl/aspire/private/8BFD721862BF6F0296392F4F92F9AF4B2176A160.crt")
    .WithEnvironment("KC_HTTPS_CERTIFICATE_KEY_FILE", "/usr/lib/ssl/aspire/private/8BFD721862BF6F0296392F4F92F9AF4B2176A160.key")
    .WithBindMount("./Realms", "/opt/keycloak/data/import")
    .WithArgs("start-dev", "--import-realm")
    .WithEndpoint("https", e =>
    {
        e.Port = 9999;
        e.TargetPort = 8443;
        e.UriScheme = "https";
    })
    .WithEndpoint("admin", e =>
    {
        e.Port = 9000;
        e.TargetPort = 9000;
        e.UriScheme = "http";
    })
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
