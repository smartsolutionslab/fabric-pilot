using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Application.DTOs;

public static class TenantUserMappings
{
    public static TenantUserDto ToDto(this TenantUser user)
    {
        Ensure.That(user).IsNotNull();
        return new TenantUserDto(user.Id.Value, user.Email, user.FirstName, user.LastName, [.. user.TenantRoles.Select(role => role.Name)]);
    }
}
