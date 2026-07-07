using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Domain.FlashSales;
using PonPon.Modules.Catalog.Infrastructure.Persistence;
using PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;
using PonPon.Modules.Catalog.Tests;

var unitTests = new (string Name, Action Run)[]
{
    (nameof(ZortProductSnapshotComparerTests.SameSnapshotWithDifferentJsonPropertyOrderIsUnchanged),
        new ZortProductSnapshotComparerTests().SameSnapshotWithDifferentJsonPropertyOrderIsUnchanged),
    (nameof(ZortProductSnapshotComparerTests.ChangedStockIsChanged),
        new ZortProductSnapshotComparerTests().ChangedStockIsChanged),
    (nameof(ZortProductTagMatcherTests.ArrayContainingLineliffMatches),
        new ZortProductTagMatcherTests().ArrayContainingLineliffMatches),
    (nameof(ZortProductTagMatcherTests.ArrayWithoutLineliffDoesNotMatch),
        new ZortProductTagMatcherTests().ArrayWithoutLineliffDoesNotMatch),
    (nameof(ZortProductTagMatcherTests.StringLineliffMatches),
        new ZortProductTagMatcherTests().StringLineliffMatches),
    (nameof(ZortProductTagMatcherTests.NullTagDoesNotMatch),
        new ZortProductTagMatcherTests().NullTagDoesNotMatch),
    (nameof(ZortProductTagMatcherTests.LowercaseLineliffMatches),
        new ZortProductTagMatcherTests().LowercaseLineliffMatches)
};
foreach (var test in unitTests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

var connection = Environment.GetEnvironmentVariable("PONPON_INTEGRATION_DB");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.WriteLine("SKIP FlashSaleQuotaConcurrency (set PONPON_INTEGRATION_DB to run PostgreSQL integration tests)");
    return;
}

var options = new DbContextOptionsBuilder<CatalogDbContext>()
    .UseNpgsql(connection, x => x.MigrationsHistoryTable("__ef_migrations_history", CatalogDbContext.Schema))
    .Options;
await using (var setup = new CatalogDbContext(options))
    await setup.Database.MigrateAsync();

var productId = Guid.NewGuid();
var sale = FlashSale.Create(
    $"Concurrency {Guid.NewGuid():N}", DateOnly.FromDateTime(DateTime.UtcNow),
    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), [],
    [(productId, 10m, 5)], DateTime.UtcNow);
await using (var setup = new CatalogDbContext(options))
{
    setup.FlashSales.Add(sale);
    await setup.SaveChangesAsync();
}

try
{
    var attempts = Enumerable.Range(0, 20).Select(async _ =>
    {
        await using var db = new CatalogDbContext(options);
        return await new FlashSaleRepository(db).TryReserveQuotaAsync(
            Guid.NewGuid(), sale.Id, new Dictionary<Guid, int> { [productId] = 1 }, DateTime.UtcNow);
    });
    var accepted = (await Task.WhenAll(attempts)).Count(x => x);
    if (accepted != 5)
        throw new InvalidOperationException($"Expected exactly 5 flash-sale reservations, got {accepted}.");
    Console.WriteLine("PASS FlashSaleQuotaConcurrency");
}
finally
{
    await using var cleanup = new CatalogDbContext(options);
    await cleanup.FlashSaleReservations.Where(x => x.FlashSaleId == sale.Id).ExecuteDeleteAsync();
    await cleanup.FlashSaleProducts.Where(x => x.FlashSaleId == sale.Id).ExecuteDeleteAsync();
    await cleanup.FlashSales.Where(x => x.Id == sale.Id).ExecuteDeleteAsync();
}
