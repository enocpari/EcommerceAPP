using EcommerceApp.Models.Reports;

namespace EcommerceApp.Services
{
    public interface IReportService
    {
        Task<List<SalesReportRow>> GetSalesReportAsync(SalesReportFilter filter);
        Task<SalesReportSummary> GetSalesReportSummaryAsync(SalesReportFilter filter);
        Task<List<ProductReportRow>> GetProductsReportAsync();
        Task<ProductReportSummary> GetProductsReportSummaryAsync();
        Task<List<InventoryReportRow>> GetInventoryReportAsync();
        Task<InventoryReportSummary> GetInventoryReportSummaryAsync();
    }
}