using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.Domain;
using System.Threading.Tasks;

namespace JobTracker.DataAccess.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<UserProfile>
    {
        Task<UserProfile?> GetByEmailAsync(string email);
        Task<UserProfile?> GetByRefreshTokenAsync(string token);
    }
}