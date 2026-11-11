namespace FabricPilot.Agents.Tests;

/// <summary>A value object without a constructor JSON can call, as many of a module's are: its factory validates.</summary>
public sealed record ShelfCode
{
    private ShelfCode(string value) => Value = value;

    public string Value { get; }

    public static ShelfCode From(string value)
    {
        Ensure.That(value).IsNotNullOrWhiteSpace();

        return new ShelfCode(value);
    }
}
