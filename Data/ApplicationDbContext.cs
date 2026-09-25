// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    // IDataProtectionKeyContext habilita PersistKeysToDbContext, que guarda las
    // claves de cifrado de cookies/tokens en Postgres. Sin esto, las claves viven
    // en el sistema de archivos del contenedor y se pierden en cada deploy de
    // Render, lo que desloguea a todos los usuarios.
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Product>()
                .Property(p => p.WasPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Order>()
                .Property(o => o.ShippingCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Order>()
                .Property(o => o.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.TransactionToken);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.DocumentId);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreatedAt);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.UserId);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}