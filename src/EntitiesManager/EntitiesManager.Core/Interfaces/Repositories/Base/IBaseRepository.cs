using EntitiesManager.Core.Entities.Base;

namespace EntitiesManager.Core.Interfaces.Repositories.Base;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetByCompositeKeyAsync(string compositeKey);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize);
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string compositeKey);
    Task<bool> ExistsByIdAsync(Guid id);
    Task<long> CountAsync();
}
