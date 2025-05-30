using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ProcessingChainEntityRepository : BaseRepository<ProcessingChainEntity>, IProcessingChainEntityRepository
{
    public ProcessingChainEntityRepository(IMongoDatabase database, ILogger<ProcessingChainEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "processingchains", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<ProcessingChainEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // ProcessingChainEntity no longer uses composite keys - return empty filter
        return Builders<ProcessingChainEntity>.Filter.Empty;
    }

    protected override void CreateIndexes()
    {
        // ProcessingChainEntity no longer uses composite keys - create indexes for new properties
        // Name index for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessingChainEntity>(
            Builders<ProcessingChainEntity>.IndexKeys.Ascending(x => x.Name)));

        // Version index for version-based queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessingChainEntity>(
            Builders<ProcessingChainEntity>.IndexKeys.Ascending(x => x.Version)));

        // StepIds index for workflow step references
        _collection.Indexes.CreateOne(new CreateIndexModel<ProcessingChainEntity>(
            Builders<ProcessingChainEntity>.IndexKeys.Ascending(x => x.StepIds)));
    }

    // GetByAddressAsync method removed since ProcessingChainEntity no longer has Address property

    public async Task<IEnumerable<ProcessingChainEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<ProcessingChainEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ProcessingChainEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ProcessingChainEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ProcessingChainEntity>> GetByStepIdAsync(Guid stepId)
    {
        var filter = Builders<ProcessingChainEntity>.Filter.AnyEq(x => x.StepIds, stepId);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(ProcessingChainEntity entity)
    {
        var createdEvent = new ProcessingChainCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            StepIds = entity.StepIds,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ProcessingChainEntity entity)
    {
        var updatedEvent = new ProcessingChainUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            StepIds = entity.StepIds,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ProcessingChainDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
