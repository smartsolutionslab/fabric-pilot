using FabricPilot.BuildingBlocks.Domain;

namespace FabricPilot.Modules.Contracts.Domain.Contracts;

/// <summary>A product a customer has under contract, from its first day to its last, and whether it renews then.</summary>
public sealed class Contract : AggregateRoot<ContractId>
{
    public const int MaximumProductLength = 200;

    /// <summary>How many days before its renewal date a contract is up for renewal.</summary>
    public const int RenewalWindowDays = 90;

    private Contract(ContractId id, string product, DateOnly startsOn, DateOnly endsOn, DateOnly? renewsOn)
        : base(id)
    {
        Product = product;
        StartsOn = startsOn;
        EndsOn = endsOn;
        RenewsOn = renewsOn;
    }

    public string Product { get; private set; }

    /// <summary>The contract's first day.</summary>
    public DateOnly StartsOn { get; private set; }

    /// <summary>The contract's last day.</summary>
    public DateOnly EndsOn { get; private set; }

    /// <summary>The first day of the next term, the day after <see cref="EndsOn"/>; empty for a contract that ends without renewing.</summary>
    public DateOnly? RenewsOn { get; private set; }

    public static Result<Contract, ContractError> Conclude(ContractId id, string product, DateOnly startsOn, DateOnly endsOn, ContractRenewal renewal)
    {
        Ensure.That(id).IsNotNull();
        Ensure.That(product).IsNotNull();
        Ensure.That((int)renewal, nameof(renewal)).Satisfies(value => Enum.IsDefined((ContractRenewal)value), $"{renewal} is not a contract renewal.");

        product = product.Trim();
        if (product.Length == 0 || product.Length > MaximumProductLength)
        {
            return Failure(ContractErrors.InvalidProduct(MaximumProductLength));
        }

        if (endsOn < startsOn)
        {
            return Failure(ContractErrors.EndsBeforeItStarts(startsOn, endsOn));
        }

        DateOnly? renewsOn = renewal == ContractRenewal.Renews ? endsOn.AddDays(1) : null;
        return Success(new Contract(id, product, startsOn, endsOn, renewsOn));
    }

    /// <summary>Whether the contract renews on a day from <paramref name="today"/> to <see cref="RenewalWindowDays"/> days later.</summary>
    public bool IsUpForRenewal(DateOnly today) =>
        RenewsOn is { } renewsOn && renewsOn >= today && renewsOn <= today.AddDays(RenewalWindowDays);
}
