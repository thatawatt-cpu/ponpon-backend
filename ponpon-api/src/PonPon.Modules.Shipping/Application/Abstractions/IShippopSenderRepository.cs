using PonPon.Modules.Shipping.Domain;

namespace PonPon.Modules.Shipping.Application.Abstractions;

public interface IShippopSenderRepository
{
    Task<ShippopSender?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ShippopSender sender, CancellationToken cancellationToken = default);
}
