using Microsoft.EntityFrameworkCore;
using bookShop.Models;

namespace bookShop.Data;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Books> Books => Set<Books>();
    public DbSet<Customers> Customers => Set<Customers>();
    public DbSet<Orders> Orders => Set<Orders>();
    public DbSet<OrderDetails> OrderDetails => Set<OrderDetails>();

    private const string ConnectionString = "server=localhost;port=3306;database=bookshopdb;user=root;password=1234";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString),
                mysqlOptions => mysqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Orders>()
            .HasMany(o => o.Details)
            .WithOne(d => d.Order)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDetails>()
            .Property(d => d.ItemName)
            .IsRequired();
    }
}