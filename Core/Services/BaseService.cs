using Core.Exceptions;
using Core.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Core.Services
{
    public abstract class BaseService : IBaseService
    {
        public void CheckIfNull<T>([NotNull] T? entity, string errorMessage) where T : class
        {
            if (entity == null)
            {
                throw new NotFoundException(errorMessage);
            }
        }
    }

}
