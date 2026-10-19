using JobTracker.Domain;
using Microsoft.EntityFrameworkCore;
namespace JobTracker.DataAccess.Context
{
    public class JobTrackerDbContext : DbContext
    {
        public JobTrackerDbContext(DbContextOptions<JobTrackerDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<CvProfile> CvProfiles { get; set; }
        public DbSet<MatchResult> MatchResults { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // Person B will add Applications and ApplicationEvents here later.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
            modelBuilder.ApplyConfiguration(new CvProfileConfiguration());
            modelBuilder.ApplyConfiguration(new MatchResultConfiguration());
            modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        }
    }
}