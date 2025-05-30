using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IProcessorEntityRepository : IBaseRepository<ProcessorEntity>
{
    // GetByAddressAsync method removed since ProcessorEntity no longer has Address property
    Task<IEnumerable<ProcessorEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ProcessorEntity>> GetByNameAsync(string name);
}
