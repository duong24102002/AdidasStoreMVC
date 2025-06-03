using AdidasStoreMVC.Models; // Ensure this namespace is correct and contains the 'Product' class
using Microsoft.EntityFrameworkCore;

namespace AdidasStoreMVC.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Admin> Admins { get; set; } // Nếu cần

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
    }
}