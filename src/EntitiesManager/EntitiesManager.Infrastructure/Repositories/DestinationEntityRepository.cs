using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class DestinationEntityRepository : BaseRepository<DestinationEntity>, IDestinationEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public DestinationEntityRepository(
        IMongoDatabase database,
        ILogger<DestinationEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "destinations", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting DestinationEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateDestinationEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of DestinationEntity {Id}: {Error}",
                    id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.DestinationEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for DestinationEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DestinationEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<DestinationEntity> UpdateAsync(DestinationEntity entity)
    {
        _logger.LogInformation("Validating referential integrity before updating DestinationEntity {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateDestinationEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of DestinationEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.DestinationEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for DestinationEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DestinationEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override FilterDefinition<DestinationEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 3);
        if (parts.Length != 3)
            throw new ArgumentException("Invalid composite key format for DestinationEntity. Expected format: 'address_version_name'");

        return Builders<DestinationEntity>.Filter.And(
            Builders<DestinationEntity>.Filter.Eq(x => x.Address, parts[0]),
            Builders<DestinationEntity>.Filter.Eq(x => x.Version, parts[1]),
            Builders<DestinationEntity>.Filter.Eq(x => x.Name, parts[2])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness (Address + Version + Name)
        var compositeKeyIndex = Builders<DestinationEntity>.IndexKeys
            .Ascending(x => x.Address)
            .Ascending(x => x.Version)
            .Ascending(x => x.Name);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<DestinationEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<DestinationEntity>(
            Builders<DestinationEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<DestinationEntity>(
            Builders<DestinationEntity>.IndexKeys.Ascending(x => x.Address)));
        _collection.Indexes.CreateOne(new CreateIndexModel<DestinationEntity>(
            Builders<DestinationEntity>.IndexKeys.Ascending(x => x.Version)));
        _collection.Indexes.CreateOne(new CreateIndexModel<DestinationEntity>(
            Builders<DestinationEntity>.IndexKeys.Ascending(x => x.ProtocolId)));
    }

    public async Task<IEnumerable<DestinationEntity>> GetByAddressAsync(string address)
    {
        var filter = Builders<DestinationEntity>.Filter.Eq(x => x.Address, address);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<DestinationEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<DestinationEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<DestinationEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<DestinationEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(DestinationEntity entity)
    {
        var createdEvent = new DestinationCreatedEvent
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

    protected override async Task PublishUpdatedEventAsync(DestinationEntity entity)
    {
        var updatedEvent = new DestinationUpdatedEvent
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
        var deletedEvent = new DestinationDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
