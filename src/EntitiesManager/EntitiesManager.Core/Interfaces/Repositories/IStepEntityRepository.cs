using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IStepEntityRepository : IBaseRepository<StepEntity>
{
    // GetByAddressAsync, GetByVersionAsync, and GetByNameAsync methods removed
    // since StepEntity no longer has these properties
    Task<IEnumerable<StepEntity>> GetByEntityIdAsync(Guid entityId);
    Task<IEnumerable<StepEntity>> GetByNextStepIdAsync(Guid nextStepId);
}
