using System.Net;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// The knowledge module's sample policy knowledge.manage requires the realm role manager and the claim
/// department=finance: developer holds both, clerk only the role, analyst only the claim.
/// </summary>
public class KnowledgeClaimTests(AspireFixture aspire)
{
    private static readonly Uri ManagePermission = new("/knowledge/permissions/manage", UriKind.Relative);

    [Fact]
    public async Task A_user_with_the_manager_role_in_the_finance_department_is_allowed()
    {
        using var client = await aspire.CreateHostClientAsync("developer");

        using var response = await client.GetAsync(ManagePermission, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_user_with_only_the_manager_role_is_forbidden()
    {
        using var client = await aspire.CreateHostClientAsync("clerk");

        using var response = await client.GetAsync(ManagePermission, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_user_with_only_the_finance_department_is_forbidden()
    {
        using var client = await aspire.CreateHostClientAsync("analyst");

        using var response = await client.GetAsync(ManagePermission, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_user_with_neither_is_forbidden()
    {
        using var client = await aspire.CreateHostClientAsync("visitor");

        using var response = await client.GetAsync(ManagePermission, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
