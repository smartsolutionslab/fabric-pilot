using FabricPilot.BuildingBlocks.Modules;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var modules = ModuleDiscovery.Discover(AppContext.BaseDirectory);
foreach (var module in modules)
{
    module.AddServices(builder.Services, builder.Configuration);
}

builder.Services.AddOpenApi(options => options.DescribeAsOpenApi31().DescribePaging());
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddKeycloakRoles();
builder.Services.AddUniqueConstraintResponses();
builder.Services.AddErrorResponses();
builder.Services.AddAzureOpenAIChatClient(builder.Configuration);
builder.Services.AddAzureOpenAIEmbeddingGenerator(builder.Configuration);
builder.Services.AddModuleVectorIndexes(builder.Configuration);
builder.Services.AddDispatcher();
builder.Services.AddTenancy();
builder.Services.AddMessaging(builder.Configuration, modules);
builder.Services.AddDashboardWidgets(modules);
builder.Services.AddAgentUi(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    // The frontend's client is generated from the document without signing in.
    app.MapOpenApi().AllowAnonymous();
}

app.MapDefaultEndpoints();
app.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) => TypedResults.Ok(new FabricPilot.Host.CurrentUser(user.FindFirst("preferred_username")?.Value)))
    .RequireAuthorization();
app.MapGet("/me/tenant", (FabricPilot.BuildingBlocks.Tenancy.ITenantContext tenant) => TypedResults.Ok(new FabricPilot.Host.CurrentTenant(tenant.TenantId)))
    .RequireAuthorization();
app.MapAgentUi();
foreach (var module in modules)
{
    module.MapEndpoints(app);
}

app.Run();
