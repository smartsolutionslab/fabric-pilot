var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "0.8.7-pg18");
// Aspire places the data volume by the major version in the image tag, which pgvector's tag does not carry; PostgreSQL 18
// keeps its data under /var/lib/postgresql and refuses to start over a mount at the older /var/lib/postgresql/data.
var database = postgres
    .WithVolume(VolumeNameGenerator.Generate(postgres, "data"), "/var/lib/postgresql")
    .AddDatabase("database");
var migrations = builder.AddProject<Projects.MigrationRunner>("migrations")
    .WithReference(database)
    .WaitFor(database);
// Keycloak runs as a plain container, development only (see docs/adr). Its administrator is "admin", with a password
// Aspire generates once and keeps in the AppHost's user secrets; the realm in ./realms is imported on first start.
var keycloakPassword = builder.AddResource(ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, "keycloak-password"));
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.6")
    .WithArgs("start-dev", "--import-realm")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakPassword)
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithHttpEndpoint(port: 8080, targetPort: 8080)
    .WithHttpEndpoint(targetPort: 9000, name: "management")
    .WithHttpHealthCheck("/health/ready", endpointName: "management")
    .WithBindMount("./realms", "/opt/keycloak/data/import", isReadOnly: true);
keycloak.WithVolume(VolumeNameGenerator.Generate(keycloak, "data"), "/opt/keycloak/data");

if (builder.ExecutionContext.IsRunMode)
{
    // Keycloak's email (verification, reset password) lands in Mailpit, whose inbox is http://localhost:8025; the realm points SMTP at it.
    var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "v1.33")
        .WithHttpEndpoint(port: 8025, targetPort: 8025)
        .WithEndpoint(targetPort: 1025, name: "smtp")
        .WithHttpHealthCheck("/livez");
    keycloak.WaitFor(mailpit);
}
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume();
// Models deployed in your own Azure OpenAI resource, through its OpenAI v1 endpoint; nothing is provisioned. The endpoint
// (https://<resource>.openai.azure.com) has no default, the key is a secret (dotnet user-secrets set
// Parameters:azure-openai-api-key ...), and the deployment must name one in that resource (gpt-5 for harder questions).
var azureOpenAIEndpoint = builder.AddParameter("azure-openai-endpoint");
var azureOpenAIDeployment = builder.AddParameter("azure-openai-deployment", "gpt-5-mini", publishValueAsDefault: true);
var azureOpenAIApiKey = builder.AddParameter("azure-openai-api-key", secret: true);
// The embedding deployment must name one in that resource: text-embedding-3-small (1536 dimensions) by default. A model
// of another size needs vector tables of that size.
var azureOpenAIEmbeddingDeployment = builder.AddParameter("azure-openai-embedding-deployment", "text-embedding-3-small", publishValueAsDefault: true);
// The agent panel's stand-in model, for tests: only the tests set it to true, and the host takes it in Development only,
// answering every agent with a stand-in instead of the provider's model.
var modelStandIn = builder.AddParameter("model-stand-in", () => builder.Configuration["Parameters:model-stand-in"] ?? "false");

var host = builder.AddProject<Projects.Host>("host")
    .WithHttpHealthCheck("/health")
    .WaitForCompletion(migrations)
    .WithReference(database)
    .WaitFor(database)
    .WithReference(keycloak.GetEndpoint("http"))
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/fabric-pilot"))
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("AzureOpenAI__Endpoint", azureOpenAIEndpoint)
    .WithEnvironment("AzureOpenAI__Deployment", azureOpenAIDeployment)
    .WithEnvironment("AzureOpenAI__ApiKey", azureOpenAIApiKey)
    .WithEnvironment("AzureOpenAI__EmbeddingDeployment", azureOpenAIEmbeddingDeployment)
    .WithEnvironment("Agents__ModelStandIn", modelStandIn);

builder.AddViteApp("web", "../../web/app")
    .WithPnpm()
    // OIDC redirect URIs name this port; the dev server proxies /api to the host.
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithReference(host)
    .WaitFor(host)
    .WaitFor(keycloak)
    .WithEnvironment("VITE_OIDC_AUTHORITY", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/fabric-pilot"));

builder.Build().Run();
