using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IFlowEntityRepository : IBaseRepository<FlowEntity>
{
    Task<IEnumerable<FlowEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<FlowEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<FlowEntity>> GetByNameAsync(string name);
}
