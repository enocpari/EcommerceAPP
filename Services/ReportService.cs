using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.Reports;

namespace EcommerceApp.Services
{
    public class ReportService(ApplicationDbContext context) : IReportService
    {
        private static readonly string[] AllStatuses = 
            [OrderStatus.Pending, OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Cancelled];

        private static DateTime ToUtc(DateTime dt) 
            => dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();

        public async Task<List<SalesReportRow>> GetSalesReportAsync(SalesReportFilter filter)
        {
            var query = context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .AsQueryable();

            if (filter.StartDate.HasValue)
            {
                var start = ToUtc(filter.StartDate.Value).Date;
                query = query.Where(o => o.CreatedAt >= start);
            }

            if (filter.EndDate.HasValue)
            {
                var end = ToUtc(filter.EndDate.Value).Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedAt <= end);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "all")
            {
                query = query.Where(o => o.Status == filter.Status);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new SalesReportRow
                {
                    OrderNumber = o.OrderNumber,
                    Date = o.CreatedAt,
                    CustomerName = o.CustomerName,
                    Email = o.Email,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    ProductsQuantity = o.Items.Sum(i => i.Quantity),
                    Subtotal = o.Subtotal,
                    ShippingCost = o.ShippingCost,
                    Total = o.Total
                })
                .ToListAsync();

            return orders;
        }

        public async Task<SalesReportSummary> GetSalesReportSummaryAsync(SalesReportFilter filter)
        {
            var query = context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .AsQueryable();

            if (filter.StartDate.HasValue)
            {
                var start = ToUtc(filter.StartDate.Value).Date;
                query = query.Where(o => o.CreatedAt >= start);
            }

            if (filter.EndDate.HasValue)
            {
                var end = ToUtc(filter.EndDate.Value).Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedAt <= end);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "all")
            {
                query = query.Where(o => o.Status == filter.Status);
            }

            var orders = await query.ToListAsync();

            var totalOrders = orders.Count;
            var totalProductsSold = orders.Sum(o => o.Items.Sum(i => i.Quantity));
            var totalSales = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total);
            var averageTicket = totalOrders > 0 ? totalSales / totalOrders : 0m;

            return new SalesReportSummary
            {
                TotalOrders = totalOrders,
                TotalProductsSold = totalProductsSold,
                TotalSales = totalSales,
                AverageTicket = averageTicket
            };
        }

        public async Task<List<ProductReportRow>> GetProductsReportAsync()
        {
            var products = await context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .OrderBy(p => p.Category != null ? p.Category.DisplayOrder : 999)
                .ThenBy(p => p.Name)
                .Select(p => new ProductReportRow
                {
                    Id = p.Id,
                    Name = p.Name,
                    Brand = p.Brand ?? "NODO",
                    Category = p.Category != null ? p.Category.Name : (p.CategoryName ?? "Sin categoría"),
                    Spec = p.Spec ?? string.Empty,
                    Price = p.Price,
                    Stock = p.Stock,
                    Status = p.IsActive ? "Activo" : "Inactivo",
                    IsOffer = p.IsOffer
                })
                .ToListAsync();

            return products;
        }

        public async Task<ProductReportSummary> GetProductsReportSummaryAsync()
        {
            var products = await context.Products
                .AsNoTracking()
                .ToListAsync();

            var totalProducts = products.Count;
            var activeProducts = products.Count(p => p.IsActive);
            var outOfStockProducts = products.Count(p => p.Stock == 0);
            var inventoryValue = products.Sum(p => p.Price * p.Stock);

            return new ProductReportSummary
            {
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                OutOfStockProducts = outOfStockProducts,
                InventoryValue = inventoryValue
            };
        }

        public async Task<List<InventoryReportRow>> GetInventoryReportAsync()
        {
            var products = await context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .OrderBy(p => p.Stock)
                .ThenBy(p => p.Name)
                .Select(p => new InventoryReportRow
                {
                    Product = p.Name,
                    Brand = p.Brand ?? "NODO",
                    Category = p.Category != null ? p.Category.Name : (p.CategoryName ?? "Sin categoría"),
                    Stock = p.Stock,
                    UnitPrice = p.Price,
                    InventoryValue = p.Price * p.Stock,
                    StockStatus = p.Stock == 0 ? "AGOTADO" : p.Stock <= 5 ? "STOCK BAJO" : "DISPONIBLE"
                })
                .ToListAsync();

            return products;
        }

        public async Task<InventoryReportSummary> GetInventoryReportSummaryAsync()
        {
            var products = await context.Products
                .AsNoTracking()
                .ToListAsync();

            var distinctProducts = products.Count;
            var totalUnitsInStock = products.Sum(p => p.Stock);
            var outOfStockProducts = products.Count(p => p.Stock == 0);
            var lowStockProducts = products.Count(p => p.Stock > 0 && p.Stock <= 5);
            var totalInventoryValue = products.Sum(p => p.Price * p.Stock);

            return new InventoryReportSummary
            {
                DistinctProducts = distinctProducts,
                TotalUnitsInStock = totalUnitsInStock,
                OutOfStockProducts = outOfStockProducts,
                LowStockProducts = lowStockProducts,
                TotalInventoryValue = totalInventoryValue
            };
        }
    }
}