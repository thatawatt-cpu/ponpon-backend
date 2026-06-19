using PonPon.Shared.Infrastructure.Messaging;

namespace PonPon.Shared.Application.Abstractions;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
