using System.Net.Http.Json;
using FabricPilot.BuildingBlocks.Tenancy;
using FabricPilot.Contracts.Identity;
using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Infrastructure.Keycloak;

/// <summary>
/// The staff role's members through Keycloak's admin API, as the service account fabric-pilot-staff-reader, which holds
/// realm-management's query-users and view-realm and nothing else: listing a realm role's members needs both
/// (docs/adr/0053). It reads; it never writes.
/// </summary>
public sealed partial class KeycloakStaffDirectory(HttpClient http, ILogger<KeycloakStaffDirectory> logger) : IStaffDirectory
{
    /// <summary>The most members the list reads; Keycloak pages a role's members but does not count them.</summary>
    private const int MostMembers = 1000;

    public async Task<IReadOnlyList<StaffMember>> ListAsync(CancellationToken cancellationToken)
    {
        var members = await http.GetFromJsonAsync<UserRepresentation[]>(
            new Uri($"roles/{Uri.EscapeDataString(CrossTenantRole.Name)}/users?first=0&max={MostMembers}&briefRepresentation=true", UriKind.Relative),
            cancellationToken) ?? [];
        if (members.Length >= MostMembers)
        {
            LogMembersCut(logger, MostMembers);
        }

        return [.. members
            .OrderBy(member => member.Username, StringComparer.Ordinal)
            .Select(member => new StaffMember(member.Username, member.FirstName ?? string.Empty, member.LastName ?? string.Empty))];
    }

    public async Task<bool> IsStaffAsync(string userName, CancellationToken cancellationToken)
    {
        Ensure.That(userName).IsNotNull();

        return (await ListAsync(cancellationToken)).Any(member => member.UserName == userName);
    }

    [LoggerMessage(EventId = 20, Level = LogLevel.Warning, Message = "The staff role has at least {Most} members; only the first {Most} are listed")]
    private static partial void LogMembersCut(ILogger logger, int most);

    private sealed record UserRepresentation(string Username, string? FirstName, string? LastName);
}
