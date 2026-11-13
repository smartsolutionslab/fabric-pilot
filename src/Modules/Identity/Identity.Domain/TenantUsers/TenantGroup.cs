namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

/// <summary>
/// A tenant's group in Keycloak, <c>/tenants/&lt;tenant&gt;</c>: membership of it is what makes a user the tenant's.
/// </summary>
public sealed record TenantGroup
{
    private const string Prefix = "/tenants/";

    private TenantGroup(string tenantId)
    {
        TenantId = tenantId;
        Path = Prefix + tenantId;
    }

    public string TenantId { get; }

    public string Path { get; }

    /// <summary>None for an id that is empty, a dot segment, or holds a separator, any of which could name another group.</summary>
    public static Option<TenantGroup> For(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || tenantId is "." or ".." || tenantId.Contains('/', StringComparison.Ordinal) || tenantId.Contains('\\', StringComparison.Ordinal))
        {
            return Option<TenantGroup>.None;
        }

        return Option<TenantGroup>.Some(new TenantGroup(tenantId));
    }
}
