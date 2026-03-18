using Microsoft.EntityFrameworkCore;
using bookShop.Models;

namespace bookShop.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = "server=localhost;port=3306;database=bookshopdb;user=root;password=1234";
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
}