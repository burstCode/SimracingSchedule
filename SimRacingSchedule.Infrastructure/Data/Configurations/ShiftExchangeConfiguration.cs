using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Infrastructure.Data.Configurations;

public class ShiftExchangeRequestConfiguration : IEntityTypeConfiguration<ShiftExchangeRequest>
{
    public void Configure(EntityTypeBuilder<ShiftExchangeRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.RequesterId, r.TargetId, r.Status });
        builder.HasIndex(r => r.Status);

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(ExchangeRequestStatus.Pending);

        builder.Property(r => r.RequestMessage)
            .HasMaxLength(500);

        builder.Property(r => r.ResponseMessage)
            .HasMaxLength(500);

        builder.HasOne(r => r.Requester)
            .WithMany(e => e.SentExchangeRequests)
            .HasForeignKey(r => r.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Target)
            .WithMany(e => e.ReceivedExchangeRequests)
            .HasForeignKey(r => r.TargetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RequesterShift)
            .WithMany(s => s.SentExchangeRequests)
            .HasForeignKey(r => r.RequesterShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TargetShift)
            .WithMany(s => s.ReceivedExchangeRequests)
            .HasForeignKey(r => r.TargetShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
