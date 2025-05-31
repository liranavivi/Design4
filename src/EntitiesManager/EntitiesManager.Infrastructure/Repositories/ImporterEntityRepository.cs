using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ImporterEntityRepository : BaseRepository<ImporterEntity>, IImporterEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public ImporterEntityRepository(
        IMongoDatabase database,
        ILogger<ImporterEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "importers", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<ImporterEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // ImporterEntity now uses only Version as composite key
        return Builders<ImporterEntity>.Filter.Eq(x => x.Version, compositeKey);
    }

    protected override void CreateIndexes()
    {
        // Version index for uniqueness (since Version is now the composite key)
        var versionIndex = Builders<ImporterEntity>.IndexKeys.Ascending(x => x.Version);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<ImporterEntity>(versionIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<ImporterEntity>(
            Builders<ImporterEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<ImporterEntity>(
            Builders<ImporterEntity>.IndexKeys.Ascending(x => x.ProtocolId)));
    }

    // GetByAddressAsync method removed since ImporterEntity no longer has Address property

    public async Task<IEnumerable<ImporterEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<ImporterEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ImporterEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<ImporterEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting ImporterEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateImporterEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of ImporterEntity {Id}: {Error}. References: {StepCount} steps",
                    id, validationResult.ErrorMessage, validationResult.ImporterEntityReferences?.StepEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.ImporterEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for ImporterEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ImporterEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<ImporterEntity> UpdateAsync(ImporterEntity entity)
    {
        _logger.LogInformation("Validating referential integrity before updating ImporterEntity {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateImporterEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of ImporterEntity {Id}: {Error}. References: {StepCount} steps",
                    entity.Id, validationResult.ErrorMessage, validationResult.ImporterEntityReferences?.StepEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.ImporterEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for ImporterEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ImporterEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override async Task PublishCreatedEventAsync(ImporterEntity entity)
    {
        var createdEvent = new ImporterCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ProtocolId = entity.ProtocolId,
            OutputSchema = entity.OutputSchema,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(ImporterEntity entity)
    {
        var updatedEvent = new ImporterUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            ProtocolId = entity.ProtocolId,
            OutputSchema = entity.OutputSchema,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ImporterDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
