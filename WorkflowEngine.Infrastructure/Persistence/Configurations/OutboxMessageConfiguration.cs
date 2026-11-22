using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Infrastructure.Persistence.Entities;

namespace Workflow.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.EventId).ValueGeneratedNever();
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.NextAttemptAtUtc).IsRequired();
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.TotalAttemptCount).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.LeaseToken);
        builder.Property(x => x.LeaseExpiresAtUtc);
        builder.Property(x => x.LastAttemptAtUtc);
        builder.Property(x => x.LastError).HasMaxLength(4000);
        builder.Property(x => x.DeadLetterAtUtc);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc })
            .HasDatabaseName("IX_OutboxMessages_Status_NextAttemptAtUtc");
        builder.HasIndex(x => new { x.Status, x.LeaseExpiresAtUtc })
            .HasDatabaseName("IX_OutboxMessages_Status_LeaseExpiresAtUtc");
    }
}
