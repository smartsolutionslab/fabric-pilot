using System.Net;
using FabricPilot.Modules.Identity.Application;
using FabricPilot.Modules.Identity.Application.Queries.ListTenantUsers;
using FabricPilot.Modules.Identity.Domain.TenantUsers;

namespace FabricPilot.Modules.Identity.Tests.Queries;

public class ListTenantUsersTests
{
    private static readonly string[] TenantA = ["/tenants/tenant-a"];

    [Fact]
    public async Task The_callers_tenants_members_are_paged_with_their_tenant_roles()
    {
        var first = DirectoryUsers.User(DirectoryUsers.NewId(), TenantA, "customer-admin", "customer-user", "manager");
        var second = DirectoryUsers.User(DirectoryUsers.NewId(), TenantA, "customer-user");
        var third = DirectoryUsers.User(DirectoryUsers.NewId(), TenantA);
        var directory = DirectoryUsers.Knowing(first, second, third);
        directory.ListMembersAsync(Arg.Is<TenantGroup>(group => group.Path == "/tenants/tenant-a"), Arg.Any<CancellationToken>())
            .Returns([first.Id, second.Id, third.Id]);

        var firstPage = await Handle(directory, "tenant-a", new ListTenantUsers(Page.First, Size(2)));
        var secondPage = await Handle(directory, "tenant-a", new ListTenantUsers(PageNumber(2), Size(2)));

        firstPage.IsSuccess.ShouldBeTrue();
        firstPage.Value.TotalCount.ShouldBe(3);
        firstPage.Value.Page.ShouldBe(1);
        firstPage.Value.PageSize.ShouldBe(2);
        firstPage.Value.Items.Select(user => user.Id).ShouldBe([first.Id.Value, second.Id.Value]);
        firstPage.Value.Items[0].Email.ShouldBe(first.Email);
        firstPage.Value.Items[0].Roles.ShouldBe(["customer-user", "customer-admin"]);
        firstPage.Value.Items[1].Roles.ShouldBe(["customer-user"]);
        secondPage.Value.Items.ShouldHaveSingleItem().Roles.ShouldBeEmpty();
        secondPage.Value.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Without_a_tenant_the_caller_is_refused_and_nothing_reaches_the_directory()
    {
        var directory = DirectoryUsers.Knowing();

        var result = await Handle(directory, null, new ListTenantUsers(Page.First, PageSize.Default));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("identity.no-tenant");
        result.Error.Status.ShouldBe(HttpStatusCode.Forbidden);
        directory.ReceivedCalls().ShouldBeEmpty();
    }

    private static Task<Result<PagedResult<FabricPilot.Modules.Identity.Application.DTOs.TenantUserDto>, ApiError>> Handle(ITenantUserDirectory directory, string? tenantId, ListTenantUsers query) =>
        new ListTenantUsersHandler(DirectoryUsers.Tenant(tenantId), directory).HandleAsync(query, TestContext.Current.CancellationToken);

    private static PageSize Size(int value) => PageSize.TryFrom(value, out var size) ? size : throw new InvalidOperationException();

    private static Page PageNumber(int value) => Page.TryFrom(value, out var page) ? page : throw new InvalidOperationException();
}
