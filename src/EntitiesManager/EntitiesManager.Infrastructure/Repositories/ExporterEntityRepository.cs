using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ExporterEntityRepository : BaseRepository<ExporterEntity>, IExporterEntityRepository
{
    public ExporterEntityRepository(IMongoDatabase database, ILogger<ExporterEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "exporters", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<ExporterEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // ExporterEntity now uses only Version as composite key
        return Builders<ExporterEntity>.Filter.Eq(x => x.Version, compositeKey);
    }

    protected override void CreateIndexes()
    {
        // Version index for uniqueness (since Version is now the composite key)
        var versionIndex = Builders<ExporterEntity>.IndexKeys.Ascending(x => x.Version);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<ExporterEntity>(versionIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ExporterEntity>(
            Builders<ExporterEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<ExporterEntity>(
            Builders<ExporterEntity>.IndexKeys.Ascending(x => x.ProtocolId)));
    }

    // GetByAddressAsync method removed since ExporterEntity no longer has Address property

    public async Task<IEnumerable<ExporterEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<ExporterEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ExporterEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ExporterEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(ExporterEntity entity)
    {
        var createdEvent = new ExporterCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ProtocolId = entity.ProtocolId,
            InputSchema = entity.InputSchema,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ExporterEntity entity)
    {
        var updatedEvent = new ExporterUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ProtocolId = entity.ProtocolId,
            InputSchema = entity.InputSchema,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ExporterDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
