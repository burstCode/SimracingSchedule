using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Application.Telegram.Models;
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

        modelBuilder.Entity<TelegramUserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.HasIndex(e => e.TelegramChatId).IsUnique();
            entity.Property(e => e.NotificationMinutesBefore).HasDefaultValue(60);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.TimeZone).HasDefaultValue("Europe/Moscow");
        });
    }
}
