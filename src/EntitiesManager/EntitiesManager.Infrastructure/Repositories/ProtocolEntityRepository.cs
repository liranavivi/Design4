using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ProtocolEntityRepository : BaseRepository<ProtocolEntity>, IProtocolEntityRepository
{
    public ProtocolEntityRepository(IMongoDatabase database, ILogger<ProtocolEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "protocols", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<ProtocolEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // ProtocolEntity now uses only Name as composite key
        return Builders<ProtocolEntity>.Filter.Eq(x => x.Name, compositeKey);
    }

    protected override void CreateIndexes()
    {
        try
        {
            // Name index for uniqueness (since Name is now the composite key)
            var nameIndex = Builders<ProtocolEntity>.IndexKeys.Ascending(x => x.Name);
            var indexOptions = new CreateIndexOptions { Unique = true, Name = "protocol_name_unique" };
            _collection.Indexes.CreateOne(new CreateIndexModel<ProtocolEntity>(nameIndex, indexOptions));
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("existing index"))
        {
            // Index already exists, ignore the error
            _logger.LogInformation("Index already exists for ProtocolEntity, skipping creation");
        }
    }

    // Note: GetByVersionAsync method removed since ProtocolEntity no longer has Version property
    // This method is now obsolete and should be removed from the interface as well

    public async Task<IEnumerable<ProtocolEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ProtocolEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(ProtocolEntity entity)
    {
        var createdEvent = new ProtocolCreatedEvent
        {
            Id = entity.Id,
            Version = string.Empty, // Version no longer exists on ProtocolEntity
            Name = entity.Name,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ProtocolEntity entity)
    {
        var updatedEvent = new ProtocolUpdatedEvent
        {
            Id = entity.Id,
            Version = string.Empty, // Version no longer exists on ProtocolEntity
            Name = entity.Name,
            Description = entity.Description,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ProtocolDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
