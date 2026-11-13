using FabricPilot.Modules.Notifications.Domain.Notices;
using FabricPilot.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FabricPilot.Modules.Notifications.Tests.Persistence;

public class NoticeConfigurationTests
{
    /// <summary>Two deliveries that read the same unclaimed notice cannot both save their claim: the second save conflicts.</summary>
    [Fact]
    public void A_notice_is_saved_only_over_the_row_version_it_was_read_with()
    {
        using var context = new NotificationsDbContextFactory().CreateDbContext([]);

        var notice = context.Model.FindEntityType(typeof(Notice)).ShouldNotBeNull();

        notice.GetProperties().ShouldContain(property => property.IsConcurrencyToken && property.GetColumnName() == "xmin");
    }
}
