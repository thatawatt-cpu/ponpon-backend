using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.RefreshTokens;

namespace PonPon.Modules.Identity.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public RefreshTokenRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
}
