using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.DataAccess.Context;

namespace JobTracker.DataAccess.Repositories.Implementations
{
    public class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly JobTrackerDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(JobTrackerDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }
    }
}