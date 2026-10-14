using JobTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(150);

        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName("IX_UserProfiles_Email");

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(u => u.RefreshToken)
               .HasMaxLength(500);

        builder.Property(u => u.IsActive)
               .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");
    }
}