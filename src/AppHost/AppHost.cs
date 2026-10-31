var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("database");
var migrations = builder.AddProject<Projects.MigrationRunner>("migrations")
    .WithReference(database)
    .WaitFor(database);
#pragma warning disable ASPIREHOSTINGKEYCLOAK001, ASPIRECERTIFICATES001 // Preview APIs, development only (see docs/adr).
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithoutHttpsCertificate()
    .WithDataVolume()
    .WithRealmImport("./realms");
#pragma warning restore ASPIREHOSTINGKEYCLOAK001, ASPIRECERTIFICATES001
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume();

var host = builder.AddProject<Projects.Host>("host")
    .WithHttpHealthCheck("/health")
    .WaitForCompletion(migrations)
    .WithReference(database)
    .WaitFor(database)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/fabric-pilot"))
    .WithReference(messaging)
    .WaitFor(messaging);

builder.AddViteApp("web", "../../web/app")
    .WithPnpm()
    // OIDC redirect URIs name this port (src/AppHost/realms); the dev server proxies /api to the host.
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithReference(host)
    .WaitFor(host)
    .WaitFor(keycloak)
    .WithEnvironment("VITE_OIDC_AUTHORITY", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/fabric-pilot"));

builder.Build().Run();
