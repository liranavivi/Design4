using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class StepEntityRepository : BaseRepository<StepEntity>, IStepEntityRepository
{
    public StepEntityRepository(IMongoDatabase database, ILogger<StepEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "steps", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<StepEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for StepEntity. Expected format: 'address_version'");

        return Builders<StepEntity>.Filter.And(
            Builders<StepEntity>.Filter.Eq(x => x.Address, parts[0]),
            Builders<StepEntity>.Filter.Eq(x => x.Version, parts[1])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness
        var compositeKeyIndex = Builders<StepEntity>.IndexKeys
            .Ascending(x => x.Address)
            .Ascending(x => x.Version);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
            Builders<StepEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
            Builders<StepEntity>.IndexKeys.Ascending(x => x.Address)));
        _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
            Builders<StepEntity>.IndexKeys.Ascending(x => x.Version)));
    }

    public async Task<IEnumerable<StepEntity>> GetByAddressAsync(string address)
    {
        var filter = Builders<StepEntity>.Filter.Eq(x => x.Address, address);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<StepEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<StepEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<StepEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<StepEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(StepEntity entity)
    {
        var createdEvent = new StepCreatedEvent
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

    protected override async Task PublishUpdatedEventAsync(StepEntity entity)
    {
        var updatedEvent = new StepUpdatedEvent
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
        var deletedEvent = new StepDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
