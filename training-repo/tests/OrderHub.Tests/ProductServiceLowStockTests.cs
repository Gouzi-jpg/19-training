using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    [Fact]
    public async Task GetLowStock_FiltersByThreshold_AndSortsByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        TestSetup.AddProduct(db, stock: 8);
        TestSetup.AddProduct(db, stock: 20);
        TestSetup.AddProduct(db, stock: 3);
        TestSetup.AddProduct(db, stock: 12);

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].StockQuantity);
        Assert.Equal(8, result[1].StockQuantity);
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        TestSetup.AddProduct(db, stock: 2, isActive: false);

        var result = await service.GetLowStockAsync(10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_ExcludesCancelledAndOldOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 100);

        // 30 天內、非 Cancelled → 計入 4
        await SeedOrderAsync(db, customer.Id, product.Id, quantity: 4,
            status: OrderStatus.Confirmed, createdAt: DateTime.UtcNow.AddDays(-5));

        // 30 天內、Cancelled → 排除
        await SeedOrderAsync(db, customer.Id, product.Id, quantity: 3,
            status: OrderStatus.Cancelled, createdAt: DateTime.UtcNow.AddDays(-5));

        // 40 天前、非 Cancelled → 排除
        await SeedOrderAsync(db, customer.Id, product.Id, quantity: 5,
            status: OrderStatus.Confirmed, createdAt: DateTime.UtcNow.AddDays(-40));

        // 門檻設大確保商品仍入列
        var result = await service.GetLowStockAsync(9999);

        var row = result.Single(r => r.Sku == product.Sku);
        Assert.Equal(4, row.SoldLast30Days);
    }

    private static async Task SeedOrderAsync(
        Infrastructure.Data.OrderHubDbContext db,
        int customerId,
        int productId,
        int quantity,
        OrderStatus status,
        DateTime createdAt)
    {
        var orderService = TestSetup.CreateOrderService(db);
        var result = await orderService.CreateOrderAsync(customerId, new[] { new NewOrderLine(productId, quantity) });
        var order = result.Value!;
        order.Status = status;
        order.CreatedAt = createdAt;
        await db.SaveChangesAsync();
    }
}
