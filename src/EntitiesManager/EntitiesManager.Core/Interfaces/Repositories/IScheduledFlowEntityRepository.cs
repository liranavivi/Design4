using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IScheduledFlowEntityRepository : IBaseRepository<ScheduledFlowEntity>
{
    Task<IEnumerable<ScheduledFlowEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<ScheduledFlowEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ScheduledFlowEntity>> GetByNameAsync(string name);
}
