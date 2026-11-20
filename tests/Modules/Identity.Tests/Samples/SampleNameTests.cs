using FabricPilot.Modules.Identity.Domain.Samples;

namespace FabricPilot.Modules.Identity.Tests.Samples;

public class SampleNameTests
{
    [Fact]
    public void A_name_holds_its_value()
    {
        var name = SampleName.From("Ada");

        name.Value.ShouldBe("Ada");
        name.ToString().ShouldBe("Ada");
        name.ShouldBe(SampleName.From("Ada"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string value) =>
        Should.Throw<GuardException>(() => SampleName.From(value));

    [Fact]
    public void A_name_longer_than_the_maximum_is_refused() =>
        Should.Throw<GuardException>(() => SampleName.From(new string('a', SampleName.MaxLength + 1)));

    [Fact]
    public void A_callers_name_of_one_to_the_maximum_characters_becomes_a_name()
    {
        SampleName.TryFrom("Ada", out var name).ShouldBeTrue();
        name.ShouldBe(SampleName.From("Ada"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_callers_blank_name_is_no_name_and_no_exception(string? value)
    {
        SampleName.TryFrom(value, out var name).ShouldBeFalse();
        name.ShouldBeNull();
    }

    [Fact]
    public void A_callers_name_longer_than_the_maximum_is_no_name() =>
        SampleName.TryFrom(new string('a', SampleName.MaxLength + 1), out _).ShouldBeFalse();
}
