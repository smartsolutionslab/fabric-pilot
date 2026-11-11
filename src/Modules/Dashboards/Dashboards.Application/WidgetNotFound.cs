using System.Net;

namespace FabricPilot.Modules.Dashboards.Application;

public sealed record WidgetNotFound(string Id) : ApiError("dashboards.widget-not-found", $"{Id} is not a widget the modules declare for you.", HttpStatusCode.NotFound);
