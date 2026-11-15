namespace FabricPilot.Contracts.Identity;

/// <summary>A staff member: the user name they sign in with, which is what a ticket is assigned to, and their name.</summary>
public sealed record StaffMember(string UserName, string FirstName, string LastName);
