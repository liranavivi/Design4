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
        // TaskScheduledEntity now uses Version-only composite key
        return Builders<TaskScheduledEntity>.Filter.Eq(x => x.Version, compositeKey);
    }

    protected override void CreateIndexes()
    {
        try
        {
            // Version-only composite key index for uniqueness
            var versionIndex = Builders<TaskScheduledEntity>.IndexKeys.Ascending(x => x.Version);
            var indexOptions = new CreateIndexOptions { Unique = true, Name = "version_unique" };
            _collection.Indexes.CreateOne(new CreateIndexModel<TaskScheduledEntity>(versionIndex, indexOptions));

            // Additional indexes for common queries
            _collection.Indexes.CreateOne(new CreateIndexModel<TaskScheduledEntity>(
                Builders<TaskScheduledEntity>.IndexKeys.Ascending(x => x.Name)));
            _collection.Indexes.CreateOne(new CreateIndexModel<TaskScheduledEntity>(
                Builders<TaskScheduledEntity>.IndexKeys.Ascending(x => x.ScheduledFlowId)));
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("existing index"))
        {
            // Index already exists, ignore the error
            _logger.LogInformation("Index already exists for TaskScheduledEntity, skipping creation");
        }
    }

    public async Task<TaskScheduledEntity?> GetByVersionAsync(string version)
    {
        var filter = Builders<TaskScheduledEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TaskScheduledEntity>> GetByScheduledFlowIdAsync(Guid scheduledFlowId)
    {
        var filter = Builders<TaskScheduledEntity>.Filter.Eq(x => x.ScheduledFlowId, scheduledFlowId);
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
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ScheduledFlowId = entity.ScheduledFlowId,
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
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ScheduledFlowId = entity.ScheduledFlowId,
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
