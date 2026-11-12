using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Tests.Contracts;

public class ContractTests
{
    private static readonly DateOnly StartsOn = new(2026, 1, 1);

    private static readonly DateOnly EndsOn = new(2026, 12, 31);

    [Fact]
    public void A_contract_is_concluded_with_its_product_start_and_end()
    {
        var id = ContractId.New();

        var result = Contract.Conclude(id, "  Managed printing  ", StartsOn, EndsOn, ContractRenewal.Renews);

        result.IsSuccess.ShouldBeTrue();
        var contract = result.Value;
        contract.Id.ShouldBe(id);
        contract.Product.ShouldBe("Managed printing");
        contract.StartsOn.ShouldBe(StartsOn);
        contract.EndsOn.ShouldBe(EndsOn);
    }

    [Fact]
    public void A_contract_that_renews_renews_on_the_day_after_its_last_day()
    {
        var contract = Contract.Conclude(ContractId.New(), "Managed printing", StartsOn, EndsOn, ContractRenewal.Renews).Value;

        contract.RenewsOn.ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public void A_contract_that_ends_without_renewing_has_no_renewal_date()
    {
        var contract = Contract.Conclude(ContractId.New(), "Managed printing", StartsOn, EndsOn, ContractRenewal.Ends).Value;

        contract.EndsOn.ShouldBe(EndsOn);
        contract.RenewsOn.ShouldBeNull();
    }

    [Theory]
    [InlineData("2026-09-01")]
    [InlineData("2026-12-31")]
    [InlineData("2027-01-01")]
    public void A_contract_that_ends_without_renewing_is_never_up_for_renewal(string today)
    {
        var contract = Contract.Conclude(ContractId.New(), "Managed printing", StartsOn, EndsOn, ContractRenewal.Ends).Value;

        contract.IsUpForRenewal(DateOnly.Parse(today, System.Globalization.CultureInfo.InvariantCulture)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(-91, false)]
    [InlineData(-90, true)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void A_contract_that_renews_is_up_for_renewal_from_the_window_before_its_renewal_date_to_that_date(int daysFromRenewal, bool expected)
    {
        var contract = Contract.Conclude(ContractId.New(), "Managed printing", StartsOn, EndsOn, ContractRenewal.Renews).Value;
        var renewsOn = new DateOnly(2027, 1, 1);

        contract.IsUpForRenewal(renewsOn.AddDays(daysFromRenewal)).ShouldBe(expected);
    }

    [Fact]
    public void The_renewal_window_is_ninety_days()
    {
        Contract.RenewalWindowDays.ShouldBe(90);
    }

    [Fact]
    public void A_contract_of_a_single_day_is_concluded()
    {
        var result = Contract.Conclude(ContractId.New(), "Workshop", StartsOn, StartsOn, ContractRenewal.Ends);

        result.IsSuccess.ShouldBeTrue();
        result.Value.EndsOn.ShouldBe(StartsOn);
    }

    [Fact]
    public void A_contract_that_ends_before_it_starts_is_refused()
    {
        var result = Contract.Conclude(ContractId.New(), "Managed printing", EndsOn, StartsOn, ContractRenewal.Renews);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<EndsBeforeItStarts>().Code.ShouldBe("contracts.ends-before-it-starts");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_contract_without_a_product_is_refused(string product)
    {
        var result = Contract.Conclude(ContractId.New(), product, StartsOn, EndsOn, ContractRenewal.Renews);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<InvalidProduct>().Code.ShouldBe("contracts.invalid-product");
    }

    [Fact]
    public void A_product_of_the_maximum_length_is_accepted_and_one_longer_is_refused()
    {
        Contract.Conclude(ContractId.New(), new string('p', Contract.MaximumProductLength), StartsOn, EndsOn, ContractRenewal.Renews).IsSuccess.ShouldBeTrue();

        var result = Contract.Conclude(ContractId.New(), new string('p', Contract.MaximumProductLength + 1), StartsOn, EndsOn, ContractRenewal.Renews);

        result.IsFailure.ShouldBeTrue();
        result.Error.Value.ShouldBeOfType<InvalidProduct>();
    }
}
