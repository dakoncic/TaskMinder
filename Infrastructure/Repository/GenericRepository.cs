using Infrastructure.DAL;
using Infrastructure.Entities;
using Infrastructure.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Z.EntityFramework.Plus;

namespace Infrastructure.Repository
{
    public class GenericRepository<TEntity, TKeyType> : IGenericRepository<TEntity, TKeyType>
        where TEntity : BaseEntity<TKeyType>
    {
        protected readonly MyFeaturesDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(MyFeaturesDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        //ako želim naglasit da će se potencijalno ova lista dalje query-at tamo gdje se zove
        //onda maknut IEnumerable i ne vratit ToListAsync() nego IQueryable npr.
        public async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "",
            int? skip = null,
            int? take = null)
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<TEntity> GetByIdAsync(TKeyType id, string includeProperties = "")
        {
            IQueryable<TEntity> query = _dbSet;

            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            return await query.FirstOrDefaultAsync(e => e.Id.Equals(id));
        }

        public async Task<TEntity> GetFirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null
            )
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.FirstOrDefaultAsync();
        }

        public void Add(TEntity entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(TEntity entity)
        {
            _dbSet.Attach(entity);

            //eksplicitno attachamo/markamo entity u context, tako ako koristimo AsNoTracking()
            //da se entity prepozna kao modified za slučaj kad ga AsNoTracking() više
            //neće automatski pazit pa da se može spremit normalno u bazu
            //*AsNoTracking() se ne smije na tablici gdje pazimo na optimistic concurrency control
            _context.Entry(entity).State = EntityState.Modified;
        }

        public async Task<int> UpdateBatchAsync(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TEntity>> updateEntityFactory)
        {
            return await _dbSet.Where(filter).UpdateAsync(updateEntityFactory);
        }

        public void Delete(TKeyType id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }
    }
}
