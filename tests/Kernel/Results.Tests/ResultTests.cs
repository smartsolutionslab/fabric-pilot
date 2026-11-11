using System.Globalization;
using System.Net;

namespace FabricPilot.Kernel.Tests;

public class ResultTests
{
    private static readonly ApiError NotFound = new InvoiceNotFound();

    [Fact]
    public void A_success_has_its_value_and_no_error()
    {
        Result<int, ApiError> result = Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void A_failure_has_its_error_and_no_value()
    {
        Result<int, ApiError> result = Failure(NotFound);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(NotFound);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Match_takes_the_branch_of_the_outcome()
    {
        Result<int, ApiError>.Success(42).Match(value => value.ToString(CultureInfo.InvariantCulture), error => error.Code).ShouldBe("42");
        Result<int, ApiError>.Failure(NotFound).Match(value => value.ToString(CultureInfo.InvariantCulture), error => error.Code).ShouldBe("billing.invoice-not-found");
    }

    [Fact]
    public void Match_needs_both_branches() =>
        Should.Throw<ArgumentException>(() => Result<int, ApiError>.Success(42).Match(null!, error => error.Code));

    [Fact]
    public void Map_error_translates_a_failure_and_keeps_a_success()
    {
        Result<int, DomainError>.Success(42).MapError(error => ApiError.From(error, HttpStatusCode.Conflict)).Value.ShouldBe(42);
        Result<int, DomainError>.Failure(new InvoiceOverdue()).MapError(error => ApiError.From(error, HttpStatusCode.Conflict)).Error.Code.ShouldBe("billing.invoice-overdue");
    }

    [Fact]
    public void A_domain_error_becomes_an_api_error_with_the_status_application_gives_it()
    {
        var overdue = new InvoiceOverdue();

        var error = ApiError.From(overdue, HttpStatusCode.Conflict);

        error.Code.ShouldBe("billing.invoice-overdue");
        error.Message.ShouldBe("The invoice is overdue.");
        error.Status.ShouldBe(HttpStatusCode.Conflict);
        error.ShouldBeOfType<DomainFailure>().Error.ShouldBe(overdue);
        Unit.Value.ShouldBe(default(Unit));
    }

    [Fact]
    public void A_switch_takes_the_case_of_the_outcome()
    {
        Describe(Success(42)).ShouldBe("42");
        Describe(Failure(NotFound)).ShouldBe("billing.invoice-not-found");
    }

    private static string Describe(Result<int, ApiError> result) =>
        result switch
        {
            SuccessOutcome<int> success => success.Value.ToString(CultureInfo.InvariantCulture),
            FailureOutcome<ApiError> failure => failure.Error.Code,
        };

    private sealed record InvoiceNotFound() : ApiError("billing.invoice-not-found", "No such invoice.", HttpStatusCode.NotFound);

    private sealed record InvoiceOverdue() : DomainError("billing.invoice-overdue", "The invoice is overdue.");
}
