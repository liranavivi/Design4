using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface ITaskScheduledEntityRepository : IBaseRepository<TaskScheduledEntity>
{
    Task<IEnumerable<TaskScheduledEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<TaskScheduledEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<TaskScheduledEntity>> GetByNameAsync(string name);
}
