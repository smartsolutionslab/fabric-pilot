using System.Net;

namespace FabricPilot.Modules.Dashboards.Application;

public sealed record TooManyWidgets(int Maximum) : ApiError("dashboards.too-many-widgets", $"A dashboard holds at most {Maximum} widgets.", HttpStatusCode.BadRequest);
