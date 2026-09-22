using Microsoft.AspNetCore.Identity;
using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            // 1. Crear roles
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Rol creado: {Role}", role);
                }
            }

            // 2. Usuarios seed (idempotente: solo crea si no existe)
            await SeedUserAsync(userManager, logger,
                email: "admin@ecommerce.com",
                password: "Admin123!",
                fullName: "Administrador NODO",
                address: "Oficina Central NODO",
                role: "Admin");

            await SeedUserAsync(userManager, logger,
                email: "user@ecommerce.com",
                password: "User123!",
                fullName: "Usuario Demo",
                address: "Calle Demo 123",
                role: "User");

            // 3. Productos y Pedidos seed (idempotente)
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await EnsureTablesAsync(dbContext, logger);
            await SeedProductsAsync(dbContext, logger);
            await SeedOrdersAsync(dbContext, logger);
        }

        private static async Task EnsureTablesAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(context.Database, @"
CREATE TABLE IF NOT EXISTS ""Orders"" (
    ""Id"" serial PRIMARY KEY,
    ""OrderNumber"" character varying(50) NOT NULL,
    ""TransactionToken"" character varying(100) NOT NULL DEFAULT '',
    ""UserId"" text NULL,
    ""CustomerName"" character varying(150) NOT NULL,
    ""DocumentId"" character varying(50) NOT NULL,
    ""Email"" character varying(150) NOT NULL,
    ""Phone"" character varying(50) NOT NULL,
    ""Address"" character varying(250) NOT NULL,
    ""City"" character varying(100) NOT NULL,
    ""Province"" character varying(100) NOT NULL,
    ""PostalCode"" character varying(20) NULL,
    ""Notes"" character varying(500) NULL,
    ""Status"" character varying(50) NOT NULL,
    ""PaymentMethod"" character varying(100) NOT NULL,
    ""ShippingMethod"" character varying(100) NOT NULL,
    ""TrackingNumber"" character varying(100) NULL,
    ""Subtotal"" numeric(18,2) NOT NULL,
    ""ShippingCost"" numeric(18,2) NOT NULL,
    ""Total"" numeric(18,2) NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""UpdatedAt"" timestamp with time zone NULL
);
ALTER TABLE IF EXISTS ""Orders"" ADD COLUMN IF NOT EXISTS ""TransactionToken"" character varying(100) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS ""Orders"" ADD COLUMN IF NOT EXISTS ""PaymentReference"" character varying(100) NULL;
ALTER TABLE IF EXISTS ""Orders"" ADD COLUMN IF NOT EXISTS ""AdminNotes"" character varying(500) NULL;
ALTER TABLE IF EXISTS ""Products"" ADD COLUMN IF NOT EXISTS ""IsActive"" boolean NOT NULL DEFAULT true;
CREATE TABLE IF NOT EXISTS ""OrderItems"" (
    ""Id"" serial PRIMARY KEY,
    ""OrderId"" integer NOT NULL REFERENCES ""Orders""(""Id"") ON DELETE CASCADE,
    ""ProductId"" integer NULL REFERENCES ""Products""(""Id"") ON DELETE SET NULL,
    ""ProductName"" character varying(150) NOT NULL,
    ""ProductBrand"" character varying(50) NULL,
    ""ProductSpec"" character varying(120) NULL,
    ""ProductImageUrl"" character varying(2048) NULL,
    ""UnitPrice"" numeric(18,2) NOT NULL,
    ""Quantity"" integer NOT NULL,
    ""TotalPrice"" numeric(18,2) NOT NULL
);
CREATE INDEX IF NOT EXISTS ""IX_Orders_OrderNumber"" ON ""Orders""(""OrderNumber"");
CREATE INDEX IF NOT EXISTS ""IX_Orders_TransactionToken"" ON ""Orders""(""TransactionToken"");
CREATE INDEX IF NOT EXISTS ""IX_Orders_DocumentId"" ON ""Orders""(""DocumentId"");
CREATE INDEX IF NOT EXISTS ""IX_Orders_Status"" ON ""Orders""(""Status"");
CREATE INDEX IF NOT EXISTS ""IX_Orders_CreatedAt"" ON ""Orders""(""CreatedAt"");
CREATE INDEX IF NOT EXISTS ""IX_Orders_UserId"" ON ""Orders""(""UserId"");
CREATE INDEX IF NOT EXISTS ""IX_OrderItems_OrderId"" ON ""OrderItems""(""OrderId"");
");
                logger.LogInformation("Tablas Orders y OrderItems verificadas correctamente con campos de integridad.");
            }
            catch (Exception ex)
            {
                logger.LogWarning("No se pudo ejecutar DDL directo (podría ser sandbox o permisos): {Message}", ex.Message);
            }
        }

        private static async Task SeedOrdersAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                if (context.Orders.Any()) return;

                var pIphone = context.Products.FirstOrDefault(p => p.Name.Contains("iPhone 15 Pro"));
                var pSony = context.Products.FirstOrDefault(p => p.Name.Contains("WH-1000XM5"));
                var pAirpods = context.Products.FirstOrDefault(p => p.Name.Contains("AirPods Pro"));
                var pGalaxy = context.Products.FirstOrDefault(p => p.Name.Contains("Galaxy S24 Ultra"));
                var pBose = context.Products.FirstOrDefault(p => p.Name.Contains("QuietComfort"));
                var pPixel = context.Products.FirstOrDefault(p => p.Name.Contains("Pixel 8"));
                var pXiaomi = context.Products.FirstOrDefault(p => p.Name.Contains("Xiaomi 14"));
                var pJBL = context.Products.FirstOrDefault(p => p.Name.Contains("JBL Live 660NC"));
                var pMotorola = context.Products.FirstOrDefault(p => p.Name.Contains("Motorola Edge 40"));
                var pWF = context.Products.FirstOrDefault(p => p.Name.Contains("WF-1000XM5"));
                var pFlip = context.Products.FirstOrDefault(p => p.Name.Contains("Galaxy Z Flip5"));
                var pAirpodsMax = context.Products.FirstOrDefault(p => p.Name.Contains("AirPods Max"));
                var pMomentum = context.Products.FirstOrDefault(p => p.Name.Contains("Momentum 4"));
                var pNothing = context.Products.FirstOrDefault(p => p.Name.Contains("Nothing Phone"));
                var pA54 = context.Products.FirstOrDefault(p => p.Name.Contains("Galaxy A54"));
                var pIphone15 = context.Products.FirstOrDefault(p => p.Name == "iPhone 15");

                var orders = new List<Order>
                {
                    // 1. Pendiente (hace 5 días)
                    new()
                    {
                        OrderNumber = "NOD-9405",
                        TransactionToken = "TRX-9405-A1B2C3D4",
                        CustomerName = "Roberto Fernández",
                        DocumentId = "27891034",
                        Email = "roberto.fernandez@example.com",
                        Phone = "+54 9 11 5555-0101",
                        Address = "Av. Corrientes 1200, Piso 3",
                        City = "San Nicolás",
                        Province = "CABA",
                        PostalCode = "1043",
                        Notes = "Esperando confirmación de transferencia.",
                        Status = "Pendiente",
                        PaymentMethod = "Transferencia Bancaria",
                        ShippingMethod = "Andreani Prioritario",
                        TrackingNumber = null,
                        Subtotal = 899000m,
                        ShippingCost = 0m,
                        Total = 899000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pPixel?.Id,
                                ProductName = pPixel?.Name ?? "Pixel 8",
                                ProductBrand = pPixel?.Brand ?? "Google",
                                ProductSpec = pPixel?.Spec ?? "128 GB · Tensor G3",
                                ProductImageUrl = pPixel?.ImageUrl,
                                UnitPrice = 899000m,
                                Quantity = 1,
                                TotalPrice = 899000m
                            }
                        }
                    },
                    // 2. Pago Acreditado (hace 4 días)
                    new()
                    {
                        OrderNumber = "NOD-9404",
                        TransactionToken = "TRX-9404-E5F6A7B8",
                        CustomerName = "Valentina Suárez",
                        DocumentId = "39102847",
                        Email = "valentina.suarez@example.com",
                        Phone = "+54 9 351 444-5566",
                        Address = "Bv. Illia 450",
                        City = "Córdoba Capital",
                        Province = "Córdoba",
                        PostalCode = "5000",
                        Notes = "Pago confirmado vía email.",
                        Status = "Pago Acreditado",
                        PaymentMethod = "MercadoPago 6c s/ interés",
                        ShippingMethod = "Andreani Prioritario",
                        TrackingNumber = null,
                        Subtotal = 1299000m,
                        ShippingCost = 0m,
                        Total = 1299000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pIphone?.Id,
                                ProductName = pIphone?.Name ?? "iPhone 15 Pro",
                                ProductBrand = pIphone?.Brand ?? "Apple",
                                ProductSpec = pIphone?.Spec ?? "128 GB · Titanio",
                                ProductImageUrl = pIphone?.ImageUrl,
                                UnitPrice = 1299000m,
                                Quantity = 1,
                                TotalPrice = 1299000m
                            }
                        }
                    },
                    // 3. En Preparación (hace 3 días)
                    new()
                    {
                        OrderNumber = "NOD-9403",
                        TransactionToken = "TRX-9403-C9D0E1F2",
                        CustomerName = "Diego Herrera",
                        DocumentId = "31209485",
                        Email = "diego.herrera@example.com",
                        Phone = "+54 9 11 3322-1100",
                        Address = "Av. Libertador 4500, Dpto 5B",
                        City = "Núñez",
                        Province = "CABA",
                        PostalCode = "1428",
                        Notes = "Empaquetar con cuidado, regalo.",
                        Status = "En Preparación",
                        PaymentMethod = "Visa 12 cuotas",
                        ShippingMethod = "Moto Express en el día (CABA)",
                        TrackingNumber = null,
                        Subtotal = 718000m,
                        ShippingCost = 0m,
                        Total = 718000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(-2),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pSony?.Id,
                                ProductName = pSony?.Name ?? "WH-1000XM5",
                                ProductBrand = pSony?.Brand ?? "Sony",
                                ProductSpec = pSony?.Spec ?? "Over-ear · ANC líder",
                                ProductImageUrl = pSony?.ImageUrl,
                                UnitPrice = 389000m,
                                Quantity = 1,
                                TotalPrice = 389000m
                            },
                            new()
                            {
                                ProductId = pAirpods?.Id,
                                ProductName = pAirpods?.Name ?? "AirPods Pro (2ª gen)",
                                ProductBrand = pAirpods?.Brand ?? "Apple",
                                ProductSpec = pAirpods?.Spec ?? "In-ear · H2 · ANC",
                                ProductImageUrl = pAirpods?.ImageUrl,
                                UnitPrice = 329000m,
                                Quantity = 1,
                                TotalPrice = 329000m
                            }
                        }
                    },
                    // 4. En Preparación (hoy -2 horas) - original
                    new()
                    {
                        OrderNumber = "NOD-9402",
                        TransactionToken = "TRX-9402-A8F7E3B1",
                        CustomerName = "Camila Morales",
                        DocumentId = "10482914",
                        Email = "camila.morales@example.com",
                        Phone = "+54 9 11 4829-1029",
                        Address = "Av. Santa Fe 3200, Piso 4B",
                        City = "Palermo",
                        Province = "CABA",
                        PostalCode = "1425",
                        Notes = "Timbre 4B. Entregar en mano hoy por favor.",
                        Status = "En preparación",
                        PaymentMethod = "MercadoPago 12c",
                        ShippingMethod = "Moto Express en el día (CABA)",
                        TrackingNumber = "MOT-9402-EXP",
                        Subtotal = 1299000m,
                        ShippingCost = 0m,
                        Total = 1299000m,
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pIphone?.Id,
                                ProductName = pIphone?.Name ?? "iPhone 15 Pro",
                                ProductBrand = pIphone?.Brand ?? "Apple",
                                ProductSpec = pIphone?.Spec ?? "128 GB · Titanio",
                                ProductImageUrl = pIphone?.ImageUrl,
                                UnitPrice = 1299000m,
                                Quantity = 1,
                                TotalPrice = 1299000m
                            }
                        }
                    },
                    // 5. Pago Acreditado (hace 6 horas) - original
                    new()
                    {
                        OrderNumber = "NOD-9401",
                        TransactionToken = "TRX-9401-E42C99D5",
                        CustomerName = "Lucas Benítez",
                        DocumentId = "38291044",
                        Email = "lucas.benitez@example.com",
                        Phone = "+54 9 351 512-8833",
                        Address = "Bv. Chacabuco 640",
                        City = "Córdoba Capital",
                        Province = "Córdoba",
                        PostalCode = "5000",
                        Notes = "Dejar en portería de 9 a 18 hs.",
                        Status = "Pago Acreditado",
                        PaymentMethod = "Transferencia 5% off",
                        ShippingMethod = "Andreani Prioritario",
                        TrackingNumber = null,
                        Subtotal = 718000m,
                        ShippingCost = 0m,
                        Total = 718000m,
                        CreatedAt = DateTime.UtcNow.AddHours(-6),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pSony?.Id,
                                ProductName = pSony?.Name ?? "WH-1000XM5",
                                ProductBrand = pSony?.Brand ?? "Sony",
                                ProductSpec = pSony?.Spec ?? "Over-ear · ANC líder",
                                ProductImageUrl = pSony?.ImageUrl,
                                UnitPrice = 389000m,
                                Quantity = 1,
                                TotalPrice = 389000m
                            },
                            new()
                            {
                                ProductId = pAirpods?.Id,
                                ProductName = pAirpods?.Name ?? "AirPods Pro (2ª gen)",
                                ProductBrand = pAirpods?.Brand ?? "Apple",
                                ProductSpec = pAirpods?.Spec ?? "In-ear · H2 · ANC",
                                ProductImageUrl = pAirpods?.ImageUrl,
                                UnitPrice = 329000m,
                                Quantity = 1,
                                TotalPrice = 329000m
                            }
                        }
                    },
                    // 6. En Camino (hace 1 día) - original
                    new()
                    {
                        OrderNumber = "NOD-9400",
                        TransactionToken = "TRX-9400-B180FA29",
                        CustomerName = "Sofía Álvarez",
                        DocumentId = "41920192",
                        Email = "sofia.alvarez@example.com",
                        Phone = "+54 9 341 622-9011",
                        Address = "Bv. Oroño 1420",
                        City = "Rosario",
                        Province = "Santa Fe",
                        PostalCode = "2000",
                        Notes = "Llamar antes de entregar.",
                        Status = "En camino",
                        PaymentMethod = "Visa Bancaria",
                        ShippingMethod = "OCA Estándar",
                        TrackingNumber = "TRK-8823901",
                        Subtotal = 1399000m,
                        ShippingCost = 0m,
                        Total = 1399000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pGalaxy?.Id,
                                ProductName = pGalaxy?.Name ?? "Galaxy S24 Ultra",
                                ProductBrand = pGalaxy?.Brand ?? "Samsung",
                                ProductSpec = pGalaxy?.Spec ?? "256 GB · Snapdragon 8 Gen 3",
                                ProductImageUrl = pGalaxy?.ImageUrl,
                                UnitPrice = 1399000m,
                                Quantity = 1,
                                TotalPrice = 1399000m
                            }
                        }
                    },
                    // 7. En Camino (hace 6 días) - nuevo
                    new()
                    {
                        OrderNumber = "NOD-9397",
                        TransactionToken = "TRX-9397-3A4B5C6D",
                        CustomerName = "Facundo Ortiz",
                        DocumentId = "34567890",
                        Email = "facundo.ortiz@example.com",
                        Phone = "+54 9 341 777-8899",
                        Address = "San Lorenzo 890",
                        City = "Rosario",
                        Province = "Santa Fe",
                        PostalCode = "2000",
                        Notes = "Entregar en horario comercial.",
                        Status = "En camino",
                        PaymentMethod = "Mastercard 3c",
                        ShippingMethod = "OCA Estándar",
                        TrackingNumber = "OCA-44556677",
                        Subtotal = 599000m,
                        ShippingCost = 0m,
                        Total = 599000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pNothing?.Id,
                                ProductName = pNothing?.Name ?? "Nothing Phone (2)",
                                ProductBrand = pNothing?.Brand ?? "Nothing",
                                ProductSpec = pNothing?.Spec ?? "128 GB · Glyph · 6.7\"",
                                ProductImageUrl = pNothing?.ImageUrl,
                                UnitPrice = 599000m,
                                Quantity = 1,
                                TotalPrice = 599000m
                            }
                        }
                    },
                    // 8. Entregado (hace 2 días) - original
                    new()
                    {
                        OrderNumber = "NOD-9399",
                        TransactionToken = "TRX-9399-7D03CE64",
                        CustomerName = "Martín Gómez",
                        DocumentId = "35102948",
                        Email = "martin.gomez@example.com",
                        Phone = "+54 9 11 3910-4491",
                        Address = "Av. Cabildo 1820, Piso 2",
                        City = "Belgrano",
                        Province = "CABA",
                        PostalCode = "1426",
                        Notes = "Entregado a recepción.",
                        Status = "Entregado",
                        PaymentMethod = "Mastercard",
                        ShippingMethod = "Moto Express en el día (CABA)",
                        TrackingNumber = "EXP-99201",
                        Subtotal = 419000m,
                        ShippingCost = 0m,
                        Total = 419000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pBose?.Id,
                                ProductName = pBose?.Name ?? "QuietComfort Ultra",
                                ProductBrand = pBose?.Brand ?? "Bose",
                                ProductSpec = pBose?.Spec ?? "Over-ear · Audio espacial",
                                ProductImageUrl = pBose?.ImageUrl,
                                UnitPrice = 419000m,
                                Quantity = 1,
                                TotalPrice = 419000m
                            }
                        }
                    },
                    // 9. Entregado (hace 8 días) - nuevo
                    new()
                    {
                        OrderNumber = "NOD-9396",
                        TransactionToken = "TRX-9396-F7E8D9A0",
                        CustomerName = "Agustina Paredes",
                        DocumentId = "40123456",
                        Email = "agustina.paredes@example.com",
                        Phone = "+54 9 11 2233-4455",
                        Address = "Av. Santa Fe 2800, Piso 1",
                        City = "Recoleta",
                        Province = "CABA",
                        PostalCode = "1425",
                        Notes = "Recibido en perfectas condiciones.",
                        Status = "Entregado",
                        PaymentMethod = "MercadoPago 3c",
                        ShippingMethod = "Moto Express en el día (CABA)",
                        TrackingNumber = "MOT-9396-OK",
                        Subtotal = 429000m,
                        ShippingCost = 0m,
                        Total = 429000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-8),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pA54?.Id,
                                ProductName = pA54?.Name ?? "Galaxy A54",
                                ProductBrand = pA54?.Brand ?? "Samsung",
                                ProductSpec = pA54?.Spec ?? "128 GB · Exynos 1380 · IP67",
                                ProductImageUrl = pA54?.ImageUrl,
                                UnitPrice = 429000m,
                                Quantity = 1,
                                TotalPrice = 429000m
                            }
                        }
                    },
                    // 10. Cancelado (hace 10 días) - nuevo
                    new()
                    {
                        OrderNumber = "NOD-9395",
                        TransactionToken = "TRX-9395-B0C1D2E3",
                        CustomerName = "Sebastián Luna",
                        DocumentId = "36789012",
                        Email = "sebastian.luna@example.com",
                        Phone = "+54 9 11 9988-7766",
                        Address = "Av. Cabildo 2100",
                        City = "Belgrano",
                        Province = "CABA",
                        PostalCode = "1428",
                        Notes = "Cliente canceló por demora en stock.",
                        Status = "Cancelado",
                        PaymentMethod = "Transferencia 5% off",
                        ShippingMethod = "Andreani Prioritario",
                        TrackingNumber = null,
                        Subtotal = 999000m,
                        ShippingCost = 0m,
                        Total = 999000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pIphone15?.Id,
                                ProductName = pIphone15?.Name ?? "iPhone 15",
                                ProductBrand = pIphone15?.Brand ?? "Apple",
                                ProductSpec = pIphone15?.Spec ?? "128 GB · A16 · 6.1\" · USB-C",
                                ProductImageUrl = pIphone15?.ImageUrl,
                                UnitPrice = 999000m,
                                Quantity = 1,
                                TotalPrice = 999000m
                            }
                        }
                    },
                    // 11. Pendiente (hace 1 día) - nuevo
                    new()
                    {
                        OrderNumber = "NOD-9394",
                        TransactionToken = "TRX-9394-F4E5D6C7",
                        CustomerName = "Lucía Domínguez",
                        DocumentId = "41234567",
                        Email = "lucia.dominguez@example.com",
                        Phone = "+54 9 11 1122-3344",
                        Address = "Av. del Libertador 6200",
                        City = "Vicente López",
                        Province = "Buenos Aires",
                        PostalCode = "1638",
                        Notes = "Pendiente comprobante de pago.",
                        Status = "Pendiente",
                        PaymentMethod = "MercadoPago",
                        ShippingMethod = "Andreani Prioritario",
                        TrackingNumber = null,
                        Subtotal = 389000m,
                        ShippingCost = 0m,
                        Total = 389000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-4),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pSony?.Id,
                                ProductName = pSony?.Name ?? "WH-1000XM5",
                                ProductBrand = pSony?.Brand ?? "Sony",
                                ProductSpec = pSony?.Spec ?? "Over-ear · ANC líder",
                                ProductImageUrl = pSony?.ImageUrl,
                                UnitPrice = 389000m,
                                Quantity = 1,
                                TotalPrice = 389000m
                            }
                        }
                    },
                    // 12. Pago Acreditado (hace 3 días) - nuevo
                    new()
                    {
                        OrderNumber = "NOD-9393",
                        TransactionToken = "TRX-9393-A8B9C0D1",
                        CustomerName = "Nicolás Ramírez",
                        DocumentId = "32901234",
                        Email = "nicolas.ramirez@example.com",
                        Phone = "+54 9 351 6677-8899",
                        Address = "Rondeau 320",
                        City = "Córdoba Capital",
                        Province = "Córdoba",
                        PostalCode = "5000",
                        Notes = "Pago confirmado, preparar envío.",
                        Status = "Pago Acreditado",
                        PaymentMethod = "Visa Bancaria",
                        ShippingMethod = "Andreani Prioritario",
                        TrackingNumber = null,
                        Subtotal = 678000m,
                        ShippingCost = 0m,
                        Total = 678000m,
                        CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(-8),
                        Items = new List<OrderItem>
                        {
                            new()
                            {
                                ProductId = pJBL?.Id,
                                ProductName = pJBL?.Name ?? "JBL Live 660NC",
                                ProductBrand = pJBL?.Brand ?? "JBL",
                                ProductSpec = pJBL?.Spec ?? "Over-ear · 50 h · BT 5.3",
                                ProductImageUrl = pJBL?.ImageUrl,
                                UnitPrice = 119000m,
                                Quantity = 1,
                                TotalPrice = 119000m
                            },
                            new()
                            {
                                ProductId = pWF?.Id,
                                ProductName = pWF?.Name ?? "WF-1000XM5",
                                ProductBrand = pWF?.Brand ?? "Sony",
                                ProductSpec = pWF?.Spec ?? "In-ear · ANC · 8 h + 16 h",
                                ProductImageUrl = pWF?.ImageUrl,
                                UnitPrice = 299000m,
                                Quantity = 1,
                                TotalPrice = 299000m
                            },
                            new()
                            {
                                ProductId = pMomentum?.Id,
                                ProductName = pMomentum?.Name ?? "Momentum 4",
                                ProductBrand = pMomentum?.Brand ?? "Sennheiser",
                                ProductSpec = pMomentum?.Spec ?? "Over-ear · 60 h · aptX",
                                ProductImageUrl = pMomentum?.ImageUrl,
                                UnitPrice = 279000m,
                                Quantity = 1,
                                TotalPrice = 279000m
                            }
                        }
                    }
                };

                await context.Orders.AddRangeAsync(orders);
                await context.SaveChangesAsync();
                logger.LogInformation("12 pedidos demo de NODO sembrados correctamente con estados variados.");
            }
            catch (Exception ex)
            {
                logger.LogWarning("No se pudieron sembrar pedidos demo: {Message}", ex.Message);
            }
        }

        private static async Task SeedProductsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (context.Products.Any()) return;

            // 1. Seed Categories first
            var categories = new List<Category>
            {
                new() { Name = "Celulares", Description = "Smartphones y teléfonos móviles", Icon = "📱", DisplayOrder = 1, IsActive = true },
                new() { Name = "Auriculares", Description = "Auriculares y audio de alta fidelidad", Icon = "🎧", DisplayOrder = 2, IsActive = true },
                new() { Name = "Accesorios", Description = "Accesorios tecnológicos y complementos", Icon = "🔌", DisplayOrder = 3, IsActive = true }
            };

            foreach (var cat in categories)
            {
                if (!context.Categories.Any(c => c.Name == cat.Name))
                {
                    context.Categories.Add(cat);
                }
            }
            await context.SaveChangesAsync();

            var catCelulares = context.Categories.First(c => c.Name == "Celulares");
            var catAuriculares = context.Categories.First(c => c.Name == "Auriculares");
            var catAccesorios = context.Categories.First(c => c.Name == "Accesorios");

            var products = new List<Product>
            {
                new() { Name = "iPhone 15 Pro", Brand = "Apple", Spec = "128 GB · Titanio · A17 Pro · 6.1\"", Price = 1299000m, WasPrice = 1399000m, Badge = "Más vendido", Stock = 15, IsOffer = true, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "El equilibrio perfecto entre potencia y tamaño. Cámara 48 MP, grabación ProRes y batería para todo el día." },
                new() { Name = "Galaxy S24 Ultra", Brand = "Samsung", Spec = "256 GB · Snapdragon 8 Gen 3 · S-Pen", Price = 1399000m, WasPrice = 1599000m, Badge = "-12%", Stock = 12, IsOffer = true, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "Pantalla 6.8\" 120 Hz, zoom 100× y la mejor batería de la serie S." },
                new() { Name = "Pixel 8", Brand = "Google", Spec = "128 GB · Tensor G3 · Cámara IA", Price = 899000m, WasPrice = null, Badge = null, Stock = 8, IsOffer = false, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "Android puro, fotos nocturnas increíbles y 7 años de actualizaciones." },
                new() { Name = "iPhone 15", Brand = "Apple", Spec = "128 GB · A16 · 6.1\" · USB-C", Price = 999000m, WasPrice = null, Badge = null, Stock = 18, IsOffer = false, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "El iPhone esencial, ahora con USB-C y Dynamic Island." },
                new() { Name = "Galaxy A54", Brand = "Samsung", Spec = "128 GB · Exynos 1380 · IP67", Price = 429000m, WasPrice = 499000m, Badge = "Cuotas", Stock = 22, IsOffer = true, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "Gama media sin recortes: agua, 120 Hz y gran autonomía." },
                new() { Name = "Xiaomi 14", Brand = "Xiaomi", Spec = "256 GB · Leica · 90W", Price = 749000m, WasPrice = null, Badge = "Nuevo", Stock = 9, IsOffer = false, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "Colaboración Leica, carga en 31 min y diseño compacto." },
                new() { Name = "WH-1000XM5", Brand = "Sony", Spec = "Over-ear · ANC líder · 30 h", Price = 389000m, WasPrice = 459000m, Badge = "-15%", Stock = 14, IsOffer = true, CategoryId = catAuriculares.Id, CategoryName = "Auriculares", Description = "Cancelación de ruido referente, llamadas cristalinas y multipoint." },
                new() { Name = "AirPods Pro (2ª gen)", Brand = "Apple", Spec = "In-ear · H2 · ANC · MagSafe", Price = 329000m, WasPrice = null, Badge = null, Stock = 25, IsOffer = false, CategoryId = catAuriculares.Id, CategoryName = "Auriculares", Description = "Audio espacial, ANC adaptativo y estuche con precisión." },
                new() { Name = "QuietComfort Ultra", Brand = "Bose", Spec = "Over-ear · Audio espacial · 24 h", Price = 419000m, WasPrice = 479000m, Badge = "-12%", Stock = 6, IsOffer = true, CategoryId = catAuriculares.Id, CategoryName = "Auriculares", Description = "Confort legendario y aislamiento total, ahora con audio espacial." },
                new() { Name = "Momentum 4", Brand = "Sennheiser", Spec = "Over-ear · 60 h · aptX", Price = 279000m, WasPrice = null, Badge = "60 h", Stock = 7, IsOffer = false, CategoryId = catAuriculares.Id, CategoryName = "Auriculares", Description = "60 h de batería real, sonido audiophile y ecualizador integrado." },
                new() { Name = "Nothing Phone (2)", Brand = "Nothing", Spec = "128 GB · Glyph · 6.7\"", Price = 599000m, WasPrice = null, Badge = null, Stock = 0, IsOffer = false, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "Diseño transparente y Nothing OS ultra fluido." },
                new() { Name = "JBL Live 660NC", Brand = "JBL", Spec = "Over-ear · 50 h · BT 5.3", Price = 119000m, WasPrice = 149000m, Badge = "Oferta", Stock = 16, IsOffer = true, CategoryId = catAuriculares.Id, CategoryName = "Auriculares", Description = "Graves potentes, 50 h de autonomía y app dedicada con EQ." },
                new() { Name = "Motorola Edge 40", Brand = "Motorola", Spec = "256 GB · Dimensity 8020 · IP68", Price = 479000m, WasPrice = null, Badge = null, Stock = 11, IsOffer = false, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "Curvo, liviano, resistente al agua y con carga TurboPower." },
                new() { Name = "WF-1000XM5", Brand = "Sony", Spec = "In-ear · ANC · 8 h + 16 h", Price = 299000m, WasPrice = null, Badge = null, Stock = 10, IsOffer = false, CategoryId = catAuriculares.Id, CategoryName = "Auriculares", Description = "Los in-ear con cancelación activa más premiados del mercado." },
                new() { Name = "Galaxy Z Flip5", Brand = "Samsung", Spec = "256 GB · Plegable · 6.7\"", Price = 1099000m, WasPrice = 1299000m, Badge = "Plegable", Stock = 5, IsOffer = true, CategoryId = catCelulares.Id, CategoryName = "Celulares", Description = "Se pliega a la mitad, cabe en cualquier bolsillo y tiene pantalla externa Flex." },
                new() { Name = "AirPods Max", Brand = "Apple", Spec = "Over-ear · H1 · 20 h", Price = 599000m, WasPrice = null, Badge = null, Stock = 4, IsOffer = false, CategoryId = catAuriculares.Id, CategoryName = "Auriculares", Description = "Aluminio anodizado, audio computacional y modo ambiente inigualable." }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
            logger.LogInformation("16 productos iniciales NODO sembrados correctamente con categorías.");
        }

        private static async Task SeedUserAsync(
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            string email,
            string password,
            string fullName,
            string address,
            string role)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                // Normalizar datos básicos si están desactualizados
                bool needsUpdate = false;
                if (existing.UserName != email)
                {
                    existing.UserName = email;
                    needsUpdate = true;
                }
                if (!existing.EmailConfirmed)
                {
                    existing.EmailConfirmed = true;
                    needsUpdate = true;
                }
                if (string.IsNullOrWhiteSpace(existing.FullName))
                {
                    existing.FullName = fullName;
                    needsUpdate = true;
                }
                if (needsUpdate)
                {
                    await userManager.UpdateAsync(existing);
                    logger.LogInformation("Usuario existente normalizado: {Email}", email);
                }

                // Asegura que tenga el rol correcto
                if (!await userManager.IsInRoleAsync(existing, role))
                {
                    await userManager.AddToRoleAsync(existing, role);
                    logger.LogInformation("Rol {Role} asignado a usuario existente {Email}", role, email);
                }

                // Opción A: si el password no coincide, lo resetea (corrige user pre-existente)
                if (!await userManager.CheckPasswordAsync(existing, password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(existing);
                    var reset = await userManager.ResetPasswordAsync(existing, token, password);
                    if (reset.Succeeded)
                        logger.LogInformation("Password reseteado para usuario existente: {Email}", email);
                    else
                    {
                        // Fallback si Reset falla (ej. token provider): Remove + Add
                        await userManager.RemovePasswordAsync(existing);
                        var add = await userManager.AddPasswordAsync(existing, password);
                        if (add.Succeeded)
                            logger.LogInformation("Password re-creado para usuario existente: {Email}", email);
                        else
                            logger.LogError("Error reseteando password {Email}: {Errors}", email,
                                string.Join(", ", reset.Errors.Concat(add.Errors).Select(e => e.Description)));
                    }
                }

                // Desbloquea por si estaba bloqueado por intentos fallidos
                if (await userManager.IsLockedOutAsync(existing))
                {
                    await userManager.SetLockoutEndDateAsync(existing, null);
                    await userManager.ResetAccessFailedCountAsync(existing);
                    logger.LogInformation("Usuario desbloqueado: {Email}", email);
                }

                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Address = address
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Usuario seed creado: {Email} -> {Role}", email, role);
            }
            else
            {
                logger.LogError("Error creando usuario {Email}: {Errors}",
                    email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
