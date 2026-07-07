using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Categories;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Domain.SyncRuns;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;

using System.Text.Json;

namespace PonPon.Modules.Catalog.Application.Features.Products.SyncProductsFromZort;

public sealed class SyncProductsFromZortHandler
{
    private readonly IZortProductClient _zortClient;
    private readonly IProductRepository _products;
    private readonly IProductSyncRunRepository _syncRuns;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public SyncProductsFromZortHandler(
        IZortProductClient zortClient,
        IProductRepository products,
        IProductSyncRunRepository syncRuns,
        ICatalogUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _zortClient = zortClient;
        _products = products;
        _syncRuns = syncRuns;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<SyncProductsFromZortResponse> HandleAsync(SyncProductsFromZortCommand command, CancellationToken cancellationToken = default)
    {
        var syncRun = command.SyncRunId.HasValue
            ? await _syncRuns.GetByIdAsync(command.SyncRunId.Value, cancellationToken)
            : null;

        if (syncRun is null)
        {
            syncRun = ProductSyncRun.Queue(_clock.UtcNow);
            await _syncRuns.AddAsync(syncRun, cancellationToken);
        }

        syncRun.MarkRunning(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await ExecuteAsync(command, cancellationToken);
            syncRun.MarkCompleted(
                result.TotalFetched,
                result.Created,
                result.Updated,
                result.Unchanged,
                result.Deactivated,
                result.Failed,
                result.Errors,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            syncRun.MarkFailed(ex.Message, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<SyncProductsFromZortResponse> ExecuteAsync(
        SyncProductsFromZortCommand command,
        CancellationToken cancellationToken)
    {
        await SyncCategoriesFromZortAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var page = Math.Max(command.PageStart, 1);
        var pageLimit = Math.Clamp(command.PageLimit, 1, 500);
        var pagesProcessed = 0;
        var totalFetched = 0;
        var created = 0;
        var updated = 0;
        var unchanged = 0;
        var failed = 0;
        var errors = new List<string>();
        var seenZortProductIds = new HashSet<long>();

        while (command.MaxPages is null || pagesProcessed < command.MaxPages.Value)
        {
            var response = await _zortClient.GetProductsAsync(page, pageLimit, cancellationToken);
            if (response.Products.Count == 0)
            {
                break;
            }

            var snapshots = response.Products
                .Where(ZortProductTagMatcher.HasLiffTag)
                .Select(ToSnapshotResult)
                .ToArray();

            failed += snapshots.Count(x => x.Error is not null);
            errors.AddRange(snapshots.Where(x => x.Error is not null).Select(x => x.Error!));
            var validSnapshots = snapshots.Where(x => x.Snapshot is not null).Select(x => x.Snapshot!).ToArray();

            await EnsureCategoriesAsync(validSnapshots, cancellationToken);

            var pageZortProductIds = validSnapshots
                .Select(x => x.ZortProductId)
                .OfType<long>()
                .ToHashSet();
            var pageBaseSkus = validSnapshots
                .Select(x => Product.ParseSku(x.Sku).BaseSku)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.Ordinal);

            seenZortProductIds.UnionWith(pageZortProductIds);

            var existingProducts = await _products.GetByBaseSkusWithVariantsAsync(pageBaseSkus, cancellationToken);
            var productsByBaseSku = existingProducts
                .Where(x => !string.IsNullOrWhiteSpace(x.BaseSku))
                .GroupBy(x => x.BaseSku!, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);

            var newProducts = new List<Product>();
            foreach (var snapshot in validSnapshots)
            {
                try
                {
                    var parsedSku = Product.ParseSku(snapshot.Sku);
                    var product = productsByBaseSku.GetValueOrDefault(parsedSku.BaseSku);

                    if (product is null)
                    {
                        product = Product.CreateFromZort(snapshot, _clock.UtcNow);
                        product.UpsertVariant(snapshot, _clock.UtcNow);
                        newProducts.Add(product);
                        productsByBaseSku[product.BaseSku ?? parsedSku.BaseSku] = product;
                        created++;
                    }
                    else
                    {
                        var fullSku = string.IsNullOrWhiteSpace(snapshot.Sku) ? $"ZORT-{snapshot.ZortProductId}" : snapshot.Sku;
                        var existingVariant = product.Variants.FirstOrDefault(
                            x => x.Sku == fullSku || (snapshot.ZortProductId.HasValue && x.ZortProductId == snapshot.ZortProductId));

                        if (existingVariant is null || ZortProductSnapshotComparer.HasChanged(existingVariant, snapshot))
                        {
                            product.ApplyZortSnapshot(snapshot, _clock.UtcNow);
                            product.UpsertVariant(snapshot, _clock.UtcNow);
                            updated++;
                        }
                        else
                        {
                            unchanged++;
                        }
                    }

                    totalFetched++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add(ex.Message);
                }
            }

            if (newProducts.Count > 0)
            {
                await _products.AddRangeAsync(newProducts, cancellationToken);
            }

            pagesProcessed++;
            page++;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var deactivated = 0;
        if (command.DeactivateMissingProducts)
        {
            var missingProducts = await _products.GetLiffZortProductsNotSeenAsync(seenZortProductIds, cancellationToken);
            foreach (var product in missingProducts)
            {
                product.MarkMissingFromZort(_clock.UtcNow);
                deactivated++;
            }
        }

        if (deactivated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new SyncProductsFromZortResponse(totalFetched, created, updated, unchanged, deactivated, failed, errors);
    }

    private static SyncSnapshotResult ToSnapshotResult(ZortProductDto zortProduct)
    {
        try
        {
            return new SyncSnapshotResult(ZortProductMapper.ToSnapshot(zortProduct), null);
        }
        catch (Exception ex)
        {
            return new SyncSnapshotResult(null, ex.Message);
        }
    }

    private async Task SyncCategoriesFromZortAsync(CancellationToken cancellationToken)
    {
        var response = await _zortClient.GetCategoriesAsync(cancellationToken);
        foreach (var zortCategory in response.Categories)
        {
            var name = ReadCategoryName(zortCategory);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var zortCategoryId = ReadCategoryId(zortCategory);
            var category = await _products.GetCategoryAsync(zortCategoryId, name, cancellationToken);
            if (category is null)
            {
                await _products.AddCategoryAsync(new Category(zortCategoryId, name, _clock.UtcNow), cancellationToken);
                continue;
            }

            category.Update(name, ReadIsActive(zortCategory), _clock.UtcNow);
        }
    }

    private async Task EnsureCategoryAsync(ProductSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(snapshot.CategoryName))
        {
            return;
        }

        var category = await _products.GetCategoryAsync(snapshot.ZortCategoryId, snapshot.CategoryName, cancellationToken);
        if (category is null)
        {
            await _products.AddCategoryAsync(new Category(snapshot.ZortCategoryId, snapshot.CategoryName, _clock.UtcNow), cancellationToken);
            return;
        }

        category.Update(snapshot.CategoryName, true, _clock.UtcNow);
    }

    private async Task EnsureCategoriesAsync(IReadOnlyCollection<ProductSnapshot> snapshots, CancellationToken cancellationToken)
    {
        var categorySnapshots = snapshots
            .Where(x => !string.IsNullOrWhiteSpace(x.CategoryName))
            .GroupBy(x => x.ZortCategoryId?.ToString() ?? x.CategoryName!, StringComparer.Ordinal)
            .Select(x => x.First())
            .ToArray();

        if (categorySnapshots.Length == 0)
        {
            return;
        }

        var zortCategoryIds = categorySnapshots
            .Select(x => x.ZortCategoryId)
            .OfType<long>()
            .ToHashSet();
        var names = categorySnapshots
            .Select(x => x.CategoryName!)
            .ToHashSet(StringComparer.Ordinal);
        var existingCategories = await _products.GetCategoriesAsync(zortCategoryIds, names, cancellationToken);
        var categoriesByZortId = existingCategories
            .Where(x => x.ZortCategoryId.HasValue)
            .GroupBy(x => x.ZortCategoryId!.Value)
            .ToDictionary(x => x.Key, x => x.First());
        var categoriesByName = existingCategories
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);

        var newCategories = new List<Category>();
        foreach (var snapshot in categorySnapshots)
        {
            var category = snapshot.ZortCategoryId is long zortCategoryId
                ? categoriesByZortId.GetValueOrDefault(zortCategoryId)
                : null;

            if (category is null)
            {
                category = categoriesByName.GetValueOrDefault(snapshot.CategoryName!);
            }

            if (category is null)
            {
                var newCategory = new Category(snapshot.ZortCategoryId, snapshot.CategoryName!, _clock.UtcNow);
                newCategories.Add(newCategory);
                if (snapshot.ZortCategoryId is long id)
                {
                    categoriesByZortId[id] = newCategory;
                }

                categoriesByName[snapshot.CategoryName!] = newCategory;
                continue;
            }

            category.Update(snapshot.CategoryName!, true, _clock.UtcNow);
        }

        if (newCategories.Count > 0)
        {
            await _products.AddCategoriesAsync(newCategories, cancellationToken);
        }
    }

    private static long? ReadCategoryId(ZortCategoryDto category)
    {
        foreach (var element in new[] { category.Id, category.CategoryId })
        {
            var value = ReadLong(element);
            if (value.HasValue)
            {
                return value;
            }
        }

        if (category.Extra is not null)
        {
            foreach (var name in new[] { "categoryID", "catid", "catId" })
            {
                if (category.Extra.TryGetValue(name, out var element))
                {
                    var value = ReadLong(element);
                    if (value.HasValue)
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private static string? ReadCategoryName(ZortCategoryDto category)
    {
        return FirstNonEmpty(category.Name, category.Category, category.CategoryName)
            ?? ReadExtraString(category, "categoryName")
            ?? ReadExtraString(category, "catname")
            ?? ReadExtraString(category, "catName");
    }

    private static bool ReadIsActive(ZortCategoryDto category)
    {
        if (category.Active.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return true;
        }

        return category.Active.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when category.Active.TryGetInt32(out var number) => number != 0,
            JsonValueKind.String => !string.Equals(category.Active.GetString(), "false", StringComparison.OrdinalIgnoreCase)
                                    && category.Active.GetString() != "0",
            _ => true
        };
    }

    private static long? ReadLong(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string? ReadExtraString(ZortCategoryDto category, string name)
    {
        if (category.Extra is null || !category.Extra.TryGetValue(name, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String ? FirstNonEmpty(element.GetString()) : FirstNonEmpty(element.ToString());
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private sealed record SyncSnapshotResult(ProductSnapshot? Snapshot, string? Error);
}
