using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Services;

namespace EntitiesManager.Infrastructure.Services;

public class ReferentialIntegrityService : IReferentialIntegrityService
{
    private readonly IMongoDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReferentialIntegrityService> _logger;

    public ReferentialIntegrityService(
        IMongoDatabase database,
        IConfiguration configuration,
        ILogger<ReferentialIntegrityService> logger)
    {
        _database = database;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ReferentialIntegrityResult> ValidateProtocolDeletionAsync(Guid protocolId)
    {
        if (!IsValidationEnabled())
        {
            _logger.LogDebug("Referential integrity validation is disabled");
            return ReferentialIntegrityResult.Valid();
        }

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for ProtocolEntity {ProtocolId}", protocolId);

        try
        {
            var references = await GetProtocolReferencesAsync(protocolId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({SourceCount} sources, {DestinationCount} destinations)", 
                duration.TotalMilliseconds, references.TotalReferences, references.SourceEntityCount, references.DestinationEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete ProtocolEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for ProtocolEntity {ProtocolId}", protocolId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateProtocolUpdateAsync(Guid currentId, Guid newId)
    {
        if (currentId == newId)
        {
            return ReferentialIntegrityResult.Valid(); // No ID change
        }

        _logger.LogInformation("Validating ProtocolEntity ID change from {CurrentId} to {NewId}", currentId, newId);
        return await ValidateProtocolDeletionAsync(currentId);
    }

    public async Task<ProtocolReferenceInfo> GetProtocolReferencesAsync(Guid protocolId)
    {
        var enableParallel = bool.Parse(_configuration["ReferentialIntegrity:EnableParallelValidation"] ?? "true");
        var validateSources = bool.Parse(_configuration["ReferentialIntegrity:ValidateSourceReferences"] ?? "true");
        var validateDestinations = bool.Parse(_configuration["ReferentialIntegrity:ValidateDestinationReferences"] ?? "true");

        var references = new ProtocolReferenceInfo();

        if (enableParallel)
        {
            var tasks = new List<Task>();
            
            if (validateSources)
            {
                tasks.Add(Task.Run(async () => 
                    references.SourceEntityCount = await CountSourceReferencesAsync(protocolId)));
            }
            
            if (validateDestinations)
            {
                tasks.Add(Task.Run(async () => 
                    references.DestinationEntityCount = await CountDestinationReferencesAsync(protocolId)));
            }

            await Task.WhenAll(tasks);
        }
        else
        {
            if (validateSources)
                references.SourceEntityCount = await CountSourceReferencesAsync(protocolId);
            
            if (validateDestinations)
                references.DestinationEntityCount = await CountDestinationReferencesAsync(protocolId);
        }

        return references;
    }

    private async Task<long> CountSourceReferencesAsync(Guid protocolId)
    {
        try
        {
            var collection = _database.GetCollection<SourceEntity>("sources");
            var filter = Builders<SourceEntity>.Filter.Eq(x => x.ProtocolId, protocolId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} SourceEntity references for ProtocolId {ProtocolId}", count, protocolId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting SourceEntity references for ProtocolId {ProtocolId}", protocolId);
            throw;
        }
    }

    private async Task<long> CountDestinationReferencesAsync(Guid protocolId)
    {
        try
        {
            var collection = _database.GetCollection<DestinationEntity>("destinations");
            var filter = Builders<DestinationEntity>.Filter.Eq(x => x.ProtocolId, protocolId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} DestinationEntity references for ProtocolId {ProtocolId}", count, protocolId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting DestinationEntity references for ProtocolId {ProtocolId}", protocolId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateSourceEntityDeletionAsync(Guid sourceId)
    {
        if (!IsValidationEnabled())
        {
            _logger.LogDebug("Referential integrity validation is disabled");
            return ReferentialIntegrityResult.Valid();
        }

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for SourceEntity {SourceId}", sourceId);

        try
        {
            var references = await GetSourceEntityReferencesAsync(sourceId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references",
                duration.TotalMilliseconds, references.TotalReferences);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete SourceEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for SourceEntity {SourceId}", sourceId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateSourceEntityUpdateAsync(Guid sourceId)
    {
        _logger.LogInformation("Validating SourceEntity update for {SourceId}", sourceId);
        return await ValidateSourceEntityDeletionAsync(sourceId); // Same validation logic
    }

    public async Task<SourceEntityReferenceInfo> GetSourceEntityReferencesAsync(Guid sourceId)
    {
        var validateScheduledFlows = bool.Parse(_configuration["ReferentialIntegrity:ValidateScheduledFlowReferences"] ?? "true");

        var references = new SourceEntityReferenceInfo();

        if (validateScheduledFlows)
        {
            // SourceEntity no longer referenced by OrchestratedFlowEntity (Assignment-focused architecture)
            // No need to count references
        }

        return references;
    }

    private async Task<long> CountScheduledFlowReferencesAsync(Guid sourceId)
    {
        try
        {
            // OrchestratedFlowEntity no longer has SourceId property - Assignment-focused architecture
            // Return 0 as there are no source references in the new architecture
            _logger.LogDebug("OrchestratedFlowEntity no longer references SourceId - Assignment-focused architecture. Returning 0 references for SourceId {SourceId}", sourceId);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting OrchestratedFlowEntity references for SourceId {SourceId}", sourceId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateDestinationEntityDeletionAsync(Guid destinationId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for DestinationEntity {DestinationId}", destinationId);

        try
        {
            var references = await GetDestinationEntityReferencesAsync(destinationId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references",
                duration.TotalMilliseconds, references.TotalReferences);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete DestinationEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for DestinationEntity {DestinationId}", destinationId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateDestinationEntityUpdateAsync(Guid destinationId)
    {
        _logger.LogInformation("Validating DestinationEntity update for {DestinationId}", destinationId);
        return await ValidateDestinationEntityDeletionAsync(destinationId); // Same validation logic
    }

    public async Task<DestinationEntityReferenceInfo> GetDestinationEntityReferencesAsync(Guid destinationId)
    {
        var validateScheduledFlows = bool.Parse(_configuration["ReferentialIntegrity:ValidateScheduledFlowReferences"] ?? "true");

        var references = new DestinationEntityReferenceInfo();

        if (validateScheduledFlows)
        {
            // DestinationEntity no longer referenced by OrchestratedFlowEntity (Assignment-focused architecture)
            // No need to count references
        }

        return references;
    }

    private async Task<long> CountScheduledFlowDestinationReferencesAsync(Guid destinationId)
    {
        try
        {
            // OrchestratedFlowEntity no longer has DestinationIds property - Assignment-focused architecture
            // Return 0 as there are no destination references in the new architecture
            _logger.LogDebug("OrchestratedFlowEntity no longer references DestinationIds - Assignment-focused architecture. Returning 0 references for DestinationId {DestinationId}", destinationId);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting OrchestratedFlowEntity references for DestinationId {DestinationId}", destinationId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateImporterEntityDeletionAsync(Guid importerId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for ImporterEntity {ImporterId}", importerId);

        try
        {
            var references = await GetImporterEntityReferencesAsync(importerId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({StepCount} steps)",
                duration.TotalMilliseconds, references.TotalReferences, references.StepEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete ImporterEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for ImporterEntity {ImporterId}", importerId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateImporterEntityUpdateAsync(Guid importerId)
    {
        _logger.LogInformation("Validating ImporterEntity update for {ImporterId}", importerId);
        return await ValidateImporterEntityDeletionAsync(importerId); // Same validation logic
    }

    public async Task<ImporterEntityReferenceInfo> GetImporterEntityReferencesAsync(Guid importerId)
    {
        var validateSteps = bool.Parse(_configuration["ReferentialIntegrity:ValidateStepReferences"] ?? "true");

        var references = new ImporterEntityReferenceInfo();

        if (validateSteps)
        {
            references.StepEntityCount = await CountStepImporterReferencesAsync(importerId);
        }

        return references;
    }

    private async Task<long> CountStepImporterReferencesAsync(Guid importerId)
    {
        try
        {
            var collection = _database.GetCollection<StepEntity>("steps");
            var filter = Builders<StepEntity>.Filter.Eq(x => x.EntityId, importerId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} StepEntity references for ImporterId {ImporterId}", count, importerId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting StepEntity references for ImporterId {ImporterId}", importerId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateExporterEntityDeletionAsync(Guid exporterId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for ExporterEntity {ExporterId}", exporterId);

        try
        {
            var references = await GetExporterEntityReferencesAsync(exporterId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({StepCount} steps)",
                duration.TotalMilliseconds, references.TotalReferences, references.StepEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete ExporterEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for ExporterEntity {ExporterId}", exporterId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateExporterEntityUpdateAsync(Guid exporterId)
    {
        _logger.LogInformation("Validating ExporterEntity update for {ExporterId}", exporterId);
        return await ValidateExporterEntityDeletionAsync(exporterId); // Same validation logic
    }

    public async Task<ExporterEntityReferenceInfo> GetExporterEntityReferencesAsync(Guid exporterId)
    {
        var validateSteps = bool.Parse(_configuration["ReferentialIntegrity:ValidateStepReferences"] ?? "true");

        var references = new ExporterEntityReferenceInfo();

        if (validateSteps)
        {
            references.StepEntityCount = await CountStepExporterReferencesAsync(exporterId);
        }

        return references;
    }

    private async Task<long> CountStepExporterReferencesAsync(Guid exporterId)
    {
        try
        {
            var collection = _database.GetCollection<StepEntity>("steps");
            var filter = Builders<StepEntity>.Filter.Eq(x => x.EntityId, exporterId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} StepEntity references for ExporterId {ExporterId}", count, exporterId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting StepEntity references for ExporterId {ExporterId}", exporterId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateProcessorEntityDeletionAsync(Guid processorId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for ProcessorEntity {ProcessorId}", processorId);

        try
        {
            var references = await GetProcessorEntityReferencesAsync(processorId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({StepCount} steps)",
                duration.TotalMilliseconds, references.TotalReferences, references.StepEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete ProcessorEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for ProcessorEntity {ProcessorId}", processorId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateProcessorEntityUpdateAsync(Guid processorId)
    {
        _logger.LogInformation("Validating ProcessorEntity update for {ProcessorId}", processorId);
        return await ValidateProcessorEntityDeletionAsync(processorId); // Same validation logic
    }

    public async Task<ProcessorEntityReferenceInfo> GetProcessorEntityReferencesAsync(Guid processorId)
    {
        var validateSteps = bool.Parse(_configuration["ReferentialIntegrity:ValidateStepReferences"] ?? "true");

        var references = new ProcessorEntityReferenceInfo();

        if (validateSteps)
        {
            references.StepEntityCount = await CountStepProcessorReferencesAsync(processorId);
        }

        return references;
    }

    private async Task<long> CountStepProcessorReferencesAsync(Guid processorId)
    {
        try
        {
            var collection = _database.GetCollection<StepEntity>("steps");
            var filter = Builders<StepEntity>.Filter.Eq(x => x.EntityId, processorId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} StepEntity references for ProcessorId {ProcessorId}", count, processorId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting StepEntity references for ProcessorId {ProcessorId}", processorId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateStepEntityDeletionAsync(Guid stepId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for StepEntity {StepId}", stepId);

        try
        {
            var references = await GetStepEntityReferencesAsync(stepId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({FlowCount} flows)",
                duration.TotalMilliseconds, references.TotalReferences, references.FlowEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete StepEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for StepEntity {StepId}", stepId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateStepEntityUpdateAsync(Guid stepId)
    {
        _logger.LogInformation("Validating StepEntity update for {StepId}", stepId);
        return await ValidateStepEntityDeletionAsync(stepId); // Same validation logic
    }

    public async Task<StepEntityReferenceInfo> GetStepEntityReferencesAsync(Guid stepId)
    {
        var validateFlows = bool.Parse(_configuration["ReferentialIntegrity:ValidateFlowReferences"] ?? "true");

        var references = new StepEntityReferenceInfo();

        if (validateFlows)
        {
            references.FlowEntityCount = await CountFlowStepReferencesAsync(stepId);
        }

        return references;
    }

    private async Task<long> CountFlowStepReferencesAsync(Guid stepId)
    {
        try
        {
            var collection = _database.GetCollection<FlowEntity>("flows");
            var filter = Builders<FlowEntity>.Filter.AnyEq(x => x.StepIds, stepId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} FlowEntity references for StepId {StepId}", count, stepId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting FlowEntity references for StepId {StepId}", stepId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateFlowEntityDeletionAsync(Guid flowId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting referential integrity validation for FlowEntity {FlowId}", flowId);

        try
        {
            var references = await GetFlowEntityReferencesAsync(flowId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({OrchestratedFlowCount} orchestrated flows)",
                duration.TotalMilliseconds, references.TotalReferences, references.OrchestratedFlowEntityCount);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete FlowEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for FlowEntity {FlowId}", flowId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateFlowEntityUpdateAsync(Guid flowId)
    {
        _logger.LogInformation("Validating FlowEntity update for {FlowId}", flowId);
        return await ValidateFlowEntityDeletionAsync(flowId); // Same validation logic
    }

    public async Task<FlowEntityReferenceInfo> GetFlowEntityReferencesAsync(Guid flowId)
    {
        var validateScheduledFlows = bool.Parse(_configuration["ReferentialIntegrity:ValidateScheduledFlowReferences"] ?? "true");

        var references = new FlowEntityReferenceInfo();

        if (validateScheduledFlows)
        {
            references.OrchestratedFlowEntityCount = await CountOrchestratedFlowFlowReferencesAsync(flowId);
        }

        return references;
    }

    private async Task<long> CountOrchestratedFlowFlowReferencesAsync(Guid flowId)
    {
        try
        {
            var collection = _database.GetCollection<OrchestratedFlowEntity>("orchestratedflows");
            var filter = Builders<OrchestratedFlowEntity>.Filter.Eq(x => x.FlowId, flowId);
            var count = await collection.CountDocumentsAsync(filter);

            _logger.LogDebug("Found {Count} OrchestratedFlowEntity references for FlowId {FlowId}", count, flowId);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting OrchestratedFlowEntity references for FlowId {FlowId}", flowId);
            throw;
        }
    }

    // OrchestratedFlowEntity validation methods
    public async Task<ReferentialIntegrityResult> ValidateOrchestratedFlowEntityDeletionAsync(Guid orchestratedFlowId)
    {
        _logger.LogInformation("Validating OrchestratedFlowEntity deletion for {OrchestratedFlowId}", orchestratedFlowId);
        var startTime = DateTime.UtcNow;

        try
        {
            var references = await GetOrchestratedFlowEntityReferencesAsync(orchestratedFlowId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references",
                duration.TotalMilliseconds, references.TotalReferences);

            if (references.HasReferences)
            {
                var referencingTypes = references.GetReferencingEntityTypes();
                var errorMessage = $"Cannot delete OrchestratedFlowEntity. Referenced by: {string.Join(", ", referencingTypes)}";
                return ReferentialIntegrityResult.Invalid(errorMessage, references);
            }

            var result = ReferentialIntegrityResult.Valid();
            result.ValidationDuration = duration;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during referential integrity validation for OrchestratedFlowEntity {OrchestratedFlowId}", orchestratedFlowId);
            throw;
        }
    }

    public async Task<ReferentialIntegrityResult> ValidateOrchestratedFlowEntityUpdateAsync(Guid orchestratedFlowId)
    {
        _logger.LogInformation("Validating OrchestratedFlowEntity update for {OrchestratedFlowId}", orchestratedFlowId);
        return await ValidateOrchestratedFlowEntityDeletionAsync(orchestratedFlowId); // Same validation logic
    }

    public async Task<OrchestratedFlowEntityReferenceInfo> GetOrchestratedFlowEntityReferencesAsync(Guid orchestratedFlowId)
    {
        // TaskScheduledEntity removed - no longer need to validate TaskScheduled references
        var references = new OrchestratedFlowEntityReferenceInfo();

        // Note: TaskScheduledEntityCount removed from OrchestratedFlowEntityReferenceInfo
        // OrchestratedFlowEntity now only manages AssignmentIds relationships

        return references;
    }

    private bool IsValidationEnabled()
    {
        return bool.Parse(_configuration["Features:ReferentialIntegrityValidation"] ?? "true");
    }
}
