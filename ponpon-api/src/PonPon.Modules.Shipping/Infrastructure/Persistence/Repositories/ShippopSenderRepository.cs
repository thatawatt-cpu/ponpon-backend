using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Domain;

namespace PonPon.Modules.Shipping.Infrastructure.Persistence.Repositories;

public sealed class ShippopSenderRepository : IShippopSenderRepository
{
    private readonly ShippingDbContext _dbContext;

    public ShippopSenderRepository(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ShippopSender?> GetAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.ShippopSenders
            .FirstOrDefaultAsync(x => x.Id == ShippopSender.DefaultId, cancellationToken);
    }

    public async Task AddAsync(
        ShippopSender sender,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ShippopSenders.AddAsync(sender, cancellationToken);
    }
}
