using System.Net;
using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Application;

/// <summary>Everything an identity use case refuses, each with its own code.</summary>
public static class IdentityFailures
{
    /// <summary>A rule the request broke: the domain's code, answered as 422.</summary>
    public static ApiError From(TenantUserError error) => error switch
    {
        RoleNotGrantable roleNotGrantable => ApiError.From(roleNotGrantable, HttpStatusCode.UnprocessableEntity),
        InvalidEmail invalidEmail => ApiError.From(invalidEmail, HttpStatusCode.UnprocessableEntity),
        InvalidName invalidName => ApiError.From(invalidName, HttpStatusCode.UnprocessableEntity),
    };

    public static ApiError NoTenant() => new NoTenant();

    public static ApiError UserAlreadyExists() => new UserAlreadyExists();

    /// <summary>The same answer for a user who does not exist and one the caller may not manage, so nobody probes other tenants' ids.</summary>
    public static ApiError UserNotFound(Guid user) => new UserNotFound(user);

    public static ApiError CannotChangeOwnRole() => new CannotChangeOwnRole();

    public static ApiError TenantNotFound(string tenant) => new TenantNotFound(tenant);

    public static ApiError InvitationNotSent(Guid user) => new InvitationNotSent(user);
}
