using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IStepEntityRepository : IBaseRepository<StepEntity>
{
    Task<IEnumerable<StepEntity>> GetByEntityIdAsync(Guid entityId);
    Task<IEnumerable<StepEntity>> GetByNextStepIdAsync(Guid nextStepId);
}
