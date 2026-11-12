namespace FabricPilot.Agents.Tests;

/// <summary>A value object whose public constructor guards its value, as one built from JSON would.</summary>
public sealed record ShelfLabel
{
    public ShelfLabel(string value)
    {
        Ensure.That(value).IsNotNullOrWhiteSpace();

        Value = value;
    }

    public string Value { get; }
}
