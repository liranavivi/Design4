using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class SourceEntityRepository : BaseRepository<SourceEntity>, ISourceEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public SourceEntityRepository(
        IMongoDatabase database,
        ILogger<SourceEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "sources", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting SourceEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateSourceEntityDeletionAsync(id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of SourceEntity {Id}: {Error}",
                    id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.SourceEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for SourceEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SourceEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<SourceEntity> UpdateAsync(SourceEntity entity)
    {
        _logger.LogInformation("Validating referential integrity before updating SourceEntity {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateSourceEntityUpdateAsync(entity.Id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of SourceEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.SourceEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for SourceEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SourceEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override FilterDefinition<SourceEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 3);
        if (parts.Length != 3)
            throw new ArgumentException("Invalid composite key format for SourceEntity. Expected format: 'address_version_name'");

        return Builders<SourceEntity>.Filter.And(
            Builders<SourceEntity>.Filter.Eq(x => x.Address, parts[0]),
            Builders<SourceEntity>.Filter.Eq(x => x.Version, parts[1]),
            Builders<SourceEntity>.Filter.Eq(x => x.Name, parts[2])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness (Address + Version + Name)
        var compositeKeyIndex = Builders<SourceEntity>.IndexKeys
            .Ascending(x => x.Address)
            .Ascending(x => x.Version)
            .Ascending(x => x.Name);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(
            Builders<SourceEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(
            Builders<SourceEntity>.IndexKeys.Ascending(x => x.Address)));
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(
            Builders<SourceEntity>.IndexKeys.Ascending(x => x.Version)));
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(
            Builders<SourceEntity>.IndexKeys.Ascending(x => x.ProtocolId)));
    }

    public async Task<IEnumerable<SourceEntity>> GetByAddressAsync(string address)
    {
        var filter = Builders<SourceEntity>.Filter.Eq(x => x.Address, address);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<SourceEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<SourceEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<SourceEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<SourceEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(SourceEntity entity)
    {
        var createdEvent = new SourceCreatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            Configuration = entity.Configuration,
            ProtocolId = entity.ProtocolId,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(SourceEntity entity)
    {
        var updatedEvent = new SourceUpdatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            Configuration = entity.Configuration,
            ProtocolId = entity.ProtocolId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new SourceDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
