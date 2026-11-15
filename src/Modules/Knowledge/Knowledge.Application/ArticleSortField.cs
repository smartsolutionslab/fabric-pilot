using System.Text.Json.Serialization;

namespace FabricPilot.Modules.Knowledge.Application;

/// <summary>What an article list can be sorted by.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ArticleSortField>))]
public enum ArticleSortField
{
    ChangedAt,
    Title,
}
