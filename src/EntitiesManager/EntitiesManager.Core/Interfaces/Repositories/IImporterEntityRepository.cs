using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IImporterEntityRepository : IBaseRepository<ImporterEntity>
{
    // GetByAddressAsync method removed since ImporterEntity no longer has Address property
    Task<IEnumerable<ImporterEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ImporterEntity>> GetByNameAsync(string name);
}
