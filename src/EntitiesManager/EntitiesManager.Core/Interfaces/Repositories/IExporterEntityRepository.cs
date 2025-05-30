using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IExporterEntityRepository : IBaseRepository<ExporterEntity>
{
    // GetByAddressAsync method removed since ExporterEntity no longer has Address property
    Task<IEnumerable<ExporterEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ExporterEntity>> GetByNameAsync(string name);
}
