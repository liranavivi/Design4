using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class StepEntityRepository : BaseRepository<StepEntity>, IStepEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public StepEntityRepository(
        IMongoDatabase database,
        ILogger<StepEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "steps", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<StepEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // StepEntity no longer uses composite keys - return empty filter
        return Builders<StepEntity>.Filter.Empty;
    }

    protected override void CreateIndexes()
    {
        // StepEntity no longer uses composite keys - create indexes for new properties
        // EntityId index for workflow entity references
        _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
            Builders<StepEntity>.IndexKeys.Ascending(x => x.EntityId)));

        // NextStepIds index for workflow navigation queries
        _collection.Indexes.CreateOne(new CreateIndexModel<StepEntity>(
            Builders<StepEntity>.IndexKeys.Ascending(x => x.NextStepIds)));
    }

    // GetByAddressAsync, GetByVersionAsync, and GetByNameAsync methods removed
    // since StepEntity no longer has these properties

    public async Task<IEnumerable<StepEntity>> GetByEntityIdAsync(Guid entityId)
    {
        var filter = Builders<StepEntity>.Filter.Eq(x => x.EntityId, entityId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<StepEntity>> GetByNextStepIdAsync(Guid nextStepId)
    {
        var filter = Builders<StepEntity>.Filter.AnyEq(x => x.NextStepIds, nextStepId);
        return await _collection.Find(filter).ToListAsync();
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting StepEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateStepEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of StepEntity {Id}: {Error}. References: {FlowCount} flows",
                    id, validationResult.ErrorMessage, validationResult.StepEntityReferences?.FlowEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.StepEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for StepEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during StepEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<StepEntity> UpdateAsync(StepEntity entity)
    {
        _logger.LogInformation("Validating referential integrity before updating StepEntity {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateStepEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of StepEntity {Id}: {Error}. References: {FlowCount} flows",
                    entity.Id, validationResult.ErrorMessage, validationResult.StepEntityReferences?.FlowEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.StepEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for StepEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during StepEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override async Task PublishCreatedEventAsync(StepEntity entity)
    {
        var createdEvent = new StepCreatedEvent
        {
            Id = entity.Id,
            EntityId = entity.EntityId,
            NextStepIds = entity.NextStepIds,
            Description = entity.Description,
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
            EntityId = entity.EntityId,
            NextStepIds = entity.NextStepIds,
            Description = entity.Description,
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
