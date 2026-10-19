using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.DataAccess.Context;
using JobTracker.Domain;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace JobTracker.DataAccess.Repositories.Implementations
{
    public class UserRepository : BaseRepository<UserProfile>, IUserRepository
    {
        public UserRepository(JobTrackerDbContext context) : base(context)
        {
        }

        public async Task<UserProfile?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserProfile?> GetByRefreshTokenAsync(string token)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.RefreshToken == token);
        }
    }
}