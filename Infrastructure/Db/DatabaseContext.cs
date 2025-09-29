using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using minimal_api.Domain.Entities;

namespace minimal_api.Infrastructure.Db
{
    public class DatabaseContext : DbContext
    {
        private readonly IConfiguration _configurationAppSettings;
        public DbSet<Administrator> Administrators { get; set; } = default;
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
            modelBuilder.Entity<Administrator>().HasData(
                new Administrator
                {
                    Id = 1,
                    Email = "dev@tester.com",
                    Password = "123456789",
                    Role = "ADM"
                }
            );
        }
    }
}