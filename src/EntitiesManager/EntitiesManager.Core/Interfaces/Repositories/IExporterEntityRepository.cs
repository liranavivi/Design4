using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IExporterEntityRepository : IBaseRepository<ExporterEntity>
{
    Task<IEnumerable<ExporterEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<ExporterEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<ExporterEntity>> GetByNameAsync(string name);
}
