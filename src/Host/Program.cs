using FabricPilot.BuildingBlocks.Modules;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var modules = ModuleDiscovery.Discover(AppContext.BaseDirectory);
foreach (var module in modules)
{
    module.AddServices(builder.Services, builder.Configuration);
}

builder.Services.AddOpenApi();
builder.Services.AddKeycloakAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddProblemDetails();
builder.Services.AddDispatcher();
builder.Services.AddTenancy();
builder.Services.AddMessaging(builder.Configuration, modules);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) => TypedResults.Ok(new FabricPilot.Host.CurrentUser(user.FindFirst("preferred_username")?.Value)))
    .RequireAuthorization();
app.MapGet("/me/tenant", (FabricPilot.BuildingBlocks.Tenancy.ITenantContext tenant) => TypedResults.Ok(new FabricPilot.Host.CurrentTenant(tenant.TenantId)))
    .RequireAuthorization();
foreach (var module in modules)
{
    module.MapEndpoints(app);
}

app.Run();
