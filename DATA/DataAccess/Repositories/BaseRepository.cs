using DATA.DataAccess.Context;
using DATA.DataAccess.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DATA.DataAccess.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected AppDbContext _context;
        public BaseRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<T> AddOrUpdateAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            return entity;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
            return entities;
        }

        public void Attach(T entity)
        {
            _context.Set<T>().Attach(entity);
        }

        public void Delete(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> criteria, string[] includes)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query.SingleOrDefaultAsync(criteria);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            IQueryable<T> query = _context.Set<T>();
            return await query.ToListAsync();
        }

        public async Task<T?> GetAsync(int id) => await _context.Set<T>().FindAsync(id);

        public async Task<T?> GetAsync(int id, string[] includes = null)
        {
            T? entity = await _context.Set<T>().FindAsync(id);

            if (includes != null)
                foreach (var include in includes)
                    await _context.Entry(entity).Collection(include).LoadAsync();

            return entity;
        }

        public async Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idSet = ids.ToHashSet();

            if (idSet.Count == 0)
                return [];

            return await _context.Set<T>()
                .Where(e => idSet.Contains(EF.Property<int>(e, "Id")))
                .ToListAsync();
        }

        public async Task<T?> GetFirstAsync() => await _context.Set<T>().FirstOrDefaultAsync();

        public IQueryable<T> Where(Expression<Func<T, bool>> criteria, string[] includes = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return query.Where(criteria);
        }
    }
}
