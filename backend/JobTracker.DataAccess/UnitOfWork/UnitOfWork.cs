using System.Threading.Tasks;
using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.DataAccess.Repositories.Implementations;

namespace JobTracker.DataAccess.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly JobTrackerDbContext _context;

        public ICvProfileRepository CvProfiles { get; }
        public IMatchResultRepository MatchResults { get; }
        public IUserRepository Users { get; }

        public UnitOfWork(JobTrackerDbContext context)
        {
            _context = context;
            CvProfiles = new CvProfileRepository(_context);
            MatchResults = new MatchResultRepository(_context);
            Users = new UserRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}