namespace FabricPilot.Modules.Identity.Application.DTOs;

/// <summary>A tenant's user, with the tenant roles they hold (customer-user, customer-admin).</summary>
public sealed record TenantUserDto(Guid Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles);
