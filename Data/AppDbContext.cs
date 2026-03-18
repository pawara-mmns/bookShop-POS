using Microsoft.EntityFrameworkCore;
using bookShop.Models;

namespace bookShop.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    private const string ConnectionString = "server=localhost;port=3306;database=bookshopdb;user=root;password=1234";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString),
            mysqlOptions => mysqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
    }
}