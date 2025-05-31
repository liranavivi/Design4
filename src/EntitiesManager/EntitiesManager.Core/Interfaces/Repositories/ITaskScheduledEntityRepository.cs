using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface ITaskScheduledEntityRepository : IBaseRepository<TaskScheduledEntity>
{
    Task<TaskScheduledEntity?> GetByVersionAsync(string version);
    Task<IEnumerable<TaskScheduledEntity>> GetByScheduledFlowIdAsync(Guid scheduledFlowId);
    Task<IEnumerable<TaskScheduledEntity>> GetByNameAsync(string name);
}
