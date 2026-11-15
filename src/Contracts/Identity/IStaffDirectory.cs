namespace FabricPilot.Contracts.Identity;

/// <summary>
/// The realm's staff, the users holding the role staff: what another module asks the identity module, which owns the
/// identity provider, when it offers or checks an assignee. Take it by constructor; the identity module registers it.
/// An unreachable identity provider throws.
/// </summary>
public interface IStaffDirectory
{
    /// <summary>Every staff member, ordered by user name.</summary>
    Task<IReadOnlyList<StaffMember>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Whether a staff member has exactly this user name.</summary>
    Task<bool> IsStaffAsync(string userName, CancellationToken cancellationToken);
}
