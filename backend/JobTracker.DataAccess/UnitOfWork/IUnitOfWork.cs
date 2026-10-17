using System;
using System.Threading.Tasks;
using JobTracker.DataAccess.Repositories.Interfaces;

namespace JobTracker.DataAccess.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        ICvProfileRepository CvProfiles { get; }
        IMatchResultRepository MatchResults { get; }
        IUserRepository Users { get; }

        // Person B will add IApplicationRepository and IEventRepository here later.

        Task<int> SaveChangesAsync();
    }
}