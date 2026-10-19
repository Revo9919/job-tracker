using System;
using System.Threading.Tasks;

namespace JobTracker.DataAccess.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> CompleteAsync();
    }
}