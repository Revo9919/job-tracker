using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.Domain;
using System.Threading.Tasks;

namespace JobTracker.DataAccess.Repositories.Interfaces
{
    public interface ICvProfileRepository : IRepository<CvProfile>
    {
        Task<CvProfile?> GetByUserIdAsync(int userId);
    }
}