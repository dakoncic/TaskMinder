using System.Linq.Expressions;

namespace Infrastructure.Interfaces.IRepository
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            string includeProperties = "",
            int? skip = null,
            int? take = null);
        Task<TEntity?> GetByIdAsync(int id, string includeProperties = "");
        Task<TEntity?> GetFirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null
            );

        void Add(TEntity entity);
        void Update(TEntity entity);
        Task<int> UpdateBatchAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TEntity>> updateEntityFactory);
        void Delete(int id);
    }
}
