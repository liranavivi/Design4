using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IProcessingChainEntityRepository : IBaseRepository<ProcessingChainEntity>
{
    // GetByAddressAsync method removed since ProcessingChainEntity no longer has Address property
    Task<IEnumerable<ProcessingChainEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ProcessingChainEntity>> GetByNameAsync(string name);
    Task<IEnumerable<ProcessingChainEntity>> GetByStepIdAsync(Guid stepId);
}
