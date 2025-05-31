using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class OrchestratedFlowEntityRepository : BaseRepository<OrchestratedFlowEntity>, IOrchestratedFlowEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public OrchestratedFlowEntityRepository(
        IMongoDatabase database,
        ILogger<OrchestratedFlowEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "orchestratedflows", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    protected override FilterDefinition<OrchestratedFlowEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        // OrchestratedFlowEntity no longer uses composite keys - return empty filter
        return Builders<OrchestratedFlowEntity>.Filter.Empty;
    }

    protected override void CreateIndexes()
    {
        try
        {
            // Create indexes for Assignment-focused architecture
            // Name index for common queries
            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Name = "orchestratedflow_name_idx" }));

            // AssignmentIds index for assignment-based queries
            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.AssignmentIds),
                new CreateIndexOptions { Name = "orchestratedflow_assignmentids_idx" }));

            // FlowId index for flow-based queries
            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.FlowId),
                new CreateIndexOptions { Name = "orchestratedflow_flowid_idx" }));
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("existing index") || ex.Message.Contains("different name"))
        {
            _logger.LogInformation("Index already exists for OrchestratedFlowEntity (possibly with different name), skipping creation: {Error}", ex.Message);
        }

        try
        {
            // Version index for version-based queries
            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.Version),
                new CreateIndexOptions { Name = "orchestratedflow_version_idx" }));
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("existing index") || ex.Message.Contains("different name"))
        {
            _logger.LogInformation("Version index already exists for OrchestratedFlowEntity, skipping creation: {Error}", ex.Message);
        }

        // Note: SourceId and DestinationIds indexes removed - Assignment-focused architecture
        // Only FlowId and AssignmentIds indexes are needed now

        try
        {
            // FlowId index for flow references
            _collection.Indexes.CreateOne(new CreateIndexModel<OrchestratedFlowEntity>(
                Builders<OrchestratedFlowEntity>.IndexKeys.Ascending(x => x.FlowId),
                new CreateIndexOptions { Name = "orchestratedflow_flowid_idx" }));
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("existing index") || ex.Message.Contains("different name"))
        {
            _logger.LogInformation("FlowId index already exists for OrchestratedFlowEntity, skipping creation: {Error}", ex.Message);
        }
    }

    // GetByAddressAsync method removed since OrchestratedFlowEntity no longer has Address property

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Starting referential integrity validation for OrchestratedFlowEntity deletion. Id: {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateOrchestratedFlowEntityDeletionAsync(id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of OrchestratedFlowEntity {Id}: {Error}",
                    id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.OrchestratedFlowEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for OrchestratedFlowEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for OrchestratedFlowEntity {Id}", id);
            throw;
        }
    }

    public override async Task<OrchestratedFlowEntity> UpdateAsync(OrchestratedFlowEntity entity)
    {
        _logger.LogInformation("Starting referential integrity validation for OrchestratedFlowEntity update. Id: {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateOrchestratedFlowEntityUpdateAsync(entity.Id);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of OrchestratedFlowEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.OrchestratedFlowEntityReferences!);
            }

            _logger.LogInformation("Referential integrity validation passed for OrchestratedFlowEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for OrchestratedFlowEntity {Id}", entity.Id);
            throw;
        }
    }

    public async Task<IEnumerable<OrchestratedFlowEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<OrchestratedFlowEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<OrchestratedFlowEntity>> GetByAssignmentIdAsync(Guid assignmentId)
    {
        var filter = Builders<OrchestratedFlowEntity>.Filter.AnyEq(x => x.AssignmentIds, assignmentId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<OrchestratedFlowEntity>> GetByFlowIdAsync(Guid flowId)
    {
        var filter = Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.FlowId, flowId);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(OrchestratedFlowEntity entity)
    {
        var createdEvent = new OrchestratedFlowCreatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            AssignmentIds = entity.AssignmentIds,
            FlowId = entity.FlowId,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(OrchestratedFlowEntity entity)
    {
        var updatedEvent = new OrchestratedFlowUpdatedEvent
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Description = entity.Description,
            AssignmentIds = entity.AssignmentIds,
            FlowId = entity.FlowId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new OrchestratedFlowDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
