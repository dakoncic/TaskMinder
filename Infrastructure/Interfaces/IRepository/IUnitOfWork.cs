namespace Infrastructure.Interfaces.IRepository
{
    public interface IUnitOfWork
    {
        bool HasChanges { get; }
        Task SaveChangesAsync();
    }
}