// SimRacingSchedule.Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Infrastructure.Data.Configurations;

namespace SimRacingSchedule.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<ShiftExchangeRequest> ShiftExchangeRequests { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new ShiftConfiguration());
        modelBuilder.ApplyConfiguration(new ShiftExchangeRequestConfiguration());

        // Seed data для начальных значений
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Добавляем администратора для тестов
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        modelBuilder.Entity<Employee>().HasData(new
        {
            Id = adminId,
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@simracing.com",
            PhoneNumber = "+7999999999",
            Position = "System Administrator",
            Role = EmployeeRole.Administrator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }
}
