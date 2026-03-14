using Microsoft.EntityFrameworkCore;
using InventSystem.Models;

namespace InventSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Vendor> Vendors { get; set; } // ✅ Add this line


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
          

                // =========================
                // PRODUCT RELATIONSHIPS
                // =========================
                modelBuilder.Entity<Product>()
                    .HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);





            // =========================
            // ORDER RELATIONSHIPS
            // =========================
            modelBuilder.Entity<Order>()
                    .HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity<Order>()
                    .HasOne(o => o.Vendor)
                    .WithMany(v => v.Orders)
                    .HasForeignKey(o => o.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity<Order>()
                    .Property(o => o.TotalAmount)
                    .HasPrecision(18, 2);

                // =========================
                // ORDER ITEM RELATIONSHIPS
                // =========================
                modelBuilder.Entity<OrderItem>()
                    .HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<OrderItem>()
                    .HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                // =========================
                // CART ITEM RELATIONSHIPS
                // =========================
                modelBuilder.Entity<CartItem>()
                    .HasOne(ci => ci.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // =========================
                // TRANSACTION RELATIONSHIPS
                // =========================
                modelBuilder.Entity<Transaction>()
                    .HasOne(t => t.Order)
                    .WithMany(o => o.Transactions)
                    .HasForeignKey(t => t.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity<Transaction>()
                    .HasOne(t => t.User)
                    .WithMany(u => u.Transactions)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            


    // =========================
    // SEED DATA
    modelBuilder.Entity<Vendor>().HasData(
    new Vendor
    {
        VendorId = 1,
        Name = "Default Vendor",
        ContactEmail = "vendor@inventsystem.com",
        Phone = "+111111111",
        Status = "Approved",
        CreatedAt = new DateTime(2024, 01, 01)
    }
    
);

            // =========================

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    Email = "admin@inventsystem.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "Admin",
                    FullName = "System Administrator",
                    PhoneNumber = "+1234567890",
                    Address = "123 Admin Street",
                    CreatedAt = new DateTime(2024, 01, 01),
                    IsActive = true,
                }
            );

            // Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Electronics", Description = "Elect and accessories", CreatedAt = new DateTime(2024, 01, 01), IsActive = true },
                new Category { CategoryId = 2, Name = "Clothing", Description = "Fashion and apparel", CreatedAt = new DateTime(2024, 01, 01), IsActive = true },
                new Category { CategoryId = 3, Name = "Books", Description = "Books and publications", CreatedAt = new DateTime(2024, 01, 01), IsActive = true },
                new Category { CategoryId = 4, Name = "Home & Kitchen", Description = "Home and kitchen appliances", CreatedAt = new DateTime(2024, 01, 01), IsActive = true },
                new Category { CategoryId = 5, Name = "Sports", Description = "Sports equipment and accessories", CreatedAt = new DateTime(2024, 01, 01), IsActive = true }
            );

            // Products
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    ProductId = 1,
                    Name = "Wireless Mouse",
                    Description = " wireless mouse with USB receiver",
                    SKU = "ELEC-WM-001",
                    Price = 29.99M,
                    StockQuantity = 50,
                    ReorderLevel = 10,
                    CategoryId = 1,
                    ImageUrl = "mouse.jpg",
                    CreatedAt = new DateTime(2024, 01, 01),
                    IsActive = true,
                    VendorId = 1
                },
                new Product
                {
                    ProductId = 2,
                    Name = "Mechanical Keyboard",
                    Description = "RGB backlit mechanical gaming keyboard",
                    SKU = "ELEC-KB-002",
                    Price = 89.99M,
                    StockQuantity = 30,
                    ReorderLevel = 10,
                    CategoryId = 1,
                    ImageUrl = "keyboard.jpg"
,                   CreatedAt = new DateTime(2024, 01, 01),
                    IsActive = true,
                    VendorId = 1
                },
                new Product
                {
                    ProductId = 3,
                    Name = "Cotton Shirt",
                    Description = "100% cotton comfortable shirt",
                    SKU = "CLO-TS-003",
                    Price = 19.99M,
                    StockQuantity = 100,
                    ReorderLevel = 20,
                    CategoryId = 2,
                    ImageUrl = "tshirt.jpg",
                    CreatedAt = new DateTime(2024, 01, 01),
                    IsActive = true,
                    VendorId = 1
                },
                new Product
                {
                    ProductId = 4,
                    Name = "Programming Guide",
                    Description = "Complete  modern programming",
                    SKU = "BOOK-PG-004",
                    Price = 49.99M,
                    StockQuantity = 25,
                    ReorderLevel = 5,
                    CategoryId = 3,
                    ImageUrl = "book.jpg",
                    CreatedAt = new DateTime(2024, 01, 01),
                    IsActive = true,
                    VendorId = 1
                },
                new Product
                {
                    ProductId = 5,
                    Name = "Coffee Maker",
                    Description = "Automatic drip coffee maker",
                    SKU = "HOME-CM-005",
                    Price = 79.99M,
                    StockQuantity = 15,
                    ReorderLevel = 5,
                    CategoryId = 4,
                    ImageUrl =  " coffeemaker.jpg", // fixed space
                    CreatedAt = new DateTime(2024, 01, 01),
                    IsActive = true,
                    VendorId = 1
                },
                new Product
                {
                    ProductId = 6,
                    Name = "Yoga Mat",
                    Description = "Non-slip exercise yoga mat",
                    SKU = "SPORT-YM-006",
                    Price = 34.99M,
                    StockQuantity = 8,
                    ReorderLevel = 10,
                    CategoryId = 5,
                    ImageUrl = "1stimg.jpg", // fixed folder
                    CreatedAt = new DateTime(2024, 01, 01),
                    IsActive = true,
                    VendorId = 1
                }

            );
        }
    }
}
