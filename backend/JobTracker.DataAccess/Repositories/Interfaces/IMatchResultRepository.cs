using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobTracker.DataAccess.Repositories.Interfaces
{
    public interface IMatchResultRepository : IRepository<MatchResult>
    {
        Task<IEnumerable<MatchResult>> GetAllByUserIdAsync(int userId);
    }
}