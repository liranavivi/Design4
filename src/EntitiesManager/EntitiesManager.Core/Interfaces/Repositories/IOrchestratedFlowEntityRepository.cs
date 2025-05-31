using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IOrchestratedFlowEntityRepository : IBaseRepository<OrchestratedFlowEntity>
{
    Task<IEnumerable<OrchestratedFlowEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<OrchestratedFlowEntity>> GetByNameAsync(string name);
    Task<IEnumerable<OrchestratedFlowEntity>> GetByAssignmentIdAsync(Guid assignmentId);
    Task<IEnumerable<OrchestratedFlowEntity>> GetByFlowIdAsync(Guid flowId);
}
