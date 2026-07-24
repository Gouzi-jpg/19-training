using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductService(IProductRepository productRepository, IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var products = await _productRepository.GetLowStockAsync(threshold);
        var soldQuantities = await _orderRepository.GetSoldQuantitiesSinceAsync(since);

        return products
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockProduct(
                p.Sku,
                p.Name,
                p.StockQuantity,
                soldQuantities.TryGetValue(p.Id, out var sold) ? sold : 0))
            .ToList();
    }
}
