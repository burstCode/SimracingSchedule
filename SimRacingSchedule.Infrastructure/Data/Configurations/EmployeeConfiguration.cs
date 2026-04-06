// Copyright (c) SimRacing Club. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimRacingSchedule.Core.Entities;

namespace SimRacingSchedule.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        _ = builder.HasKey(e => e.Id);

        _ = builder.HasIndex(e => e.Email).IsUnique();
        _ = builder.HasIndex(e => e.PhoneNumber).IsUnique();

        _ = builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        _ = builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        _ = builder.Property(e => e.Patronymic)
            .HasMaxLength(100);

        _ = builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(200);

        _ = builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        _ = builder.Property(e => e.Position)
            .IsRequired()
            .HasMaxLength(100);

        _ = builder.Property(e => e.Role)
            .HasConversion<int>()
            .IsRequired();

        _ = builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        _ = builder.HasMany(e => e.Shifts)
            .WithOne(s => s.Employee)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
