using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IAssignmentEntityRepository : IBaseRepository<AssignmentEntity>
{
    Task<IEnumerable<AssignmentEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<AssignmentEntity>> GetByNameAsync(string name);
    Task<AssignmentEntity?> GetByStepIdAsync(Guid stepId);
    Task<IEnumerable<AssignmentEntity>> GetByEntityIdAsync(Guid entityId);
}
