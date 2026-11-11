using System.Text.Json;
using System.Text.Json.Serialization;

namespace FabricPilot.BuildingBlocks.Agents;

/// <summary>Reads and writes every <see cref="Option{T}"/>, wherever it is nested, with <see cref="OptionJsonConverter{T}"/>.</summary>
internal sealed class OptionJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        Ensure.That(typeToConvert).IsNotNull();

        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Option<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Ensure.That(typeToConvert).IsNotNull();

        return (JsonConverter)Activator.CreateInstance(typeof(OptionJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
    }
}
