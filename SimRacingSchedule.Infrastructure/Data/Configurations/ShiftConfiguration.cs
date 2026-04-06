using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Infrastructure.Data.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        _ = builder.HasKey(s => s.Id);

        _ = builder.HasIndex(s => new { s.EmployeeId, s.StartTime });
        _ = builder.HasIndex(s => s.Status);

        _ = builder.Property(s => s.Type)
            .HasConversion<int>()
            .IsRequired();

        _ = builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(ShiftStatus.Scheduled);

        _ = builder.Property(s => s.Notes)
            .HasMaxLength(500);

        _ = builder.Property(s => s.StartTime)
            .IsRequired();

        _ = builder.Property(s => s.EndTime)
            .IsRequired();
    }
}
