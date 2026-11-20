namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

public sealed record InvalidEmail(int Maximum)
    : DomainError("identity.invalid-email", $"A user needs an email address of at most {Maximum} characters.");
