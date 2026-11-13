using FabricPilot.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.RabbitMQ;

namespace FabricPilot.Integration.Tests;

/// <summary>
/// Publishes integration events onto the AppHost's broker as the tickets module would, with Wolverine's own routing and
/// format, for a module whose events no use case publishes yet (#7 changes a ticket's status). The test fills the event's
/// metadata itself, standing in for the tickets module's unit of work.
/// </summary>
public sealed class TicketsEventPublisher : IAsyncDisposable
{
    private readonly IHost host;

    private TicketsEventPublisher(IHost host) => this.host = host;

    public static async Task<TicketsEventPublisher> StartAsync(string brokerConnectionString, CancellationToken cancellationToken)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddWolverine(options =>
        {
            options.ServiceName = "integration-tests";
            options.Discovery.DisableConventionalDiscovery();
            options.UseRabbitMq(new Uri(brokerConnectionString)).AutoProvision().UseConventionalRouting();
        });
        var host = builder.Build();
        await host.StartAsync(cancellationToken);
        return new TicketsEventPublisher(host);
    }

    public async Task PublishAsync(IntegrationEvent integrationEvent)
    {
        await using var scope = host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(integrationEvent);
    }

    public async ValueTask DisposeAsync()
    {
        await host.StopAsync();
        host.Dispose();
    }
}
