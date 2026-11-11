using System.Text.Json.Serialization;

namespace FabricPilot.Agents.Tests;

[JsonConverter(typeof(JsonStringEnumConverter<Status>))]
public enum Status
{
    Open,
    Closed,
}
