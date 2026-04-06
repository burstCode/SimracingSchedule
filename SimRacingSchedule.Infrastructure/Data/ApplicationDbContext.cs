using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Infrastructure.Data.Configurations;

namespace SimRacingSchedule.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees { get; set; }

    public DbSet<Shift> Shifts { get; set; }

    public DbSet<ShiftExchangeRequest> ShiftExchangeRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ShiftConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ShiftExchangeRequestConfiguration());
    }
}
