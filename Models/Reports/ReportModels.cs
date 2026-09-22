namespace EcommerceApp.Models.Reports
{
    public class SalesReportRow
    {
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public int ProductsQuantity { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
    }

    public class ProductReportRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Spec { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsOffer { get; set; }
    }

    public class InventoryReportRow
    {
        public string Product { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal InventoryValue { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }

    public class SalesReportFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
    }

    public class SalesReportSummary
    {
        public int TotalOrders { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AverageTicket { get; set; }
    }

    public class ProductReportSummary
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal InventoryValue { get; set; }
    }

    public class InventoryReportSummary
    {
        public int DistinctProducts { get; set; }
        public int TotalUnitsInStock { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
    }
}