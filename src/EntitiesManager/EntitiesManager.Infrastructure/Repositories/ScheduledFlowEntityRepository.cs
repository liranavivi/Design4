using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ScheduledFlowEntityRepository : BaseRepository<ScheduledFlowEntity>, IScheduledFlowEntityRepository
{
    public ScheduledFlowEntityRepository(IMongoDatabase database, ILogger<ScheduledFlowEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "scheduledflows", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<ScheduledFlowEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // ScheduledFlowEntity no longer uses composite keys - return empty filter
        return Builders<ScheduledFlowEntity>.Filter.Empty;
    }

    protected override void CreateIndexes()
    {
        // ScheduledFlowEntity no longer uses composite keys - create indexes for new properties
        // Name index for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ScheduledFlowEntity>(
            Builders<ScheduledFlowEntity>.IndexKeys.Ascending(x => x.Name)));

        // Version index for version-based queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ScheduledFlowEntity>(
            Builders<ScheduledFlowEntity>.IndexKeys.Ascending(x => x.Version)));

        // SourceId index for scheduled flow source references
        _collection.Indexes.CreateOne(new CreateIndexModel<ScheduledFlowEntity>(
            Builders<ScheduledFlowEntity>.IndexKeys.Ascending(x => x.SourceId)));

        // DestinationIds index for scheduled flow destination references
        _collection.Indexes.CreateOne(new CreateIndexModel<ScheduledFlowEntity>(
            Builders<ScheduledFlowEntity>.IndexKeys.Ascending(x => x.DestinationIds)));

        // FlowId index for flow references
        _collection.Indexes.CreateOne(new CreateIndexModel<ScheduledFlowEntity>(
            Builders<ScheduledFlowEntity>.IndexKeys.Ascending(x => x.FlowId)));
    }

    // GetByAddressAsync method removed since ScheduledFlowEntity no longer has Address property

    public async Task<IEnumerable<ScheduledFlowEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<ScheduledFlowEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ScheduledFlowEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ScheduledFlowEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ScheduledFlowEntity>> GetBySourceIdAsync(Guid sourceId)
    {
        var filter = Builders<ScheduledFlowEntity>.Filter.Eq(x => x.SourceId, sourceId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ScheduledFlowEntity>> GetByDestinationIdAsync(Guid destinationId)
    {
        var filter = Builders<ScheduledFlowEntity>.Filter.AnyEq(x => x.DestinationIds, destinationId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ScheduledFlowEntity>> GetByFlowIdAsync(Guid flowId)
    {
        var filter = Builders<ScheduledFlowEntity>.Filter.Eq(x => x.FlowId, flowId);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(ScheduledFlowEntity entity)
    {
        var createdEvent = new ScheduledFlowCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            SourceId = entity.SourceId,
            DestinationIds = entity.DestinationIds,
            FlowId = entity.FlowId,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ScheduledFlowEntity entity)
    {
        var updatedEvent = new ScheduledFlowUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            SourceId = entity.SourceId,
            DestinationIds = entity.DestinationIds,
            FlowId = entity.FlowId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ScheduledFlowDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
