using System.Security.Claims;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Api.Endpoints;

/// <summary>The signed-in customer as a ticket records them: the token's subject, which Keycloak sets to the user's id, and e-mail.</summary>
internal static class CustomerClaims
{
    public static TicketCustomer Of(ClaimsPrincipal user)
    {
        Ensure.That(user).IsNotNull();

        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("The caller's token names no subject or no e-mail address; the realm gives every customer both.");
        }

        return new TicketCustomer(subject, email);
    }
}
