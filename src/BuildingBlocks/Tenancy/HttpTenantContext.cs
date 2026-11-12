using Microsoft.AspNetCore.Http;

namespace FabricPilot.BuildingBlocks.Tenancy;

/// <summary>An explicit <see cref="TenantScope"/> first; otherwise the user's one group under /tenants/.</summary>
public sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    private const string TenantGroupPrefix = "/tenants/";

    /// <summary>
    /// The scope's, where one chose it: none inside <see cref="TenantScope.AllTenants"/> or
    /// <see cref="TenantScope.SharedRecords"/>, whose work belongs to no tenant, and none for an event without one.
    /// </summary>
    public string? TenantId => TenantScope.ChoosesTheTenant ? TenantScope.TenantId : FromToken();

    public bool ReadsAllTenants => TenantScope.ReadsAllTenants;

    public bool WritesSharedRecords => TenantScope.WritesSharedRecords;

    private string? FromToken()
    {
        var tenants = accessor.HttpContext?.User.FindAll("groups")
            .Select(claim => claim.Value)
            .Where(group => group.StartsWith(TenantGroupPrefix, StringComparison.Ordinal))
            .Select(group => group[TenantGroupPrefix.Length..])
            .ToList();

        return tenants is [var tenant] ? tenant : null;
    }
}
