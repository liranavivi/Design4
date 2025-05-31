// FOCUSED REFERENTIAL INTEGRITY IMPLEMENTATION
// ProtocolEntity validation for SourceEntity and DestinationEntity references only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using EntitiesManager.Core.Entities;

namespace EntitiesManager.Infrastructure.Services
{
    // ===================================================================
    // 1. FOCUSED INTERFACES AND RESULT TYPES
    // ===================================================================

    public interface IReferentialIntegrityService
    {
        Task<ReferentialIntegrityResult> ValidateProtocolDeletionAsync(Guid protocolId);
        Task<ReferentialIntegrityResult> ValidateProtocolUpdateAsync(Guid currentId, Guid newId);
        Task<ProtocolReferenceInfo> GetProtocolReferencesAsync(Guid protocolId);
    }

    public class ReferentialIntegrityResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public ProtocolReferenceInfo References { get; private set; } = new();
        public TimeSpan ValidationDuration { get; set; }

        public static ReferentialIntegrityResult Valid() => new() { IsValid = true };
        
        public static ReferentialIntegrityResult Invalid(string message, ProtocolReferenceInfo references) => new() 
        { 
            IsValid = false, 
            ErrorMessage = message, 
            References = references 
        };
    }

    public class ProtocolReferenceInfo
    {
        public long SourceEntityCount { get; set; }
        public long DestinationEntityCount { get; set; }
        public long TotalReferences => SourceEntityCount + DestinationEntityCount;
        public bool HasReferences => TotalReferences > 0;

        public List<string> GetReferencingEntityTypes()
        {
            var types = new List<string>();
            if (SourceEntityCount > 0) types.Add($"SourceEntity ({SourceEntityCount} records)");
            if (DestinationEntityCount > 0) types.Add($"DestinationEntity ({DestinationEntityCount} records)");
            return types;
        }
    }

    public class ReferentialIntegrityException : Exception
    {
        public ProtocolReferenceInfo References { get; }

        public ReferentialIntegrityException(string message, ProtocolReferenceInfo references) 
            : base(message)
        {
            References = references;
        }

        public ReferentialIntegrityException(string message, ProtocolReferenceInfo references, Exception innerException) 
            : base(message, innerException)
        {
            References = references;
        }
    }

    // ===================================================================
    // 2. FOCUSED REFERENTIAL INTEGRITY SERVICE
    // ===================================================================

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

                return ReferentialIntegrityResult.Valid() { ValidationDuration = duration };
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
            var enableParallel = _configuration.GetValue<bool>("ReferentialIntegrity:EnableParallelValidation", true);
            var validateSources = _configuration.GetValue<bool>("ReferentialIntegrity:ValidateSourceReferences", true);
            var validateDestinations = _configuration.GetValue<bool>("ReferentialIntegrity:ValidateDestinationReferences", true);

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
            return _configuration.GetValue<bool>("Features:ReferentialIntegrityValidation", true);
        }
    }

    // ===================================================================
    // 3. ENHANCED PROTOCOL ENTITY REPOSITORY
    // ===================================================================

    public class EnhancedProtocolEntityRepository : ProtocolEntityRepository
    {
        private readonly IReferentialIntegrityService _integrityService;

        public EnhancedProtocolEntityRepository(
            IMongoDatabase database, 
            ILogger<ProtocolEntityRepository> logger, 
            IEventPublisher eventPublisher,
            IReferentialIntegrityService integrityService)
            : base(database, logger, eventPublisher)
        {
            _integrityService = integrityService;
        }

        public override async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Validating referential integrity before deleting ProtocolEntity {Id}", id);

            try
            {
                var validationResult = await _integrityService.ValidateProtocolDeletionAsync(id);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Referential integrity violation prevented deletion of ProtocolEntity {Id}: {Error}. References: {SourceCount} sources, {DestinationCount} destinations", 
                        id, validationResult.ErrorMessage, validationResult.References.SourceEntityCount, validationResult.References.DestinationEntityCount);
                    throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.References);
                }

                _logger.LogInformation("Referential integrity validation passed for ProtocolEntity {Id}. Proceeding with deletion", id);
                return await base.DeleteAsync(id);
            }
            catch (ReferentialIntegrityException)
            {
                throw; // Re-throw referential integrity exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during referential integrity validation for ProtocolEntity {Id}", id);
                throw;
            }
        }

        public override async Task<ProtocolEntity> UpdateAsync(ProtocolEntity entity)
        {
            // Check if ID is changing (rare but possible in some scenarios)
            var existing = await GetByIdAsync(entity.Id);
            if (existing != null && existing.Id != entity.Id)
            {
                _logger.LogInformation("Validating referential integrity for ProtocolEntity ID change from {OldId} to {NewId}", 
                    existing.Id, entity.Id);

                var validationResult = await _integrityService.ValidateProtocolUpdateAsync(existing.Id, entity.Id);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Referential integrity violation prevented update of ProtocolEntity {Id}: {Error}", 
                        existing.Id, validationResult.ErrorMessage);
                    throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.References);
                }
            }

            return await base.UpdateAsync(entity);
        }
    }

    // ===================================================================
    // 4. ENHANCED MASSTRANSIT CONSUMERS
    // ===================================================================

    public class EnhancedDeleteProtocolCommandConsumer : IConsumer<DeleteProtocolCommand>
    {
        private readonly IProtocolEntityRepository _repository;
        private readonly IReferentialIntegrityService _integrityService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<EnhancedDeleteProtocolCommandConsumer> _logger;

        public EnhancedDeleteProtocolCommandConsumer(
            IProtocolEntityRepository repository,
            IReferentialIntegrityService integrityService,
            IPublishEndpoint publishEndpoint,
            ILogger<EnhancedDeleteProtocolCommandConsumer> logger)
        {
            _repository = repository;
            _integrityService = integrityService;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<DeleteProtocolCommand> context)
        {
            _logger.LogInformation("Processing DeleteProtocolCommand for ID {Id}", context.Message.Id);

            try
            {
                // Validate referential integrity before deletion
                var validationResult = await _integrityService.ValidateProtocolDeletionAsync(context.Message.Id);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Referential integrity violation in DeleteProtocolCommand for ID {Id}: {Error}. References: {SourceCount} sources, {DestinationCount} destinations", 
                        context.Message.Id, validationResult.ErrorMessage, validationResult.References.SourceEntityCount, validationResult.References.DestinationEntityCount);

                    await context.RespondAsync(new 
                    { 
                        Success = false, 
                        Error = validationResult.ErrorMessage,
                        ErrorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                        References = new
                        {
                            SourceEntityCount = validationResult.References.SourceEntityCount,
                            DestinationEntityCount = validationResult.References.DestinationEntityCount,
                            TotalReferences = validationResult.References.TotalReferences
                        }
                    });
                    return;
                }

                // Proceed with deletion
                var deleted = await _repository.DeleteAsync(context.Message.Id);

                if (deleted)
                {
                    await _publishEndpoint.Publish(new ProtocolDeletedEvent
                    {
                        Id = context.Message.Id,
                        DeletedAt = DateTime.UtcNow,
                        DeletedBy = context.Message.RequestedBy
                    });

                    await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                    _logger.LogInformation("Successfully processed DeleteProtocolCommand for ID {Id}", context.Message.Id);
                }
                else
                {
                    _logger.LogWarning("ProtocolEntity with ID {Id} not found for deletion", context.Message.Id);
                    await context.RespondAsync(new { Success = false, Error = "Entity not found" });
                }
            }
            catch (ReferentialIntegrityException ex)
            {
                _logger.LogWarning("Referential integrity violation in DeleteProtocolCommand for ID {Id}: {Error}", 
                    context.Message.Id, ex.Message);
                await context.RespondAsync(new 
                { 
                    Success = false, 
                    Error = ex.Message,
                    ErrorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                    References = new
                    {
                        SourceEntityCount = ex.References.SourceEntityCount,
                        DestinationEntityCount = ex.References.DestinationEntityCount,
                        TotalReferences = ex.References.TotalReferences
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DeleteProtocolCommand for ID {Id}", context.Message.Id);
                throw;
            }
        }
    }
}
