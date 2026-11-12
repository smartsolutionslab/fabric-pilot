using System.Net;

namespace FabricPilot.Integration.Tests;

/// <summary>The knowledge module's sample policy knowledge.manage requires the realm role manager (and the claim department=finance), which developer holds and visitor does not.</summary>
public class KnowledgeRoleTests(AspireFixture aspire)
{
    private static readonly Uri ManagePermission = new("/knowledge/permissions/manage", UriKind.Relative);

    [Fact]
    public async Task A_user_with_the_manager_role_is_allowed()
    {
        using var client = await aspire.CreateHostClientAsync("developer");

        using var response = await client.GetAsync(ManagePermission, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_signed_in_user_without_the_role_is_forbidden()
    {
        using var client = await aspire.CreateHostClientAsync("visitor");

        using var response = await client.GetAsync(ManagePermission, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized()
    {
        using var client = aspire.CreateHostClient();

        using var response = await client.GetAsync(ManagePermission, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
