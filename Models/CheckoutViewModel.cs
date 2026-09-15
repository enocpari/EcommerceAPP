using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [Display(Name = "Nombre Completo")]
        [MaxLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El carnet de identidad o DNI es obligatorio.")]
        [Display(Name = "Carnet de Identidad / DNI")]
        [MaxLength(50)]
        public string DocumentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono o WhatsApp es obligatorio.")]
        [Phone(ErrorMessage = "Ingrese un número telefónico válido.")]
        [Display(Name = "Teléfono / WhatsApp")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección de entrega es obligatoria.")]
        [Display(Name = "Dirección de Entrega")]
        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ciudad o municipio es obligatorio.")]
        [Display(Name = "Ciudad / Municipio")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "El departamento o provincia es obligatorio.")]
        [Display(Name = "Departamento / Provincia")]
        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [Display(Name = "Código Postal")]
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [Display(Name = "Notas / Referencias para la entrega")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Seleccione un método de pago.")]
        [Display(Name = "Método de Pago")]
        public string PaymentMethod { get; set; } = "QR / Transferencia Bancaria";

        [Required(ErrorMessage = "Seleccione un método de envío.")]
        [Display(Name = "Método de Envío")]
        public string ShippingMethod { get; set; } = "Envío Express a Domicilio";

        [Display(Name = "Comprobante / Nro. de Operación")]
        [MaxLength(100)]
        public string? PaymentReference { get; set; }

        /// <summary>
        /// JSON serializado con los productos acumulados en el carrito de compras
        /// </summary>
        [Required(ErrorMessage = "El carrito no contiene productos.")]
        public string ItemsJson { get; set; } = string.Empty;
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Spec { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
    }
}
