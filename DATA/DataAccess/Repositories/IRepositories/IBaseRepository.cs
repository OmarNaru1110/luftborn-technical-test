using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DATA.DataAccess.Repositories.IRepositories
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T?> GetAsync(int id);
        Task<T?> GetFirstAsync();
        Task<T?> GetAsync(int id, string[] includes = null);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<int> ids);
        Task<T> FindAsync(Expression<Func<T, bool>> criteria, string[] includes = null);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, string[] includes = null);
        Task<T> AddOrUpdateAsync(T entity);
        void Delete(T entity);
        void Attach(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        IQueryable<T> Where(Expression<Func<T, bool>> criteria, string[] includes = null);
    }
}
