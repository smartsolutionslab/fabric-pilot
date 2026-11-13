using System.Linq.Expressions;
using FabricPilot.Modules.Notifications.Domain.Notices;

namespace FabricPilot.Modules.Notifications.Application.DTOs;

public static class NoticeMappings
{
    /// <summary>For an EF <c>Select</c>.</summary>
    public static Expression<Func<Notice, NoticeDto>> ToNoticeDtoExpression() =>
        notice => new NoticeDto(notice.Id.Value, notice.TicketId, notice.Text, notice.CreatedAt);
}
