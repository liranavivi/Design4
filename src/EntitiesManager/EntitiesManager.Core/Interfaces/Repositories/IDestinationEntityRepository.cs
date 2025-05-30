using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface IDestinationEntityRepository : IBaseRepository<DestinationEntity>
{
    Task<IEnumerable<DestinationEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<DestinationEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<DestinationEntity>> GetByNameAsync(string name);
}
