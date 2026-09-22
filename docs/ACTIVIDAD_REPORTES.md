# Actividad — Diseño de Reportes

## 1. Objetivo

Implementar un módulo profesional de reportería para un e-commerce universitario (tienda de celulares, auriculares y accesorios tecnológicos) que permita al administrador generar reportes en PDF con datos reales provenientes de PostgreSQL (Supabase) mediante Entity Framework Core. El módulo cumple con los requisitos de la actividad universitaria de reportería, incluyendo estructura Header/Detail/Footer, agregaciones (totales, sumatorias, promedios), filtros y exportación a PDF.

## 2. Fuente de datos

- **Framework**: ASP.NET Core 10 (MVC)
- **ORM**: Entity Framework Core 10
- **Base de datos**: PostgreSQL alojada en Supabase
- **Conexión**: Cadena de conexión segura mediante User Secrets / Variables de entorno (no hardcodeada en repositorio)
- **Consultas**: LINQ con `AsNoTracking()` para reportes de solo lectura, proyecciones eficientes, sin consultas N+1

Entidades principales consultadas:
- `Order` + `OrderItem` (para Reporte de Ventas)
- `Product` + `Category` (para Reporte de Productos e Inventario)
- `ApplicationUser` (para datos de cliente en pedidos)

## 3. Reporte seleccionado

**Reporte de Ventas** — Reporte principal de la actividad universitaria.

## 4. Estructura del reporte

### HEADER
- Nombre de la tienda: **NODO** (configurable vía `StoreConfigService`)
- Logo/inicial de la marca: **N**
- Título: **REPORTE DE VENTAS**
- Fecha y hora de generación: `dd/MM/yyyy HH:mm`
- Filtros aplicados: Fecha inicial, Fecha final, Estado del pedido

### DETAIL
Tabla con columnas alineadas y bordes:
| Columna | Formato |
|---------|---------|
| N° Pedido | `#NOD-XXXXX` (monoespacio) |
| Fecha | `dd/MM/yyyy` |
| Cliente | Nombre completo |
| Email | Correo electrónico |
| Estado | Badge coloreado según estado |
| Método Pago | Texto |
| Cant. Prod. | Entero centrado |
| Subtotal | `Bs #,##0.00` (alineado derecha) |
| Envío | `Bs #,##0.00` (alineado derecha) |
| Total | `Bs #,##0.00` (alineado derecha, negrita, color azul) |

### FOOTER
- Total pedidos: `{TotalOrders}`
- Productos vendidos: `{TotalProductsSold}`
- Ventas totales: `Bs {TotalSales:N2}`
- Ticket promedio: `Bs {AverageTicket:N2}`
- Paginación: `Página X de Y · Generado por NODO Admin`

## 5. Campos utilizados

| Campo DTO | Origen | Descripción |
|-----------|--------|-------------|
| `OrderNumber` | `Order.OrderNumber` | Número único de pedido |
| `Date` | `Order.CreatedAt` | Fecha de creación |
| `CustomerName` | `Order.CustomerName` | Nombre del cliente |
| `Email` | `Order.Email` | Correo del cliente |
| `Status` | `Order.Status` | Estado logístico |
| `PaymentMethod` | `Order.PaymentMethod` | Método de pago |
| `ProductsQuantity` | `SUM(OrderItem.Quantity)` | Cantidad total de items |
| `Subtotal` | `Order.Subtotal` | Subtotal del pedido |
| `ShippingCost` | `Order.ShippingCost` | Costo de envío |
| `Total` | `Order.Total` | Total del pedido |

## 6. Formatos

| Tipo | Formato | Ejemplo |
|------|---------|---------|
| Fecha | `dd/MM/yyyy` | `15/09/2026` |
| Fecha/Hora generación | `dd/MM/yyyy HH:mm` | `15/09/2026 14:30` |
| Moneda (Bolivianos) | `Bs #,##0.00` | `Bs 1.250,00` |
| Enteros | `#,##0` | `1,250` |
| Texto estado | Badge coloreado | Pendiente, Entregado, etc. |

## 7. Agregaciones

