namespace FabricPilot.Modules.Identity.Domain.TenantUsers;

/// <summary>A user to invite: their email, lower-cased, is also their username.</summary>
public sealed record NewTenantUser
{
    /// <summary>The longest username, email and name the realm's user profile keeps.</summary>
    public const int MaximumLength = 255;

    private NewTenantUser(string email, string firstName, string lastName)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }

    public string Email { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public static Result<NewTenantUser, TenantUserError> Create(string email, string firstName, string lastName)
    {
        var address = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsEmail(address))
        {
            return Failure(TenantUserErrors.InvalidEmail(MaximumLength));
        }

        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();
        if (first.Length is 0 or > MaximumLength || last.Length is 0 or > MaximumLength)
        {
            return Failure(TenantUserErrors.InvalidName(MaximumLength));
        }

        return Success(new NewTenantUser(address, first, last));
    }

    private static bool IsEmail(string address)
    {
        var at = address.IndexOf('@', StringComparison.Ordinal);
        return address.Length <= MaximumLength
            && at > 0
            && at < address.Length - 1
            && at == address.LastIndexOf('@')
            && !address.Any(char.IsWhiteSpace);
    }
}
