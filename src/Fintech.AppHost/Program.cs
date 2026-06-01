var builder = DistributedApplication.CreateBuilder(args);

// Без WithDataVolume: БД эфемерная и при каждом запуске инициализируется
// с актуальным сгенерированным паролем. Это убирает рассинхрон пароля со
// старым томом (Postgres задаёт пароль только при первой инициализации).
// POSTGRES_DB заставляет контейнер создать базу investment_demo при init,
// иначе ресурс БД не станет healthy и api повиснет на WaitFor.
// Для персистентности верни .WithDataVolume() и один раз удали старый том.
var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_DB", "investment_demo")
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
