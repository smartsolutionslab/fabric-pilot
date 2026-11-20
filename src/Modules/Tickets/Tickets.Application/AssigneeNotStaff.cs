using System.Net;

namespace FabricPilot.Modules.Tickets.Application;

public sealed record AssigneeNotStaff(string Assignee)
    : ApiError("tickets.assignee-not-staff", $"'{Assignee}' is not a staff member's user name; a ticket is assigned to staff only.", HttpStatusCode.UnprocessableEntity);
