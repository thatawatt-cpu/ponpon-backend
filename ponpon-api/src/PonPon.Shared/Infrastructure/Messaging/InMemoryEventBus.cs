using PonPon.Shared.Application.Abstractions;

namespace PonPon.Shared.Infrastructure.Messaging;

public sealed class InMemoryEventBus : IEventBus
{
    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
