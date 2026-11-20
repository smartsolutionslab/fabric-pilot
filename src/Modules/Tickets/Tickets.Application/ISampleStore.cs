using FabricPilot.Modules.Tickets.Domain.Samples;

namespace FabricPilot.Modules.Tickets.Application;

/// <summary>
/// The module's samples; implemented in its Infrastructure project. What a command adds is saved when it commits the
/// module's unit of work.
/// </summary>
public interface ISampleStore
{
    void Add(Sample sample);

    /// <summary>The sample as it is stored, for a query: nothing that changes it is saved.</summary>
    Task<Option<Sample>> FindAsync(SampleId id, CancellationToken cancellationToken);

    /// <summary>One page of the samples, by name.</summary>
    Task<PagedResult<Sample>> ListAsync(Page page, PageSize pageSize, CancellationToken cancellationToken);
}
