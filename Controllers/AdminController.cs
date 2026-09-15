using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(ApplicationDbContext context, StoreConfigService storeConfig) : Controller
    {
        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var products = await context.Products.AsNoTracking().ToListAsync();
            var orders = await context.Orders.Include(o => o.Items).AsNoTracking().ToListAsync();

            ViewBag.TotalProducts = products.Count;
            ViewBag.LowStockCount = products.Count(p => p.Stock <= 5);
            ViewBag.OutOfStockCount = products.Count(p => p.Stock == 0);
            ViewBag.TotalStock = products.Sum(p => p.Stock);

            // Métricas financieras y operativas calculadas en tiempo real de la base de datos
            var validOrders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var weeklySales = validOrders.Where(o => o.CreatedAt >= weekAgo).Sum(o => o.Total);
            var totalSales = validOrders.Sum(o => o.Total);
            var pendingOrders = orders.Count(o => o.Status.Contains("Pendiente"));
            var avgTicket = validOrders.Count > 0 ? validOrders.Average(o => o.Total) : 0m;

            var culture = new System.Globalization.CultureInfo("es-AR");
            ViewBag.WeeklySales = weeklySales.ToString("C0", culture);
            ViewBag.TotalSales = totalSales.ToString("C0", culture);
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.AverageTicket = avgTicket.ToString("C0", culture);

            ViewBag.LowStockProducts = products.Where(p => p.Stock <= 5).OrderBy(p => p.Stock).Take(6).ToList();
            ViewBag.RecentProducts = products.OrderByDescending(p => p.Id).Take(5).ToList();
            ViewBag.RecentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(5).ToList();

            return View(products);
        }

        // GET: /Admin/Products
        public async Task<IActionResult> Products(string? category, string? search)
        {
            var query = context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "all")
            {
                query = query.Where(p => (p.Category ?? "").ToLower().Contains(category.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s) || (p.Brand ?? "").ToLower().Contains(s));
            }

            var list = await query.OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.CurrentCategory = category ?? "all";
            ViewBag.CurrentSearch = search ?? "";
            return View(list);
        }

        // GET: /Admin/Inventory
        public async Task<IActionResult> Inventory()
        {
            var products = await context.Products
                .AsNoTracking()
                .OrderBy(p => p.Stock)
                .ThenBy(p => p.Name)
                .ToListAsync();
            return View(products);
        }

        // POST: /Admin/UpdateStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int stock)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Stock = Math.Max(0, stock);
            product.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Stock de '{product.Name}' actualizado a {product.Stock} unidades.";
            return RedirectToAction(nameof(Inventory));
        }

        // GET: /Admin/Orders
        public async Task<IActionResult> Orders(string? status, string? search)
        {
            var query = context.Orders.Include(o => o.Items).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
            {
                var s = status.ToLower();
                if (s == "pendiente")
                    query = query.Where(o => o.Status.ToLower().Contains("pendiente"));
                else if (s == "pago")
                    query = query.Where(o => o.Status.ToLower().Contains("pago"));
                else if (s == "preparando")
                    query = query.Where(o => o.Status.ToLower().Contains("prepara") || o.Status.ToLower().Contains("empaqueta"));
                else if (s == "camino")
                    query = query.Where(o => o.Status.ToLower().Contains("camino") || o.Status.ToLower().Contains("tránsito") || o.Status.ToLower().Contains("transito"));
                else if (s == "entregado")
                    query = query.Where(o => o.Status.ToLower().Contains("entrega"));
                else
                    query = query.Where(o => o.Status.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderNumber.ToLower().Contains(q) ||
                    o.TransactionToken.ToLower().Contains(q) ||
                    o.CustomerName.ToLower().Contains(q) ||
                    o.DocumentId.ToLower().Contains(q) ||
                    o.Email.ToLower().Contains(q) ||
                    o.Phone.ToLower().Contains(q) ||
                    o.City.ToLower().Contains(q));
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            // Resumen de cantidades y métricas para los KPI cards y filtros
            var allOrders = await context.Orders.AsNoTracking().ToListAsync();
            ViewBag.CountAll = allOrders.Count;
            ViewBag.CountPendiente = allOrders.Count(o => o.Status.ToLower().Contains("pendiente"));
            ViewBag.CountPago = allOrders.Count(o => o.Status.ToLower().Contains("pago"));
            ViewBag.CountPreparando = allOrders.Count(o => o.Status.ToLower().Contains("prepara") || o.Status.ToLower().Contains("empaqueta"));
            ViewBag.CountCamino = allOrders.Count(o => o.Status.ToLower().Contains("camino") || o.Status.ToLower().Contains("tránsito") || o.Status.ToLower().Contains("transito"));
            ViewBag.CountEntregado = allOrders.Count(o => o.Status.ToLower().Contains("entrega"));
            ViewBag.TotalSales = allOrders.Where(o => o.Status != "Cancelado").Sum(o => o.Total);

            ViewBag.CurrentStatus = status ?? "all";
            ViewBag.CurrentSearch = search ?? "";

            return View(orders);
        }

        // GET: /Admin/OrderDetail/{id}
        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: /Admin/EditOrder/{id}
        public async Task<IActionResult> EditOrder(int id)
        {
            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: /Admin/EditOrder/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrder(int id, Order model)
        {
            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (!ModelState.IsValid)
            {
                // Si la validación falla por campos de Items o similares, permitimos actualizar si los campos principales son válidos
                // Verificamos campos críticos
                if (string.IsNullOrWhiteSpace(model.CustomerName) || string.IsNullOrWhiteSpace(model.DocumentId))
                {
                    return View(order);
                }
            }

            var oldStatus = order.Status;
            order.CustomerName = model.CustomerName;
            order.DocumentId = model.DocumentId;
            order.Email = model.Email;
            order.Phone = model.Phone;
            order.Address = model.Address;
            order.City = model.City;
            order.Province = model.Province;
            order.PostalCode = model.PostalCode;
            order.Notes = model.Notes;
            order.PaymentReference = model.PaymentReference?.Trim();
            order.AdminNotes = model.AdminNotes?.Trim();
            order.Status = model.Status;
            order.PaymentMethod = model.PaymentMethod;
            order.ShippingMethod = model.ShippingMethod;
            order.TrackingNumber = model.TrackingNumber;
            order.UpdatedAt = DateTime.UtcNow;

            await HandleOrderStatusStockChangeAsync(order, oldStatus, model.Status);
            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Pedido #{order.OrderNumber} actualizado correctamente.";
            return RedirectToAction(nameof(OrderDetail), new { id = order.Id });
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status, string? trackingNumber, string? returnUrl)
        {
            var order = await context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            var oldStatus = order.Status;
            order.Status = status;
            if (!string.IsNullOrWhiteSpace(trackingNumber))
            {
                order.TrackingNumber = trackingNumber.Trim();
            }
            order.UpdatedAt = DateTime.UtcNow;

            await HandleOrderStatusStockChangeAsync(order, oldStatus, status);
            await context.SaveChangesAsync();

            var msg = $"Estado de pedido #{order.OrderNumber} cambiado a '{status}'.";
            if (status.Contains("Cancel", StringComparison.OrdinalIgnoreCase) && !oldStatus.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
            {
                msg += " Se reintegró el stock de los productos a la base de datos.";
            }
            TempData["SuccessMessage"] = msg;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(OrderDetail), new { id = order.Id });
        }

        private async Task HandleOrderStatusStockChangeAsync(Order order, string oldStatus, string newStatus)
        {
            bool isBecomingCancelled = newStatus.Contains("Cancel", StringComparison.OrdinalIgnoreCase);
            bool wasCancelled = oldStatus.Contains("Cancel", StringComparison.OrdinalIgnoreCase);

            if (isBecomingCancelled && !wasCancelled)
            {
                // Reintegrar unidades a inventario
                foreach (var item in order.Items)
                {
                    if (item.ProductId.HasValue)
                    {
                        var product = await context.Products.FindAsync(item.ProductId.Value);
                        if (product != null)
                        {
                            product.Stock += item.Quantity;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
            }
            else if (!isBecomingCancelled && wasCancelled)
            {
                // Reactivación: volver a reservar unidades si existen
                foreach (var item in order.Items)
                {
                    if (item.ProductId.HasValue)
                    {
                        var product = await context.Products.FindAsync(item.ProductId.Value);
                        if (product != null)
                        {
                            product.Stock = Math.Max(0, product.Stock - item.Quantity);
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
            }
        }

        // GET: /Admin/Settings
        public IActionResult Settings()
        {
            var model = storeConfig.GetSettings();
            return View(model);
        }

        // POST: /Admin/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(StoreSettingsModel model)
        {
            if (string.IsNullOrWhiteSpace(model.BrandName))
            {
                ModelState.AddModelError(nameof(model.BrandName), "El nombre de la marca es obligatorio.");
                return View(model);
            }

            storeConfig.UpdateSettings(model);
            TempData["SuccessMessage"] = "Ajustes de la marca y tienda guardados correctamente.";
            return RedirectToAction(nameof(Settings));
        }
    }
}
