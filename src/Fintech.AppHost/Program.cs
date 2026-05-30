var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("investmentdb", "investment_demo");

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "25.0")
    .WithArgs("start-dev", "--import-realm")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
    .WithBindMount("../../infra/keycloak", "/opt/keycloak/data/import", isReadOnly: true)
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http");

var api = builder.AddProject<Projects.Fintech_Api>("api")
    .WithReference(postgres)
    .WithEnvironment("Auth__Mode", "Development")
    .WithEnvironment("Auth__Authority", "http://localhost:8080/realms/fintech-demo")
    .WithEnvironment("Auth__Audience", "investment-api")
    .WaitFor(postgres)
    .WaitFor(keycloak);

builder.AddProject<Projects.Fintech_Web>("web")
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
