using FabricPilot.BuildingBlocks.Dashboards;
using FabricPilot.BuildingBlocks.Identity;
using FabricPilot.BuildingBlocks.Modules;

namespace Microsoft.Extensions.DependencyInjection;

public static class DashboardWidgetsRegistration
{
    /// <summary>
    /// The widgets every module declares in its <see cref="IModuleWidgets"/>, and the current user they are run for. A
    /// widget that is not a valid declared module query stops the host, naming it.
    /// </summary>
    public static IServiceCollection AddDashboardWidgets(this IServiceCollection services, IReadOnlyList<IModule> modules)
    {
        Ensure.That(services).IsNotNull();
        Ensure.That(modules).IsNotNull();

        List<WidgetScan> scans = [new WidgetScan([], WidgetCatalog.Misplaced(typeof(IModuleWidgets).Assembly))];
        foreach (var module in modules)
        {
            var api = module.GetType().Assembly;
            var name = api.GetName().Name!;
            name = name[..name.LastIndexOf('.')];
            var application = ModuleAssemblies.Of(module, "Application");
            scans.Add(WidgetCatalog.Scan(name[(name.LastIndexOf('.') + 1)..], application, api));
            scans.Add(new WidgetScan([], [.. WidgetCatalog.Misplaced(api), .. WidgetCatalog.Misplaced(ModuleAssemblies.Of(module, "Infrastructure"))]));
        }

        List<string> problems = [.. scans.SelectMany(scan => scan.Problems)];
        if (problems.Count > 0)
        {
            throw new InvalidOperationException($"The modules declare widgets that cannot be on a dashboard:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }

        services.AddHttpContextAccessor();
        services.AddAuthorizationCore();
        return services
            .AddSingleton(new WidgetCatalog([.. scans.SelectMany(scan => scan.Widgets)]))
            .AddScoped<IWidgetAccess, WidgetAccess>()
            .AddScoped<ICurrentUser, HttpCurrentUser>();
    }
}
