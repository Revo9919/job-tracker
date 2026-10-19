using System.Threading.Tasks;
using JobTracker.DataAccess.Context;

namespace JobTracker.DataAccess.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly JobTrackerDbContext _context;

        public UnitOfWork(JobTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}