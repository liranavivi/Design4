using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IFlowEntityRepository : IBaseRepository<FlowEntity>
{
    // GetByAddressAsync method removed since FlowEntity no longer has Address property
    Task<IEnumerable<FlowEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<FlowEntity>> GetByNameAsync(string name);
    Task<IEnumerable<FlowEntity>> GetByStepIdAsync(Guid stepId);
}
