using PonPon.Modules.Ordering.Domain.Quotes;

namespace PonPon.Modules.Ordering.Application.Abstractions;

public interface ICheckoutQuoteRepository
{
    Task AddAsync(CheckoutQuote quote, CancellationToken cancellationToken = default);
    Task<CheckoutQuote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
