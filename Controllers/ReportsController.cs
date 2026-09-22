using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using EcommerceApp.Services;
using EcommerceApp.Models;
using EcommerceApp.Models.Reports;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Reports")]
    public class ReportsController(IReportService reportService) : Controller
    {
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Statuses = new List<string> { "all", OrderStatus.Pending, OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Cancelled };
            return View();
        }

        [HttpGet("Sales")]
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate, string? status)
        {
            var filter = new SalesReportFilter
            {
                StartDate = startDate,
                EndDate = endDate,
                Status = status ?? "all"
            };

            var data = await reportService.GetSalesReportAsync(filter);
            var summary = await reportService.GetSalesReportSummaryAsync(filter);

            ViewBag.Filter = filter;
            ViewBag.Summary = summary;
            ViewBag.StoreName = "NODO";
            ViewBag.GeneratedAt = DateTime.Now;

            return View(data);
        }

        [HttpGet("SalesPdf")]
        public async Task<IActionResult> SalesPdf(DateTime? startDate, DateTime? endDate, string? status)
        {
            var filter = new SalesReportFilter
            {
                StartDate = startDate,
                EndDate = endDate,
                Status = status ?? "all"
            };

            var data = await reportService.GetSalesReportAsync(filter);
            var summary = await reportService.GetSalesReportSummaryAsync(filter);

            var document = new SalesReportDocument(data, summary, filter, "NODO");
            var pdfBytes = document.GeneratePdf();

            var fileName = $"ReporteVentas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("Products")]
        public async Task<IActionResult> Products()
        {
            var data = await reportService.GetProductsReportAsync();
            var summary = await reportService.GetProductsReportSummaryAsync();

            ViewBag.Summary = summary;
            ViewBag.StoreName = "NODO";
            ViewBag.GeneratedAt = DateTime.Now;

            return View(data);
        }

        [HttpGet("ProductsPdf")]
        public async Task<IActionResult> ProductsPdf()
        {
            var data = await reportService.GetProductsReportAsync();
            var summary = await reportService.GetProductsReportSummaryAsync();

            var document = new ProductsReportDocument(data, summary, "NODO");
            var pdfBytes = document.GeneratePdf();

            var fileName = $"ReporteProductos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("Inventory")]
        public async Task<IActionResult> Inventory()
        {
            var data = await reportService.GetInventoryReportAsync();
            var summary = await reportService.GetInventoryReportSummaryAsync();

            ViewBag.Summary = summary;
            ViewBag.StoreName = "NODO";
            ViewBag.GeneratedAt = DateTime.Now;

            return View(data);
        }

        [HttpGet("InventoryPdf")]
        public async Task<IActionResult> InventoryPdf()
        {
            var data = await reportService.GetInventoryReportAsync();
            var summary = await reportService.GetInventoryReportSummaryAsync();

            var document = new InventoryReportDocument(data, summary, "NODO");
            var pdfBytes = document.GeneratePdf();

            var fileName = $"ReporteInventario_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }

    public class SalesReportDocument : IDocument
    {
        private readonly List<SalesReportRow> _data;
        private readonly SalesReportSummary _summary;
        private readonly SalesReportFilter _filter;
        private readonly string _storeName;

        public SalesReportDocument(List<SalesReportRow> data, SalesReportSummary summary, SalesReportFilter filter, string storeName)
        {
            _data = data;
            _summary = summary;
            _filter = filter;
            _storeName = storeName;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(_storeName).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text(_storeName + " · Tecnología Curada").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("Generado el").FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(10).Bold().FontFamily("Lato");
                    });
                });

                column.Item().PaddingTop(10).Text("REPORTE DE VENTAS").FontSize(16).Bold().AlignCenter().FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("Filtros: ").FontSize(8).FontColor(Colors.Grey.Medium);
                    if (_filter.StartDate.HasValue) text.Span($"Desde {_filter.StartDate.Value:dd/MM/yyyy} ").FontSize(8);
                    if (_filter.EndDate.HasValue) text.Span($"Hasta {_filter.EndDate.Value:dd/MM/yyyy} ").FontSize(8);
                    if (_filter.Status != "all") text.Span($"Estado: {_filter.Status}").FontSize(8);
                    if (!_filter.StartDate.HasValue && !_filter.EndDate.HasValue && _filter.Status == "all")
                        text.Span("Sin filtros aplicados (todos los pedidos)").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.2f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("N° Pedido").Bold();
                    header.Cell().Element(CellStyle).Text("Fecha").Bold();
                    header.Cell().Element(CellStyle).Text("Cliente").Bold();
                    header.Cell().Element(CellStyle).Text("Email").Bold();
                    header.Cell().Element(CellStyle).Text("Estado").Bold();
                    header.Cell().Element(CellStyle).Text("Método Pago").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Cant. Prod.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Subtotal").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Envío").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold();
                });

                foreach (var item in _data)
                {
                    var statusColor = item.Status switch
                    {
                        "Pendiente" => Colors.Orange.Lighten1,
                        "Pago Acreditado" => Colors.Green.Lighten1,
                        "En Preparación" => Colors.Purple.Lighten1,
                        "En Camino" => Colors.Teal.Lighten1,
                        "Entregado" => Colors.Green.Lighten2,
                        "Cancelado" => Colors.Red.Lighten1,
                        _ => Colors.Grey.Lighten1
                    };

                    table.Cell().Element(CellStyle).Text($"#{item.OrderNumber}");
                    table.Cell().Element(CellStyle).Text(item.Date.ToString("dd/MM/yyyy"));
                    table.Cell().Element(CellStyle).Text(item.CustomerName);
                    table.Cell().Element(CellStyle).Text(item.Email);
                    table.Cell().Element(CellStyle).Background(statusColor).Padding(2).Text(item.Status).FontSize(7);
                    table.Cell().Element(CellStyle).Text(item.PaymentMethod);
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.ProductsQuantity.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text($"Bs {item.Subtotal:N2}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"Bs {item.ShippingCost:N2}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"Bs {item.Total:N2}").Bold().FontColor(Colors.Blue.Darken2);
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Total pedidos: {_summary.TotalOrders}").FontSize(10).Bold();
                        col.Item().Text($"Productos vendidos: {_summary.TotalProductsSold}").FontSize(10).Bold();
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Ventas totales: Bs {_summary.TotalSales:N2}").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Ticket promedio: Bs {_summary.AverageTicket:N2}").FontSize(10).Bold();
                    });
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span($"  ·  Generado por {_storeName} Admin").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }

        static IContainer CellStyle(IContainer container) => container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
    }

    public class ProductsReportDocument : IDocument
    {
        private readonly List<ProductReportRow> _data;
        private readonly ProductReportSummary _summary;
        private readonly string _storeName;

        public ProductsReportDocument(List<ProductReportRow> data, ProductReportSummary summary, string storeName)
        {
            _data = data;
            _summary = summary;
            _storeName = storeName;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(_storeName).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text(_storeName + " · Tecnología Curada").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("Generado el").FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(10).Bold().FontFamily("Lato");
                    });
                });

                column.Item().PaddingTop(10).Text("REPORTE DE CATÁLOGO DE PRODUCTOS").FontSize(16).Bold().AlignCenter().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.5f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(0.7f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.7f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Id").Bold();
                    header.Cell().Element(CellStyle).Text("Nombre").Bold();
                    header.Cell().Element(CellStyle).Text("Marca").Bold();
                    header.Cell().Element(CellStyle).Text("Categoría").Bold();
                    header.Cell().Element(CellStyle).Text("Especificación").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Precio").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Stock").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Estado").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Oferta").Bold();
                });

                foreach (var item in _data)
                {
                    table.Cell().Element(CellStyle).Text(item.Id.ToString());
                    table.Cell().Element(CellStyle).Text(item.Name);
                    table.Cell().Element(CellStyle).Text(item.Brand);
                    table.Cell().Element(CellStyle).Text(item.Category);
                    table.Cell().Element(CellStyle).Text(item.Spec);
                    table.Cell().Element(CellStyle).AlignRight().Text($"Bs {item.Price:N2}");
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Stock.ToString())
                        .FontColor(item.Stock == 0 ? Colors.Red.Darken1 : item.Stock <= 5 ? Colors.Orange.Darken1 : Colors.Green.Darken1);
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Status);
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.IsOffer ? "Sí" : "No");
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Total productos: {_summary.TotalProducts}").FontSize(10).Bold();
                        col.Item().Text($"Productos activos: {_summary.ActiveProducts}").FontSize(10).Bold().FontColor(Colors.Green.Darken1);
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Productos agotados: {_summary.OutOfStockProducts}").FontSize(10).Bold().FontColor(Colors.Red.Darken1);
                        col.Item().Text($"Valor inventario: Bs {_summary.InventoryValue:N2}").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                    });
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span($"  ·  Generado por {_storeName} Admin").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }

        static IContainer CellStyle(IContainer container) => container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
    }

    public class InventoryReportDocument : IDocument
    {
        private readonly List<InventoryReportRow> _data;
        private readonly InventoryReportSummary _summary;
        private readonly string _storeName;

        public InventoryReportDocument(List<InventoryReportRow> data, InventoryReportSummary summary, string storeName)
        {
            _data = data;
            _summary = summary;
            _storeName = storeName;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(_storeName).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text(_storeName + " · Tecnología Curada").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("Generado el").FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(10).Bold().FontFamily("Lato");
                    });
                });

                column.Item().PaddingTop(10).Text("REPORTE DE INVENTARIO").FontSize(16).Bold().AlignCenter().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(0.7f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Producto").Bold();
                    header.Cell().Element(CellStyle).Text("Marca").Bold();
                    header.Cell().Element(CellStyle).Text("Categoría").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Stock").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Precio Unit.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Valor Inventario").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Estado Stock").Bold();
                });

                foreach (var item in _data)
                {
                    var statusColor = item.StockStatus switch
                    {
                        "AGOTADO" => Colors.Red.Lighten1,
                        "STOCK BAJO" => Colors.Orange.Lighten1,
                        "DISPONIBLE" => Colors.Green.Lighten2,
                        _ => Colors.Grey.Lighten1
                    };

                    table.Cell().Element(CellStyle).Text(item.Product);
                    table.Cell().Element(CellStyle).Text(item.Brand);
                    table.Cell().Element(CellStyle).Text(item.Category);
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Stock.ToString())
                        .FontColor(item.Stock == 0 ? Colors.Red.Darken1 : item.Stock <= 5 ? Colors.Orange.Darken1 : Colors.Green.Darken1);
                    table.Cell().Element(CellStyle).AlignRight().Text($"Bs {item.UnitPrice:N2}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"Bs {item.InventoryValue:N2}").Bold().FontColor(Colors.Blue.Darken2);
                    table.Cell().Element(CellStyle).AlignCenter().Background(statusColor).Padding(2).Text(item.StockStatus).FontSize(8);
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Productos diferentes: {_summary.DistinctProducts}").FontSize(10).Bold();
                        col.Item().Text($"Unidades en stock: {_summary.TotalUnitsInStock}").FontSize(10).Bold();
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Agotados: {_summary.OutOfStockProducts}").FontSize(10).Bold().FontColor(Colors.Red.Darken1);
                        col.Item().Text($"Stock bajo (≤5): {_summary.LowStockProducts}").FontSize(10).Bold().FontColor(Colors.Orange.Darken1);
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Valor total inventario: Bs {_summary.TotalInventoryValue:N2}").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                    });
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span($"  ·  Generado por {_storeName} Admin").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }

        static IContainer CellStyle(IContainer container) => container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
    }
}