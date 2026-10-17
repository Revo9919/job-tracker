using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.Domain;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobTracker.DataAccess.Repositories.Implementations
{
    public class MatchResultRepository : BaseRepository<MatchResult>, IMatchResultRepository
    {
        public MatchResultRepository(JobTrackerDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MatchResult>> GetAllByUserIdAsync(int userId)
        {
            return await _dbSet.Where(m => m.UserId == userId).ToListAsync();
        }
    }
}