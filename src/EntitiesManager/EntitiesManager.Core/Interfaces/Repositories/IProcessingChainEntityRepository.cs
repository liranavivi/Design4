using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IProcessingChainEntityRepository : IBaseRepository<ProcessingChainEntity>
{
    Task<IEnumerable<ProcessingChainEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<ProcessingChainEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ProcessingChainEntity>> GetByNameAsync(string name);
}
