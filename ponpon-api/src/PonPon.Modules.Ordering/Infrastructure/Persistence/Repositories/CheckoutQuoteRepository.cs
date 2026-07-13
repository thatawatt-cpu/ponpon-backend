using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Quotes;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;

public sealed class CheckoutQuoteRepository(OrderingDbContext dbContext) : ICheckoutQuoteRepository
{
    public async Task AddAsync(CheckoutQuote quote, CancellationToken cancellationToken = default)
    {
        await dbContext.CheckoutQuotes.AddAsync(quote, cancellationToken);
    }

    public Task<CheckoutQuote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.CheckoutQuotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
