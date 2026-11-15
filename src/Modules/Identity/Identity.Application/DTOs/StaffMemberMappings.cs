using FabricPilot.Contracts.Identity;

namespace FabricPilot.Modules.Identity.Application.DTOs;

public static class StaffMemberMappings
{
    public static StaffMemberDto ToDto(this StaffMember member)
    {
        Ensure.That(member).IsNotNull();
        return new StaffMemberDto(member.UserName, member.FirstName, member.LastName);
    }
}
