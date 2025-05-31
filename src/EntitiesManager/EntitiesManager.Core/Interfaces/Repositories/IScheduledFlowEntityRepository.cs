using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IScheduledFlowEntityRepository : IBaseRepository<ScheduledFlowEntity>
{
    // GetByAddressAsync method removed since ScheduledFlowEntity no longer has Address property
    Task<IEnumerable<ScheduledFlowEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ScheduledFlowEntity>> GetByNameAsync(string name);
    Task<IEnumerable<ScheduledFlowEntity>> GetBySourceIdAsync(Guid sourceId);
    Task<IEnumerable<ScheduledFlowEntity>> GetByDestinationIdAsync(Guid destinationId);
    Task<IEnumerable<ScheduledFlowEntity>> GetByFlowIdAsync(Guid flowId);
}
