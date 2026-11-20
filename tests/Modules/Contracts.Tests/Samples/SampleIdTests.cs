using FabricPilot.Modules.Contracts.Domain.Samples;

namespace FabricPilot.Modules.Contracts.Tests.Samples;

public class SampleIdTests
{
    [Fact]
    public void A_new_identifier_is_a_version_7_guid() =>
        SampleId.New().Value.Version.ShouldBe(7);

    [Fact]
    public void An_identifier_comes_back_from_its_value()
    {
        var identifier = SampleId.New();

        SampleId.From(identifier.Value).ShouldBe(identifier);
        identifier.ToString().ShouldBe(identifier.Value.ToString());
    }

    [Fact]
    public void An_empty_guid_is_not_an_identifier() =>
        Should.Throw<GuardException>(() => SampleId.From(Guid.Empty));
}
