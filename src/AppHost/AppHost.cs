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

// Resource API: Weather
var weather = builder.AddProject<Projects.Weather_API>("Weather")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health");

// API Gateway (BFF + YARP + OIDC)
var gateway = builder.AddProject<Projects.Gateway_API>("Gateway")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(weather)
    .WaitFor(weather)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Front End only Interacts with the API Gateway, and Resources are Proxied through the API Gateway with YARP
#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var webfrontend = builder.AddViteApp("WebFrontEnd", "../frontend")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithHttpsEndpoint(env: "PORT", port: 54955)
    .WithHttpsDeveloperCertificate();
//#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

gateway.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
