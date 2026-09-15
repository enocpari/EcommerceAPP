using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    public class OrdersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<OrdersController> logger) : Controller
    {
        // GET: /Orders/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var model = new CheckoutViewModel();

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.CustomerName = user.FullName ?? "";
                    model.Email = user.Email ?? "";
                    model.Address = user.Address ?? "";
                    model.Phone = user.PhoneNumber ?? "";
                }
            }

            return View(model);
        }

        // POST: /Orders/ValidateCart
        [HttpPost]
        public async Task<IActionResult> ValidateCart([FromBody] List<CartItemDto> cartItems)
        {
            if (cartItems == null || cartItems.Count == 0)
            {
                return Json(new { success = true, items = new List<object>(), allInStock = true, grandTotal = 0m });
            }

            var productIds = cartItems.Select(c => c.Id).Where(id => id > 0).Distinct().ToList();
            var products = await context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var validatedItems = new List<object>();
            bool allInStock = true;
            decimal grandTotal = 0m;
            var alerts = new List<string>();

            foreach (var item in cartItems)
            {
                if (products.TryGetValue(item.Id, out var product))
                {
                    bool isActive = product.IsActive;
                    bool inStock = isActive && product.Stock >= item.Qty;
                    int maxQty = Math.Max(0, product.Stock);

                    if (!isActive)
                    {
                        allInStock = false;
                        alerts.Add($"El producto '{product.Name}' ya no está disponible.");
                    }
                    else if (product.Stock < item.Qty)
                    {
                        allInStock = false;
                        if (product.Stock == 0)
                            alerts.Add($"El producto '{product.Name}' está agotado.");
                        else
                            alerts.Add($"El producto '{product.Name}' solo tiene {product.Stock} unidad(es) disponible(s).");
                    }

                    decimal lineTotal = product.Price * item.Qty;
                    grandTotal += lineTotal;

                    validatedItems.Add(new
                    {
                        id = product.Id,
                        name = product.Name,
                        brand = product.Brand,
                        spec = product.Spec,
                        imageUrl = product.ImageUrl,
                        price = product.Price,
                        qty = item.Qty,
                        availableStock = product.Stock,
                        isActive = product.IsActive,
                        inStock = inStock,
                        maxQty = maxQty,
                        priceChanged = product.Price != item.Price
                    });
                }
                else
                {
                    allInStock = false;
                    alerts.Add($"El artículo '{item.Name}' no existe en nuestro catálogo.");
                }
            }

            return Json(new
            {
                success = true,
                items = validatedItems,
                allInStock = allInStock,
                grandTotal = grandTotal,
                alerts = alerts
            });
        }

        // POST: /Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckoutViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ItemsJson) || model.ItemsJson == "[]")
            {
                ModelState.AddModelError(nameof(model.ItemsJson), "El carrito de compras está vacío. Agrega productos antes de continuar.");
            }

            List<CartItemDto>? cartItems = null;
            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                cartItems = JsonSerializer.Deserialize<List<CartItemDto>>(model.ItemsJson, jsonOptions);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al deserializar ItemsJson del carrito.");
                ModelState.AddModelError(nameof(model.ItemsJson), "Formato de artículos no válido.");
            }

            if (cartItems == null || cartItems.Count == 0)
            {
                ModelState.AddModelError(nameof(model.ItemsJson), "No se encontraron artículos válidos en el carrito.");
            }

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                    Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, errors });
                }
                return View("Checkout", model);
            }

            // Iniciar transacción de base de datos para garantizar consistencia atómica
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var itemIds = cartItems!.Select(i => i.Id).Where(id => id > 0).Distinct().ToList();
                var productsInDb = await context.Products
                    .Where(p => itemIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p);

                var stockErrors = new List<string>();
                decimal subtotal = 0m;
                var orderItems = new List<OrderItem>();

                foreach (var item in cartItems!)
                {
                    if (item.Qty <= 0) continue;

                    if (!productsInDb.TryGetValue(item.Id, out var product) || !product.IsActive)
                    {
                        stockErrors.Add($"El producto '{item.Name}' ya no se encuentra disponible.");
                        continue;
                    }

                    // Validación estricta de stock disponible (evita sobreventa)
                    if (product.Stock < item.Qty)
                    {
                        if (product.Stock == 0)
                            stockErrors.Add($"El producto '{product.Name}' se ha agotado mientras completabas tu orden.");
                        else
                            stockErrors.Add($"El producto '{product.Name}' solo cuenta con {product.Stock} unidades en almacén (solicitaste {item.Qty}).");
                        continue;
                    }

                    // Precios determinados exclusivamente por el servidor (no se confía en el cliente)
                    decimal unitPrice = product.Price;
                    decimal itemTotal = unitPrice * item.Qty;
                    subtotal += itemTotal;

                    // Descuento atómico de inventario
                    product.Stock -= item.Qty;
                    product.UpdatedAt = DateTime.UtcNow;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductBrand = product.Brand,
                        ProductSpec = product.Spec,
                        ProductImageUrl = product.ImageUrl,
                        UnitPrice = unitPrice,
                        Quantity = item.Qty,
                        TotalPrice = itemTotal
                    });
                }

                if (stockErrors.Count > 0)
                {
                    await transaction.RollbackAsync();
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                        Request.Headers.Accept.ToString().Contains("application/json"))
                    {
                        return Json(new { success = false, errors = stockErrors });
                    }
                    foreach (var err in stockErrors)
                    {
                        ModelState.AddModelError(string.Empty, err);
                    }
                    return View("Checkout", model);
                }

                // Generar número de orden y Token Criptográfico de Transacción único
                var orderNumber = $"NOD-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
                var tokenHex = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4));
                var transactionToken = $"TRX-{orderNumber.Replace("NOD-", "")}-{tokenHex}";

                string? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await userManager.GetUserAsync(User);
                    userId = user?.Id;
                }

                decimal shippingCost = 0m; // Envíos bonificados en NODO
                decimal grandTotal = subtotal + shippingCost;

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    TransactionToken = transactionToken,
                    UserId = userId,
                    CustomerName = model.CustomerName.Trim(),
                    DocumentId = model.DocumentId.Trim(),
                    Email = model.Email.Trim(),
                    Phone = model.Phone.Trim(),
                    Address = model.Address.Trim(),
                    City = model.City.Trim(),
                    Province = model.Province.Trim(),
                    PostalCode = model.PostalCode?.Trim(),
                    Notes = model.Notes?.Trim(),
                    PaymentReference = model.PaymentReference?.Trim(),
                    Status = OrderStatus.Pending,
                    PaymentMethod = model.PaymentMethod,
                    ShippingMethod = model.ShippingMethod,
                    Subtotal = subtotal,
                    ShippingCost = shippingCost,
                    Total = grandTotal,
                    CreatedAt = DateTime.UtcNow,
                    Items = orderItems
                };

                context.Orders.Add(order);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                logger.LogInformation("Pedido #{OrderNumber} registrado exitosamente para el cliente {Customer} (CI/DNI: {DocumentId})",
                    order.OrderNumber, order.CustomerName, order.DocumentId);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                    Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new 
                    { 
                        success = true, 
                        orderId = order.Id, 
                        orderNumber = order.OrderNumber,
                        transactionToken = order.TransactionToken,
                        redirectUrl = Url.Action("Confirmation", new { id = order.Id })
                    });
                }

                return RedirectToAction("Confirmation", new { id = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Fallo crítico al procesar la orden en Create.");
                var errorMsg = "Ocurrió un error inesperado al procesar tu pedido. Por favor intenta de nuevo.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                    Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, errors = new List<string> { errorMsg } });
                }
                ModelState.AddModelError(string.Empty, errorMsg);
                return View("Checkout", model);
            }
        }

        // GET: /Orders/Confirmation/{id}
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: /Orders/MyOrders
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orders = await context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == user.Id || o.Email == user.Email)
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(orders);
        }

        // GET: /Orders/Track
        [HttpGet]
        public async Task<IActionResult> Track(string? orderNumber, string? documentId)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                return View(null as Order);
            }

            var cleanSearch = orderNumber.Trim().ToUpper();
            var normalizedOrderNum = cleanSearch;
            if (!normalizedOrderNum.StartsWith("NOD-") && !normalizedOrderNum.StartsWith("TRX-"))
            {
                normalizedOrderNum = "NOD-" + normalizedOrderNum.TrimStart('#');
            }
            normalizedOrderNum = normalizedOrderNum.TrimStart('#');

            var query = context.Orders
                .Include(o => o.Items)
                .Where(o => o.OrderNumber == normalizedOrderNum || 
                            o.OrderNumber == cleanSearch ||
                            o.TransactionToken == cleanSearch ||
                            o.TransactionToken.ToUpper() == cleanSearch);

            if (!string.IsNullOrWhiteSpace(documentId))
            {
                var doc = documentId.Trim();
                query = query.Where(o => o.DocumentId == doc);
            }

            var order = await query.AsNoTracking().FirstOrDefaultAsync();

            ViewBag.SearchedNumber = orderNumber;
            ViewBag.SearchedDoc = documentId;
            ViewBag.HasSearched = true;

            return View(order);
        }
    }
}
