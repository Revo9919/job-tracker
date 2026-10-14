using JobTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.HasCheckConstraint("CK_AuditLogs_Action", "Action IN ('INSERT', 'UPDATE', 'DELETE')");

        builder.HasIndex(a => new { a.TableName, a.ChangedAt })
               .HasDatabaseName("IX_AuditLogs_Table");

        builder.Property(a => a.ChangedAt)
               .HasDefaultValueSql("GETUTCDATE()");
    }
}