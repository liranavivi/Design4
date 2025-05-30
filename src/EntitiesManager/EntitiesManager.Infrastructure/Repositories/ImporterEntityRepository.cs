using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ImporterEntityRepository : BaseRepository<ImporterEntity>, IImporterEntityRepository
{
    public ImporterEntityRepository(IMongoDatabase database, ILogger<ImporterEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "importers", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<ImporterEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // ImporterEntity now uses only Version as composite key
        return Builders<ImporterEntity>.Filter.Eq(x => x.Version, compositeKey);
    }

    protected override void CreateIndexes()
    {
        // Version index for uniqueness (since Version is now the composite key)
        var versionIndex = Builders<ImporterEntity>.IndexKeys.Ascending(x => x.Version);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<ImporterEntity>(versionIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ImporterEntity>(
            Builders<ImporterEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<ImporterEntity>(
            Builders<ImporterEntity>.IndexKeys.Ascending(x => x.ProtocolId)));
    }

    // GetByAddressAsync method removed since ImporterEntity no longer has Address property

    public async Task<IEnumerable<ImporterEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<ImporterEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ImporterEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ImporterEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(ImporterEntity entity)
    {
        var createdEvent = new ImporterCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ProtocolId = entity.ProtocolId,
            OutputSchema = entity.OutputSchema,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ImporterEntity entity)
    {
        var updatedEvent = new ImporterUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ProtocolId = entity.ProtocolId,
            OutputSchema = entity.OutputSchema,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ImporterDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
