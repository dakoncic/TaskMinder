using Infrastructure.DAL;
using Infrastructure.Interfaces.IRepository;

namespace Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MyFeaturesDbContext _context;

        public UnitOfWork(MyFeaturesDbContext context)
        {
            _context = context;
        }

        public bool HasChanges => _context.ChangeTracker.HasChanges();

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}