| Agregación | Fórmula | Descripción |
|------------|---------|-------------|
| Total pedidos | `COUNT(Orders)` | Cantidad de registros en el filtro |
| Total productos vendidos | `SUM(OrderItem.Quantity)` | Unidades totales |
| Ventas totales | `SUM(Order.Total) WHERE Status != 'Cancelado'` | Suma de totales válidos |
| Ticket promedio | `Ventas totales / Total pedidos` | Promedio por pedido válido |

## 8. Tecnología

| Componente | Versión | Uso |
|------------|---------|-----|
| **QuestPDF** | 2026.9.0 | Generación de PDF nativo (fluent API) |
| **FastReport OpenSource** | 2026.2.8 | Plantillas `.frx` físicas para cumplimiento actividad |
| **Entity Framework Core** | 10.0.12 | Acceso a datos, consultas LINQ |
| **PostgreSQL (Supabase)** | 16+ | Base de datos relacional |
| **ASP.NET Core MVC** | 10.0 | Framework web |

> **Nota**: Las plantillas `.frx` (`SalesReport.frx`, `ProductsReport.frx`, `InventoryReport.frx`) existen físicamente en `/Reports` como requisito de la actividad universitaria. La generación de PDF en la aplicación utiliza **QuestPDF** por estabilidad y compatibilidad con .NET 10, manteniendo idéntica estructura visual (Header/Detail/Footer) y datos.

## 9. Evidencias

> **Instrucción**: Reemplazar los marcadores `[CAPTURA]` con capturas de pantalla reales al preparar la entrega.

### 9.1 Pantalla de Reportes (Admin)
![Pantalla Reportes]([CAPTURA: Admin/Reports/Index.cshtml - 3 tarjetas con filtros y botones])

### 9.2 Reporte de Ventas generado (HTML)
![Reporte Ventas HTML]([CAPTURA: Admin/Reports/Sales - tabla con datos, header, footer])

### 9.3 Reporte de Ventas en PDF
![Reporte Ventas PDF]([CAPTURA: PDF descargado - header, detail, footer, paginación])

### 9.4 Reporte de Productos en PDF
![Reporte Productos PDF]([CAPTURA: PDF catálogo productos])

### 9.5 Reporte de Inventario en PDF
![Reporte Inventario PDF]([CAPTURA: PDF inventario con estados visuales])

### 9.6 Base de datos - Tablas consultadas
```sql
-- Pedidos con items
SELECT * FROM "Orders" o JOIN "OrderItems" oi ON o."Id" = oi."OrderId";

-- Productos con categorías
SELECT p.*, c."Name" as CategoryName FROM "Products" p LEFT JOIN "Categories" c ON p."CategoryId" = c."Id";
```

### 9.7 Estructura del proyecto
```
EcommerceApp/
├── Controllers/
│   └── ReportsController.cs
├── Models/
│   └── Reports/
│       └── ReportModels.cs
├── Services/
│   ├── IReportService.cs
│   └── ReportService.cs
├── Reports/
│   ├── SalesReport.frx
│   ├── ProductsReport.frx
│   └── InventoryReport.frx
├── Views/
│   └── Admin/
│       └── Reports/
│           ├── Index.cshtml
│           ├── Sales.cshtml
│           ├── Products.cshtml
│           └── Inventory.cshtml
└── docs/
    └── ACTIVIDAD_REPORTES.md
```

## 10. Conclusión

Se implementó exitosamente un módulo de reportería completo para el e-commerce NODO, cumpliendo con todos los requisitos de la actividad universitaria:

1. **Separación de responsabilidades**: DTOs, Service, Controller, Views, Plantillas .frx
2. **Datos reales**: Consultas EF Core optimizadas (`AsNoTracking`, proyecciones, sin N+1)
3. **Estructura profesional**: Header (tienda, título, fecha, filtros), Detail (tabla con bordes, formato monetario Bs), Footer (agregaciones + paginación)
4. **Tres reportes funcionales**: Ventas (principal), Productos, Inventario
5. **Exportación PDF**: Generación nativa via QuestPDF, plantillas .frx versionables en `/Reports`
6. **Filtros dinámicos**: Por fecha inicial/final y estado en Reporte de Ventas
7. **Seguridad**: Solo rol Admin, conexión segura sin passwords en repo
8. **Datos demo**: 12 pedidos con 6 estados distintos, 16 productos en 3 categorías

El código es mantenible, simple de explicar y listo para entregar comprimido.