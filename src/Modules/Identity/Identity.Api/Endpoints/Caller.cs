using System.Security.Claims;

namespace FabricPilot.Modules.Identity.Api.Endpoints;

/// <summary>The signed-in caller's user id: the token's subject, which Keycloak sets to the user's id.</summary>
internal static class Caller
{
    public static Guid Of(ClaimsPrincipal user)
    {
        Ensure.That(user).IsNotNull();

        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(subject, out var id) ? id : throw new InvalidOperationException("The caller's token names no subject.");
    }
}
