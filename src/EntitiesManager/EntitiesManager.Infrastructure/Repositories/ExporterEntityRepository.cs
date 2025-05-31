using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ExporterEntityRepository : BaseRepository<ExporterEntity>, IExporterEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public ExporterEntityRepository(
        IMongoDatabase database,
        ILogger<ExporterEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "exporters", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<ExporterEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // ExporterEntity now uses only Version as composite key
        return Builders<ExporterEntity>.Filter.Eq(x => x.Version, compositeKey);
    }

    protected override void CreateIndexes()
    {
        // Version index for uniqueness (since Version is now the composite key)
        var versionIndex = Builders<ExporterEntity>.IndexKeys.Ascending(x => x.Version);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<ExporterEntity>(versionIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ExporterEntity>(
            Builders<ExporterEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<ExporterEntity>(
            Builders<ExporterEntity>.IndexKeys.Ascending(x => x.ProtocolId)));
    }

    // GetByAddressAsync method removed since ExporterEntity no longer has Address property

    public async Task<IEnumerable<ExporterEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<ExporterEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ExporterEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ExporterEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting ExporterEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateExporterEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of ExporterEntity {Id}: {Error}. References: {StepCount} steps",
                    id, validationResult.ErrorMessage, validationResult.ExporterEntityReferences?.StepEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.ExporterEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for ExporterEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ExporterEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<ExporterEntity> UpdateAsync(ExporterEntity entity)
    {
        _logger.LogInformation("Validating referential integrity before updating ExporterEntity {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateExporterEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of ExporterEntity {Id}: {Error}. References: {StepCount} steps",
                    entity.Id, validationResult.ErrorMessage, validationResult.ExporterEntityReferences?.StepEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.ExporterEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for ExporterEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ExporterEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override async Task PublishCreatedEventAsync(ExporterEntity entity)
    {
        var createdEvent = new ExporterCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ProtocolId = entity.ProtocolId,
            InputSchema = entity.InputSchema,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ExporterEntity entity)
    {
        var updatedEvent = new ExporterUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ProtocolId = entity.ProtocolId,
            InputSchema = entity.InputSchema,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ExporterDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
