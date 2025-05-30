using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class TaskScheduledEntityRepository : BaseRepository<TaskScheduledEntity>, ITaskScheduledEntityRepository
{
    public TaskScheduledEntityRepository(IMongoDatabase database, ILogger<TaskScheduledEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "taskscheduleds", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<TaskScheduledEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for TaskScheduledEntity. Expected format: 'address_version'");

        return Builders<TaskScheduledEntity>.Filter.And(
            Builders<TaskScheduledEntity>.Filter.Eq(x => x.Address, parts[0]),
            Builders<TaskScheduledEntity>.Filter.Eq(x => x.Version, parts[1])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness
        var compositeKeyIndex = Builders<TaskScheduledEntity>.IndexKeys
            .Ascending(x => x.Address)
            .Ascending(x => x.Version);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<TaskScheduledEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<TaskScheduledEntity>(
            Builders<TaskScheduledEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<TaskScheduledEntity>(
            Builders<TaskScheduledEntity>.IndexKeys.Ascending(x => x.Address)));
        _collection.Indexes.CreateOne(new CreateIndexModel<TaskScheduledEntity>(
            Builders<TaskScheduledEntity>.IndexKeys.Ascending(x => x.Version)));
    }

    public async Task<IEnumerable<TaskScheduledEntity>> GetByAddressAsync(string address)
    {
        var filter = Builders<TaskScheduledEntity>.Filter.Eq(x => x.Address, address);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<TaskScheduledEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<TaskScheduledEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<TaskScheduledEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<TaskScheduledEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(TaskScheduledEntity entity)
    {
        var createdEvent = new TaskScheduledCreatedEvent
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

    protected override async Task PublishUpdatedEventAsync(TaskScheduledEntity entity)
    {
        var updatedEvent = new TaskScheduledUpdatedEvent
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
        var deletedEvent = new TaskScheduledDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
