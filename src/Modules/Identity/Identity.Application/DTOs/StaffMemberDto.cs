namespace FabricPilot.Modules.Identity.Application.DTOs;

/// <summary>A staff member: the user name a ticket is assigned to, and their name to show.</summary>
public sealed record StaffMemberDto(string UserName, string FirstName, string LastName);
