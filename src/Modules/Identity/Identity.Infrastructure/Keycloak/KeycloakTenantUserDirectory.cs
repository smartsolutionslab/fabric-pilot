using System.Net;
using System.Net.Http.Json;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Domain.TenantUsers;
using Microsoft.Extensions.Logging;

namespace FabricPilot.Modules.Identity.Infrastructure.Keycloak;

/// <summary>
/// The realm's users through Keycloak's admin REST API, as the service account fabric-pilot-user-admin, which holds
/// realm-management's manage-users and nothing else. The only code that knows the API's paths and shapes. Its POSTs
/// get one attempt (the standard resilience handler retries only idempotent methods).
/// </summary>
public sealed partial class KeycloakTenantUserDirectory(HttpClient http, ILogger<KeycloakTenantUserDirectory> logger) : ITenantUserDirectory
{
    /// <summary>The most members a list reads; Keycloak pages members but does not count them.</summary>
    private const int MostMembers = 1000;

    private static readonly string[] InvitationActions = ["VERIFY_EMAIL", "UPDATE_PASSWORD"];

    public async Task<bool> TenantExistsAsync(TenantGroup group, CancellationToken cancellationToken) =>
        (await FindGroupAsync(group, cancellationToken)).HasValue;

    public async Task<Option<TenantUserId>> CreateAsync(TenantGroup group, NewTenantUser user, CancellationToken cancellationToken)
    {
        Ensure.That(group).IsNotNull();
        Ensure.That(user).IsNotNull();

        var representation = new NewUserRepresentation(user.Email, user.Email, user.FirstName, user.LastName, Enabled: true, EmailVerified: false, [group.Path], InvitationActions);
        using var response = await http.PostAsJsonAsync(new Uri("users", UriKind.Relative), representation, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return Option<TenantUserId>.None;
        }

        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location ?? throw new InvalidOperationException("Keycloak created a user without answering where.");
        return Option<TenantUserId>.Some(new TenantUserId(Guid.Parse(location.Segments[^1])));
    }

    public async Task<Option<TenantUser>> FindAsync(TenantUserId id, CancellationToken cancellationToken)
    {
        Ensure.That(id).IsNotNull();

        using var response = await http.GetAsync(UserPath(id, string.Empty), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Option<TenantUser>.None;
        }

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserRepresentation>(cancellationToken)
            ?? throw new InvalidOperationException($"Keycloak answered user {id.Value} without a body.");
        var groups = await http.GetFromJsonAsync<GroupRepresentation[]>(UserPath(id, "/groups"), cancellationToken) ?? [];
        var roles = await http.GetFromJsonAsync<RoleRepresentation[]>(UserPath(id, "/role-mappings/realm/composite"), cancellationToken) ?? [];
        return Option<TenantUser>.Some(new TenantUser(
            id,
            user.Email ?? user.Username,
            user.FirstName ?? string.Empty,
            user.LastName ?? string.Empty,
            [.. groups.Select(group => group.Path)],
            [.. roles.Select(role => role.Name)]));
    }

    public async Task<IReadOnlyList<TenantUserId>> ListMembersAsync(TenantGroup group, CancellationToken cancellationToken)
    {
        var found = await FindGroupAsync(group, cancellationToken);
        if (!found.HasValue)
        {
            return [];
        }

        var members = await http.GetFromJsonAsync<UserRepresentation[]>(
            new Uri($"groups/{found.Value.Id}/members?first=0&max={MostMembers}&briefRepresentation=true", UriKind.Relative),
            cancellationToken) ?? [];
        if (members.Length >= MostMembers)
        {
            LogMembersCut(logger, group.TenantId, MostMembers);
        }

        return [.. members.OrderBy(member => member.Username, StringComparer.Ordinal).Select(member => new TenantUserId(Guid.Parse(member.Id)))];
    }

    public async Task SetRoleAsync(TenantUserId id, TenantRole role, CancellationToken cancellationToken)
    {
        Ensure.That(id).IsNotNull();
        Ensure.That(role).IsNotNull();

        // Read through the user's mappings: reading a role by name would need the service account to view the realm.
        var mapped = await http.GetFromJsonAsync<RoleRepresentation[]>(UserPath(id, "/role-mappings/realm"), cancellationToken) ?? [];
        if (!mapped.Any(candidate => candidate.Name == role.Name))
        {
            var available = await http.GetFromJsonAsync<RoleRepresentation[]>(UserPath(id, "/role-mappings/realm/available"), cancellationToken) ?? [];
            var grant = available.SingleOrDefault(candidate => candidate.Name == role.Name)
                ?? throw new InvalidOperationException($"The realm has no role {role.Name} to grant.");
            using var granted = await http.PostAsJsonAsync(UserPath(id, "/role-mappings/realm"), new[] { grant }, cancellationToken);
            granted.EnsureSuccessStatusCode();
        }

        RoleRepresentation[] previous = [.. mapped.Where(candidate => candidate.Name != role.Name && TenantRole.All.Any(tenantRole => tenantRole.Name == candidate.Name))];
        if (previous.Length > 0)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, UserPath(id, "/role-mappings/realm")) { Content = JsonContent.Create(previous) };
            using var removed = await http.SendAsync(request, cancellationToken);
            removed.EnsureSuccessStatusCode();
        }
    }

    public async Task<bool> SendInvitationAsync(TenantUserId id, CancellationToken cancellationToken)
    {
        Ensure.That(id).IsNotNull();

        try
        {
            using var response = await http.PutAsJsonAsync(UserPath(id, "/execute-actions-email"), InvitationActions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            LogInvitationRefused(logger, id.Value, (int)response.StatusCode);
            return false;
        }
        catch (HttpRequestException exception)
        {
            LogInvitationFailed(logger, exception, id.Value);
            return false;
        }
    }

    public async Task DeleteAsync(TenantUserId id, CancellationToken cancellationToken)
    {
        Ensure.That(id).IsNotNull();

        using var response = await http.DeleteAsync(UserPath(id, string.Empty), cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private static Uri UserPath(TenantUserId id, string rest) => new($"users/{id.Value}{rest}", UriKind.Relative);

    private async Task<Option<GroupRepresentation>> FindGroupAsync(TenantGroup group, CancellationToken cancellationToken)
    {
        Ensure.That(group).IsNotNull();

        var path = string.Join('/', group.Path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
        using var response = await http.GetAsync(new Uri($"group-by-path/{path}", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Option<GroupRepresentation>.None;
        }

        response.EnsureSuccessStatusCode();
        var found = await response.Content.ReadFromJsonAsync<GroupRepresentation>(cancellationToken);
        return found is not null && found.Path == group.Path ? Option<GroupRepresentation>.Some(found) : Option<GroupRepresentation>.None;
    }

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Tenant {Tenant} has at least {Most} members; only the first {Most} are listed")]
    private static partial void LogMembersCut(ILogger logger, string tenant, int most);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Keycloak refused to send the invitation to user {User} with status {Status}")]
    private static partial void LogInvitationRefused(ILogger logger, Guid user, int status);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Keycloak could not be reached to send the invitation to user {User}")]
    private static partial void LogInvitationFailed(ILogger logger, Exception exception, Guid user);

    private sealed record NewUserRepresentation(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        bool Enabled,
        bool EmailVerified,
        IReadOnlyList<string> Groups,
        IReadOnlyList<string> RequiredActions);

    private sealed record UserRepresentation(string Id, string Username, string? Email, string? FirstName, string? LastName);

    private sealed record GroupRepresentation(string Id, string Path);

    private sealed record RoleRepresentation(string Id, string Name);
}
