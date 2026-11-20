using System.Net;

namespace FabricPilot.Modules.Identity.Application;

public sealed record InvitationNotSent(Guid User)
    : ApiError("identity.invitation-not-sent", $"The invitation to user {User} could not be sent; try again later.", HttpStatusCode.BadGateway);
