using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Url(ErrorMessage = "Debe ser una URL válida (http:// o https://)")]
        [Display(Name = "Enlace de la imagen")]
        [MaxLength(2048)]
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }

        // NODO extensions (nullable para no romper datos existentes)
        [MaxLength(50)]
        public string? Brand { get; set; }

        [MaxLength(120)]
        public string? Spec { get; set; }

        public decimal? WasPrice { get; set; }

        [MaxLength(30)]
        public string? Badge { get; set; }

        public bool IsOffer { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}