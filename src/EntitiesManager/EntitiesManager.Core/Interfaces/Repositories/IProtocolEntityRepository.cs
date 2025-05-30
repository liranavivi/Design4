using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IProtocolEntityRepository : IBaseRepository<ProtocolEntity>
{
    // GetByVersionAsync method removed since ProtocolEntity no longer has Version property
    Task<IEnumerable<ProtocolEntity>> GetByNameAsync(string name);
}
