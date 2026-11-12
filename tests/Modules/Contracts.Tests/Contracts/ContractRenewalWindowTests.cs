using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Tests.Contracts;

/// <summary>The renewal window a query selects by is the domain's own: it agrees with <see cref="Contract.IsUpForRenewal"/> day for day.</summary>
public class ContractRenewalWindowTests
{
    private static readonly DateOnly Today = new(2026, 11, 12);

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(Contract.RenewalWindowDays, true)]
    [InlineData(Contract.RenewalWindowDays + 1, false)]
    public void A_query_selects_a_renewing_contract_exactly_when_it_is_up_for_renewal(int daysUntilRenewal, bool expected)
    {
        var contract = Renewing(Today.AddDays(daysUntilRenewal));

        var selected = Contract.UpForRenewal(Today).Compile()(contract);

        selected.ShouldBe(expected);
        selected.ShouldBe(contract.IsUpForRenewal(Today));
    }

    [Fact]
    public void A_query_never_selects_a_contract_that_ends_without_renewing()
    {
        var contract = Contract.Conclude(ContractId.New(), "Migration project", Today.AddDays(-30), Today.AddDays(10), ContractRenewal.Ends).Value;

        Contract.UpForRenewal(Today).Compile()(contract).ShouldBeFalse();
    }

    private static Contract Renewing(DateOnly renewsOn) =>
        Contract.Conclude(ContractId.New(), "Managed printing", renewsOn.AddYears(-1), renewsOn.AddDays(-1), ContractRenewal.Renews).Value;
}
