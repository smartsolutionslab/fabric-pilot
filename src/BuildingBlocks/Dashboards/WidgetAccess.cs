using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace FabricPilot.BuildingBlocks.Dashboards;

/// <summary>
/// A widget's policy, checked for the user of the current request as an endpoint naming it would. A widget's query runs
/// through the dispatcher, not through the endpoint that may guard the same query, so the widget carries the policy.
/// </summary>
public sealed class WidgetAccess(IHttpContextAccessor accessor, IAuthorizationService authorization) : IWidgetAccess
{
    public async Task<bool> AllowsAsync(WidgetDefinition widget, CancellationToken cancellationToken)
    {
        Ensure.That(widget).IsNotNull();

        if (!widget.Policy.HasValue)
        {
            return true;
        }

        var policy = widget.Policy.Value;

        var user = accessor.HttpContext?.User;
        return user is not null && (await authorization.AuthorizeAsync(user, policy)).Succeeded;
    }
}
