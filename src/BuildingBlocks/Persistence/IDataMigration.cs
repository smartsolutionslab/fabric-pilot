namespace FabricPilot.BuildingBlocks.Persistence;

/// <summary>
/// A change to a module's data that its database migrations cannot make, such as renaming the module's blobs. The
/// MigrationRunner finds every one in the modules' Infrastructure projects and runs it after every module's
/// migrations, on every start, before the host starts: it must find nothing left to do once it has run. Its
/// constructor may take the module's DbContext, which sees no tenant there, and <c>IConfiguration</c>.
/// </summary>
public interface IDataMigration
{
    Task RunAsync(CancellationToken cancellationToken);
}
