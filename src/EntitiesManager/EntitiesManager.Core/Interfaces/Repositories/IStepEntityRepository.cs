using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IStepEntityRepository : IBaseRepository<StepEntity>
{
    Task<IEnumerable<StepEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<StepEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<StepEntity>> GetByNameAsync(string name);
}
