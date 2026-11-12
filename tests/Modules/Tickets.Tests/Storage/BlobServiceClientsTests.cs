using FabricPilot.Modules.Tickets.Infrastructure.Storage;

namespace FabricPilot.Modules.Tickets.Tests.Storage;

/// <summary>The two forms Aspire gives the blobs connection: a connection string (Azurite's), and a deployed account's blob endpoint.</summary>
public class BlobServiceClientsTests
{
    [Fact]
    public void A_connection_string_makes_a_client_for_its_account_and_blob_endpoint()
    {
        const string connection = "UseDevelopmentStorage=true";

        var client = BlobServiceClients.Create(connection);

        client.AccountName.ShouldBe("devstoreaccount1");
        client.Uri.ShouldBe(new Uri("http://127.0.0.1:10000/devstoreaccount1"));
    }

    [Fact]
    public void A_blob_endpoint_makes_a_client_for_that_endpoint()
    {
        var client = BlobServiceClients.Create("https://kundenportal.blob.core.windows.net/");

        client.Uri.ShouldBe(new Uri("https://kundenportal.blob.core.windows.net/"));
    }
}
