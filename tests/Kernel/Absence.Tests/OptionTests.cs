namespace FabricPilot.Kernel.Tests;

public class OptionTests
{
    private static readonly Option<string> Number = Option<string>.Some("INV-1");
    private static readonly Option<string> Nothing = Option<string>.None;

    [Fact]
    public void Some_has_its_value_and_none_has_none()
    {
        Number.HasValue.ShouldBeTrue();
        Number.Value.ShouldBe("INV-1");
        Nothing.HasValue.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => Nothing.Value);
    }

    [Fact]
    public void Some_refuses_null() =>
        Should.Throw<ArgumentNullException>(() => Option<string>.Some(null!));

    [Fact]
    public void Match_takes_the_branch_of_the_case_it_is()
    {
        Number.Match(number => number, () => "none").ShouldBe("INV-1");
        Nothing.Match(number => number, () => "none").ShouldBe("none");
    }

    [Fact]
    public void Map_changes_a_value_and_leaves_none_none()
    {
        Number.Map(number => number.Length).ShouldBe(Option<int>.Some(5));
        Nothing.Map(number => number.Length).ShouldBe(Option<int>.None);
    }

    [Fact]
    public void Get_or_default_falls_back_only_without_a_value()
    {
        Number.GetOrDefault("INV-0").ShouldBe("INV-1");
        Nothing.GetOrDefault("INV-0").ShouldBe("INV-0");
    }

    [Fact]
    public void Options_are_equal_when_both_are_none_or_both_hold_equal_values()
    {
        (Number == Option<string>.Some("INV-1")).ShouldBeTrue();
        (Nothing == Option<string>.None).ShouldBeTrue();
        (Number != Nothing).ShouldBeTrue();
        (Number != Option<string>.Some("INV-2")).ShouldBeTrue();
        Number.Equals((object)Option<string>.Some("INV-1")).ShouldBeTrue();
        Number.Equals("INV-1").ShouldBeFalse();
        Number.GetHashCode().ShouldBe(Option<string>.Some("INV-1").GetHashCode());
        Nothing.GetHashCode().ShouldBe(Option<string>.None.GetHashCode());
    }

    [Fact]
    public void An_option_says_what_it_holds()
    {
        Number.ToString().ShouldBe("Some(INV-1)");
        Nothing.ToString().ShouldBe("None");
    }

    [Fact]
    public void A_switch_takes_the_case_of_the_option_and_the_default_is_none()
    {
        Describe(Number).ShouldBe("INV-1");
        Describe(Nothing).ShouldBe("none");
        Describe(default).ShouldBe("none");
    }

    [Fact]
    public void Every_option_holds_one_of_its_cases_and_the_case_converts_back_to_the_option()
    {
        Option<string>.IUnionMembers number = Number;
        Option<string>.IUnionMembers nothing = Nothing;

        number.HasValue.ShouldBeTrue();
        nothing.HasValue.ShouldBeTrue();
        Option<string> numberAgain = number.Value.ShouldBeOfType<Some<string>>();
        Option<string> nothingAgain = nothing.Value.ShouldBeOfType<None>();
        numberAgain.ShouldBe(Number);
        nothingAgain.ShouldBe(Nothing);
    }

    private static string Describe(Option<string> number) =>
        number switch
        {
            Some<string> some => some.Value,
            None => "none",
        };
}
