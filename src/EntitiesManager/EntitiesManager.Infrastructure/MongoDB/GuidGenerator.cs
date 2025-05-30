using MongoDB.Bson.Serialization;

namespace EntitiesManager.Infrastructure.MongoDB;

public class GuidGenerator : IIdGenerator
{
    public object GenerateId(object container, object document)
    {
        return Guid.NewGuid();
    }
    
    public bool IsEmpty(object id)
    {
        return id == null || (Guid)id == Guid.Empty;
    }
}
