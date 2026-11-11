using System.Net;

namespace FabricPilot.Modules.Dashboards.Application;

public sealed record InvalidWidget(string Key, string Reason) : ApiError("dashboards.invalid-widget", $"The widget {Key} {Reason}", HttpStatusCode.BadRequest);
