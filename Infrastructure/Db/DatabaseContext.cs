using Microsoft.EntityFrameworkCore;
using minimal_api.Domain.Entities;
using minimal_api.Domain.Enums;

namespace minimal_api.Infrastructure.Db
{
    public class DatabaseContext : DbContext
    {
        private readonly IConfiguration _configurationAppSettings;
        public DbSet<User> Users { get; set; } = default;
        public DbSet<Vehicle> Vehicles { get; set; } = default;
        public DatabaseContext(IConfiguration configurationAppSettings)
        {
            _configurationAppSettings = configurationAppSettings;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = _configurationAppSettings.GetConnectionString("mysql")?.ToString();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                }
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Email = "admin@api.com",
                    Password = "123456789",
                    Role = Role.ADMIN.ToString()
                }
            );
        }
    }
}