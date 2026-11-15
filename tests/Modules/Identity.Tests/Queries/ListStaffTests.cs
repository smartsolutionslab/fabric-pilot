using FabricPilot.Contracts.Identity;
using FabricPilot.Modules.Identity.Application.DTOs;
using FabricPilot.Modules.Identity.Application.Queries.ListStaff;

namespace FabricPilot.Modules.Identity.Tests.Queries;

public class ListStaffTests
{
    private static readonly StaffMember Ada = new("ada", "Ada", "Lovelace");
    private static readonly StaffMember Grace = new("grace", "Grace", "Hopper");
    private static readonly StaffMember Linus = new("linus", "Linus", "Torvalds");

    private readonly IStaffDirectory staff = Substitute.For<IStaffDirectory>();

    public ListStaffTests()
    {
        staff.ListAsync(Arg.Any<CancellationToken>()).Returns([Ada, Grace, Linus]);
    }

    [Fact]
    public async Task Staff_list_every_staff_member_with_their_user_name_and_name()
    {
        var result = await Handle(new ListStaff(Page.First, PageSize.Default));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBe([new StaffMemberDto("ada", "Ada", "Lovelace"), new StaffMemberDto("grace", "Grace", "Hopper"), new StaffMemberDto("linus", "Linus", "Torvalds")]);
        result.Value.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task A_page_holds_its_share_and_counts_every_staff_member()
    {
        Page.TryFrom(2, out var second).ShouldBeTrue();
        PageSize.TryFrom(2, out var two).ShouldBeTrue();

        var result = await Handle(new ListStaff(second, two));

        result.Value.Items.Select(member => member.UserName).ShouldBe(["linus"]);
        result.Value.TotalCount.ShouldBe(3);
        result.Value.Page.ShouldBe(2);
        result.Value.PageSize.ShouldBe(2);
    }

    private Task<Result<PagedResult<StaffMemberDto>, ApiError>> Handle(ListStaff query) =>
        new ListStaffHandler(staff).HandleAsync(query, TestContext.Current.CancellationToken);
}
