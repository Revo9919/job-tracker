using JobTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CvProfileConfiguration : IEntityTypeConfiguration<CvProfile>
{
    public void Configure(EntityTypeBuilder<CvProfile> builder)
    {
        builder.ToTable("CvProfiles");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.UserId)
               .IsUnique()
               .HasDatabaseName("IX_CvProfiles_UserId");

        builder.Property(c => c.SkillsText)
               .IsRequired();

        builder.HasOne<UserProfile>()
               .WithOne()
               .HasForeignKey<CvProfile>(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}