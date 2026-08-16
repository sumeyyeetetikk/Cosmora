using Cosmora.Models;
using Microsoft.EntityFrameworkCore;

namespace Cosmora.Context
{
    public class CosmoraDbContext : DbContext
    {
        public CosmoraDbContext(DbContextOptions<CosmoraDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<Sale> Sales => Set<Sale>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(e =>
            {
                e.Property(c => c.Name).HasMaxLength(100).IsRequired();
                e.HasIndex(c => c.Name).IsUnique();
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.Property(p => p.Name).HasMaxLength(150).IsRequired();
                e.Property(p => p.BaseUnitPrice).HasColumnType("decimal(18,2)");
                e.HasOne(p => p.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(p => p.Name);
            });

            modelBuilder.Entity<City>(e =>
            {
                e.Property(c => c.Name).HasMaxLength(100).IsRequired();
                e.Property(c => c.Country).HasMaxLength(100).IsRequired();
                e.HasIndex(c => new { c.Country, c.Name });
            });

            modelBuilder.Entity<Sale>(e =>
            {
                e.Property(s => s.UnitPrice).HasColumnType("decimal(18,2)");
                e.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
                e.Property(s => s.DiscountRate).HasColumnType("decimal(5,4)");
                e.Property(s => s.PaymentMethod)
                 .HasConversion<string>()   
                 .HasMaxLength(20);

                e.HasOne(s => s.Product)
                 .WithMany(p => p.Sales)
                 .HasForeignKey(s => s.ProductId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.City)
                 .WithMany(c => c.Sales)
                 .HasForeignKey(s => s.CityId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(s => new { s.CityId, s.OrderDate });
                e.HasIndex(s => new { s.ProductId, s.CityId, s.OrderDate });
                e.HasIndex(s => s.OrderDate);
            });
        }
    }
}