using System.Globalization;
using System.Net;
using FabricPilot.Modules.Contracts.Application.Commands.RecordContract;
using FabricPilot.Modules.Contracts.Domain.Contracts;

namespace FabricPilot.Modules.Contracts.Tests.Commands;

public class RecordContractTests
{
    private readonly InMemoryContractStore contracts = new();
    private readonly RecordingUnitOfWork unitOfWork;

    public RecordContractTests()
    {
        unitOfWork = new RecordingUnitOfWork(contracts);
    }

    [Fact]
    public async Task A_renewing_contract_is_saved_with_the_day_after_its_last_day_as_its_renewal_date()
    {
        var result = await Record(new RecordContract("Managed printing", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), Renews: true));

        result.IsSuccess.ShouldBeTrue();
        var contract = contracts.Saved.ShouldHaveSingleItem();
        contract.Id.Value.ShouldBe(result.Value);
        contract.Product.ShouldBe("Managed printing");
        contract.StartsOn.ShouldBe(new DateOnly(2026, 1, 1));
        contract.EndsOn.ShouldBe(new DateOnly(2026, 12, 31));
        contract.RenewsOn.ShouldBe(new DateOnly(2027, 1, 1));
        unitOfWork.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task A_contract_that_ends_without_renewing_is_saved_without_a_renewal_date()
    {
        var result = await Record(new RecordContract("Migration project", new DateOnly(2026, 3, 1), new DateOnly(2026, 8, 31), Renews: false));

        result.IsSuccess.ShouldBeTrue();
        var contract = contracts.Saved.ShouldHaveSingleItem();
        contract.EndsOn.ShouldBe(new DateOnly(2026, 8, 31));
        contract.RenewsOn.ShouldBeNull();
    }

    [Theory]
    [InlineData(" ", "2026-01-01", "2026-12-31", "contracts.invalid-product")]
    [InlineData("Hosting", "2026-12-31", "2026-01-01", "contracts.ends-before-it-starts")]
    public async Task A_contract_that_breaks_a_rule_is_refused_with_the_domains_code_as_422_and_nothing_is_saved(string product, string startsOn, string endsOn, string code)
    {
        var result = await Record(new RecordContract(product, DateOnly.Parse(startsOn, CultureInfo.InvariantCulture), DateOnly.Parse(endsOn, CultureInfo.InvariantCulture), Renews: true));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(code);
        result.Error.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        contracts.Saved.ShouldBeEmpty();
        unitOfWork.Commits.ShouldBe(0);
    }

    private Task<Result<Guid, ApiError>> Record(RecordContract command) =>
        new RecordContractHandler(contracts, unitOfWork).HandleAsync(command, TestContext.Current.CancellationToken);
}
