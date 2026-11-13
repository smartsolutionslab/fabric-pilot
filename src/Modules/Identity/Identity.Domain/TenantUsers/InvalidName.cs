namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

public sealed record InvalidName(int Maximum)
    : DomainError("identity.invalid-name", $"A user needs a first and a last name of 1 to {Maximum} characters.");
