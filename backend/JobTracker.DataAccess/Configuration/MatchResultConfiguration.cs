using JobTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MatchResultConfiguration : IEntityTypeConfiguration<MatchResult>
{
    public void Configure(EntityTypeBuilder<MatchResult> builder)
    {
        builder.ToTable("MatchResults");
        builder.HasKey(m => m.Id);

        builder.HasCheckConstraint("CK_MatchResults_Score", "MatchScore BETWEEN 0 AND 100");

        builder.HasOne<UserProfile>()
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.Property(m => m.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");
    }
}