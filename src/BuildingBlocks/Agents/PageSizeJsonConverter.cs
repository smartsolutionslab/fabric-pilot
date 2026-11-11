using System.Text.Json;
using System.Text.Json.Serialization;

namespace FabricPilot.BuildingBlocks.Agents;

/// <summary>
/// A tool's page size as the whole number a list endpoint's <c>?pageSize=</c> is, read through
/// <see cref="PageSize.TryFrom"/>: no tool call reads more than <see cref="PageSize.Maximum"/> items at once.
/// </summary>
internal sealed class PageSizeJsonConverter : JsonConverter<PageSize>
{
    public override PageSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number) && PageSize.TryFrom(number, out var pageSize))
        {
            return pageSize;
        }

        throw new JsonException($"A page size is a whole number from 1 to {PageSize.Maximum}.");
    }

    public override void Write(Utf8JsonWriter writer, PageSize value, JsonSerializerOptions options)
    {
        Ensure.That(writer).IsNotNull();
        Ensure.That(value).IsNotNull();

        writer.WriteNumberValue(value.Value);
    }
}
