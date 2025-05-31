using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class AssignmentEntityRepository : BaseRepository<AssignmentEntity>, IAssignmentEntityRepository
{
    public AssignmentEntityRepository(IMongoDatabase database, ILogger<AssignmentEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "assignments", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<AssignmentEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // StepId-only composite key
        if (!Guid.TryParse(compositeKey, out var stepId))
            throw new ArgumentException("Invalid composite key format for AssignmentEntity. Expected format: 'stepId' (GUID)");

        return Builders<AssignmentEntity>.Filter.Eq(x => x.StepId, stepId);
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness (StepId-only)
        var compositeKeyIndex = Builders<AssignmentEntity>.IndexKeys
            .Ascending(x => x.StepId);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<AssignmentEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<AssignmentEntity>(
            Builders<AssignmentEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<AssignmentEntity>(
            Builders<AssignmentEntity>.IndexKeys.Ascending(x => x.Version)));
        _collection.Indexes.CreateOne(new CreateIndexModel<AssignmentEntity>(
            Builders<AssignmentEntity>.IndexKeys.Ascending(x => x.EntityIds)));
    }

    public async Task<IEnumerable<AssignmentEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<AssignmentEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<AssignmentEntity?> GetByStepIdAsync(Guid stepId)
    {
        var filter = Builders<AssignmentEntity>.Filter.Eq(x => x.StepId, stepId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AssignmentEntity>> GetByEntityIdAsync(Guid entityId)
    {
        var filter = Builders<AssignmentEntity>.Filter.AnyEq(x => x.EntityIds, entityId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<AssignmentEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<AssignmentEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(AssignmentEntity entity)
    {
        var createdEvent = new AssignmentCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            StepId = entity.StepId,
            EntityIds = entity.EntityIds,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(AssignmentEntity entity)
    {
        var updatedEvent = new AssignmentUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            StepId = entity.StepId,
            EntityIds = entity.EntityIds,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new AssignmentDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
