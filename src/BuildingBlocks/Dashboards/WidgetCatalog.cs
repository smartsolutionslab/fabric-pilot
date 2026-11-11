using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using FabricPilot.BuildingBlocks.Messaging;
using FabricPilot.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FabricPilot.BuildingBlocks.Dashboards;

/// <summary>
/// Every widget the modules declare, and nothing else: a dashboard can name and run only these. <see cref="Scan"/> is the
/// one definition of a valid widget; the host refuses to start on a problem it finds, and the architecture rule
/// EveryWidgetIsADeclaredModuleQuery fails the build on the same problems.
/// </summary>
public sealed class WidgetCatalog
{
    private const string ApplicationSuffix = ".Application";

    private readonly Dictionary<string, WidgetDefinition> byId;

    public WidgetCatalog(IReadOnlyList<WidgetDefinition> widgets)
    {
        Ensure.That(widgets).IsNotNull();
        Widgets = [.. widgets.OrderBy(widget => widget.Id, StringComparer.Ordinal)];
        byId = Widgets.ToDictionary(widget => widget.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<WidgetDefinition> Widgets { get; }

    public bool TryFind(string id, [NotNullWhen(true)] out WidgetDefinition? widget) => byId.TryGetValue(id, out widget);

    /// <summary>The widgets one module's Application assembly declares, and what is wrong with any of them.</summary>
    /// <param name="module">The module's name, e.g. <c>BillingFind</c>; its widgets' ids start with <c>billing-find.</c>.</param>
    /// <param name="application">The assembly holding the module's <see cref="IModuleWidgets"/>, queries and handlers.</param>
    /// <param name="api">The assembly holding the module's <see cref="IModulePolicies"/>.</param>
    public static WidgetScan Scan(string module, Assembly application, Assembly api)
    {
        Ensure.That(module).IsNotNullOrWhiteSpace();
        Ensure.That(application).IsNotNull();
        Ensure.That(api).IsNotNull();

        var types = application.GetTypes();
        var builder = new WidgetCatalogBuilder(KebabCase(module));
        foreach (var declaration in types
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IModuleWidgets).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            ((IModuleWidgets)Activator.CreateInstance(declaration)!).Declare(builder);
        }

        var policies = Policies(api);
        List<string> problems = [];
        foreach (var widget in builder.Widgets)
        {
            if (widget.Query.Assembly != application)
            {
                problems.Add($"The widget {widget.Id} runs {widget.Query.FullName}, which is not a query of {application.GetName().Name}: a widget runs a query of its own module.");
                continue;
            }

            var handler = typeof(IQueryHandler<,>).MakeGenericType(widget.Query, widget.Result);
            var handlers = types.Count(candidate => candidate is { IsClass: true, IsAbstract: false } && handler.IsAssignableFrom(candidate));
            if (handlers != 1)
            {
                problems.Add($"The widget {widget.Id} runs {widget.Query.FullName}, which has {handlers} handlers in {application.GetName().Name}: a widget's query needs exactly one.");
            }

            if (widget.Policy.HasValue && policies.GetPolicy(widget.Policy.Value) is null)
            {
                problems.Add($"The widget {widget.Id} requires the policy {widget.Policy.Value}, which the module {module} does not register.");
            }
        }

        problems.AddRange(builder.Widgets
            .GroupBy(widget => widget.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"The module {module} declares the widget {group.Key} {group.Count()} times: one id names one widget."));
        return new WidgetScan(builder.Widgets, problems);
    }

    /// <summary>Every <see cref="IModuleWidgets"/> in an assembly that is not a module's Application project.</summary>
    public static IReadOnlyList<string> Misplaced(Assembly assembly)
    {
        Ensure.That(assembly).IsNotNull();

        var name = assembly.GetName().Name!;
        if (name.EndsWith(ApplicationSuffix, StringComparison.Ordinal))
        {
            return [];
        }

        return [.. assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IModuleWidgets).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => $"{type.FullName} declares widgets in {name}: a module declares its widgets, and their queries, in its Application project.")];
    }

    /// <summary>The policies the module's own <see cref="IModulePolicies"/> register, and no other module's.</summary>
    private static AuthorizationOptions Policies(Assembly api)
    {
        var services = new ServiceCollection();
        var builder = new AuthorizationBuilder(services.AddAuthorizationCore());
        foreach (var registration in api.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IModulePolicies).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => (IModulePolicies)Activator.CreateInstance(type)!))
        {
            registration.AddPolicies(builder);
        }

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
    }

    /// <summary><c>BillingFind</c> becomes <c>billing-find</c>.</summary>
    private static string KebabCase(string name)
    {
        var kebab = new StringBuilder();
        foreach (var character in name)
        {
            if (char.IsUpper(character) && kebab.Length > 0)
            {
                kebab.Append('-');
            }

            kebab.Append(char.ToLowerInvariant(character));
        }

        return kebab.ToString();
    }
}
