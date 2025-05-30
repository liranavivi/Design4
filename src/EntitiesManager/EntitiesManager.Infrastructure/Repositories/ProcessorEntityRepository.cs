using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ProcessorEntityRepository : BaseRepository<ProcessorEntity>, IProcessorEntityRepository
{
    public ProcessorEntityRepository(IMongoDatabase database, ILogger<ProcessorEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "processors", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<ProcessorEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for ProcessorEntity. Expected format: 'address_version'");

        return Builders<ProcessorEntity>.Filter.And(
            Builders<ProcessorEntity>.Filter.Eq(x => x.Address, parts[0]),
            Builders<ProcessorEntity>.Filter.Eq(x => x.Version, parts[1])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness
        var compositeKeyIndex = Builders<ProcessorEntity>.IndexKeys
            .Ascending(x => x.Address)
            .Ascending(x => x.Version);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessorEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessorEntity>(
            Builders<ProcessorEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessorEntity>(
            Builders<ProcessorEntity>.IndexKeys.Ascending(x => x.Address)));
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessorEntity>(
            Builders<ProcessorEntity>.IndexKeys.Ascending(x => x.Version)));
    }

    public async Task<IEnumerable<ProcessorEntity>> GetByAddressAsync(string address)
    {
        var filter = Builders<ProcessorEntity>.Filter.Eq(x => x.Address, address);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ProcessorEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<ProcessorEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ProcessorEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ProcessorEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(ProcessorEntity entity)
    {
        var createdEvent = new ProcessorCreatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            Configuration = entity.Configuration,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ProcessorEntity entity)
    {
        var updatedEvent = new ProcessorUpdatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            Configuration = entity.Configuration,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ProcessorDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
