using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "N° de Pedido")]
        public string OrderNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Token de Transacción / Recibo")]
        public string TransactionToken { get; set; } = string.Empty;

        public string? UserId { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido.")]
        [MaxLength(150)]
        [Display(Name = "Nombre Completo")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El carnet de identidad o documento es requerido.")]
        [MaxLength(50)]
        [Display(Name = "Carnet de Identidad / DNI")]
        public string DocumentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [MaxLength(150)]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono / WhatsApp es requerido.")]
        [Phone(ErrorMessage = "Ingrese un teléfono válido.")]
        [MaxLength(50)]
        [Display(Name = "Teléfono / WhatsApp")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección de entrega es requerida.")]
        [MaxLength(250)]
        [Display(Name = "Dirección de Entrega")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ciudad es requerida.")]
        [MaxLength(100)]
        [Display(Name = "Ciudad / Municipio")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "La provincia o departamento es requerido.")]
        [MaxLength(100)]
        [Display(Name = "Departamento / Provincia")]
        public string Province { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Código Postal")]
        public string? PostalCode { get; set; }

        [MaxLength(500)]
        [Display(Name = "Notas / Instrucciones")]
        public string? Notes { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "Estado")]
        public string Status { get; set; } = "Pendiente";

        [Required, MaxLength(100)]
        [Display(Name = "Método de Pago")]
        public string PaymentMethod { get; set; } = "QR / Transferencia";

        [Required, MaxLength(100)]
        [Display(Name = "Método de Envío")]
        public string ShippingMethod { get; set; } = "Envío Express a Domicilio";

        [MaxLength(100)]
        [Display(Name = "Código de Seguimiento")]
        public string? TrackingNumber { get; set; }

        [MaxLength(100)]
        [Display(Name = "Comprobante / Nro. de Operación")]
        public string? PaymentReference { get; set; }

        [MaxLength(500)]
        [Display(Name = "Notas Administrativas")]
        public string? AdminNotes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Subtotal")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Costo de Envío")]
        public decimal ShippingCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Última Actualización")]
        public DateTime? UpdatedAt { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }

    public static class OrderStatus
    {
        public const string Pending = "Pendiente";
        public const string Paid = "Pago Aprobado";
        public const string Processing = "En Preparación";
        public const string Shipped = "En Camino";
        public const string Delivered = "Entregado";
        public const string Cancelled = "Cancelado";

        public static readonly string[] All = [Pending, Paid, Processing, Shipped, Delivered, Cancelled];
    }
}
