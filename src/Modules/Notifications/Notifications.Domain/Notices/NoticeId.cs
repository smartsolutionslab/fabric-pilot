namespace FabricPilot.Modules.Notifications.Domain.Notices;

public sealed record NoticeId(Guid Value)
{
    public static NoticeId New() => new(Guid.CreateVersion7());
}
