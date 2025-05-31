using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class FlowEntityRepository : BaseRepository<FlowEntity>, IFlowEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public FlowEntityRepository(
        IMongoDatabase database,
        ILogger<FlowEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "flows", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<FlowEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // FlowEntity no longer uses composite keys - return empty filter
        return Builders<FlowEntity>.Filter.Empty;
    }

    protected override void CreateIndexes()
    {
        // FlowEntity no longer uses composite keys - create indexes for new properties
        // Name index for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<FlowEntity>(
            Builders<FlowEntity>.IndexKeys.Ascending(x => x.Name)));

        // Version index for version-based queries
        _collection.Indexes.CreateOne(new CreateIndexModel<FlowEntity>(
            Builders<FlowEntity>.IndexKeys.Ascending(x => x.Version)));

        // StepIds index for workflow step references
        _collection.Indexes.CreateOne(new CreateIndexModel<FlowEntity>(
            Builders<FlowEntity>.IndexKeys.Ascending(x => x.StepIds)));
    }

    // GetByAddressAsync method removed since FlowEntity no longer has Address property

    public async Task<IEnumerable<FlowEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<FlowEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<FlowEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<FlowEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<FlowEntity>> GetByStepIdAsync(Guid stepId)
    {
        var filter = Builders<FlowEntity>.Filter.AnyEq(x => x.StepIds, stepId);
        return await _collection.Find(filter).ToListAsync();
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting FlowEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateFlowEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of FlowEntity {Id}: {Error}. References: {OrchestratedFlowCount} orchestrated flows",
                    id, validationResult.ErrorMessage, validationResult.FlowEntityReferences?.OrchestratedFlowEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.FlowEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for FlowEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during FlowEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<FlowEntity> UpdateAsync(FlowEntity entity)
    {
        _logger.LogInformation("Validating referential integrity before updating FlowEntity {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateFlowEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of FlowEntity {Id}: {Error}. References: {OrchestratedFlowCount} orchestrated flows",
                    entity.Id, validationResult.ErrorMessage, validationResult.FlowEntityReferences?.OrchestratedFlowEntityCount ?? 0);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.FlowEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for FlowEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during FlowEntity update validation for {Id}", entity.Id);
            throw;
        }
    }

    protected override async Task PublishCreatedEventAsync(FlowEntity entity)
    {
        var createdEvent = new FlowCreatedEvent
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

    protected override async Task PublishUpdatedEventAsync(FlowEntity entity)
    {
        var updatedEvent = new FlowUpdatedEvent
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
        var deletedEvent = new FlowDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
