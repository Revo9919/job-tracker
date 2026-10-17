using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.Domain;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace JobTracker.DataAccess.Repositories.Implementations
{
    public class CvProfileRepository : BaseRepository<CvProfile>, ICvProfileRepository
    {
        public CvProfileRepository(JobTrackerDbContext context) : base(context)
        {
        }

        public async Task<CvProfile?> GetByUserIdAsync(int userId)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}