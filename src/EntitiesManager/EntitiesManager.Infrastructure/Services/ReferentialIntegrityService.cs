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

    private bool IsValidationEnabled()
    {
        return bool.Parse(_configuration["Features:ReferentialIntegrityValidation"] ?? "true");
    }
}
