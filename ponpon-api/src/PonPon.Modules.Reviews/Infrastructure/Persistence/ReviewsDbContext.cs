using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Reviews.Domain;

namespace PonPon.Modules.Reviews.Infrastructure.Persistence;

public sealed class ReviewsDbContext : DbContext
{
    public const string Schema = "reviews";

    public ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : base(options)
    {
    }

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewMedia> ReviewMedia => Set<ReviewMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);
    }
}
