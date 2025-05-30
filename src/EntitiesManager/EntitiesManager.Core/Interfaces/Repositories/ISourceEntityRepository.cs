using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface ISourceEntityRepository : IBaseRepository<SourceEntity>
{
    Task<IEnumerable<SourceEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<SourceEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<SourceEntity>> GetByNameAsync(string name);
}
