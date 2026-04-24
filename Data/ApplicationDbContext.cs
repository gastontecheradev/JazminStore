using Jazmin.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jazmin.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // decimal precision for SQLite
        b.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(12,2)");
        b.Entity<Product>().Property(p => p.ComparePrice).HasColumnType("decimal(12,2)");
        b.Entity<Order>().Property(o => o.Subtotal).HasColumnType("decimal(12,2)");
        b.Entity<Order>().Property(o => o.Total).HasColumnType("decimal(12,2)");
        b.Entity<Order>().Property(o => o.ShippingCost).HasColumnType("decimal(12,2)");
        b.Entity<OrderItem>().Property(o => o.UnitPrice).HasColumnType("decimal(12,2)");

        // indices
        b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
        b.Entity<Favorite>().HasIndex(f => new { f.UserId, f.ProductId }).IsUnique();

        // relations
        b.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<ProductImage>()
            .HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CartItem>()
            .HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Favorite>()
            .HasOne(f => f.Product)
            .WithMany(p => p.Favorites)
            .HasForeignKey(f => f.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<OrderItem>()
            .HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<OrderItem>()
            .HasOne(i => i.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Review>()
            .HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Review>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
