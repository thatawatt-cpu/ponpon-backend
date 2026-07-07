using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

namespace PonPon.Modules.Catalog.Application.Abstractions;

public interface IZortProductClient
{
    Task<ZortGetWarehousesResponse> GetWarehousesAsync(int page = 1, int limit = 500, CancellationToken cancellationToken = default);
    Task<ZortProductDto> GetProductByIdAsync(long zortProductId, CancellationToken cancellationToken = default);
    Task<ZortGetCategoriesResponse> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ZortGetProductsResponse> GetProductsAsync(int page, int limit, CancellationToken cancellationToken = default);
    Task<ZortApiResponse> UpdateProductStockListAsync(ZortStockUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ZortApiResponse> IncreaseProductStockListAsync(ZortStockUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ZortApiResponse> DecreaseProductStockListAsync(ZortStockUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ZortApiResponse> UpdateProductAvailableStockListAsync(ZortStockUpdateRequest request, CancellationToken cancellationToken = default);
}
