// REFERENTIAL INTEGRITY IMPLEMENTATION EXAMPLES
// Complete code examples for implementing ProtocolEntity referential integrity validation

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
    // 1. CORE INTERFACES AND RESULT TYPES
    // ===================================================================

    public interface IReferentialIntegrityService
    {
        Task<ReferentialIntegrityResult> ValidateProtocolDeletionAsync(Guid protocolId);
        Task<ReferentialIntegrityResult> ValidateProtocolUpdateAsync(Guid currentId, Guid newId);
        Task<List<ReferenceInfo>> GetProtocolReferencesAsync(Guid protocolId);
    }

    public class ReferentialIntegrityResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public List<ReferenceInfo> References { get; private set; } = new();
        public TimeSpan ValidationDuration { get; set; }

        public static ReferentialIntegrityResult Valid() => new() { IsValid = true };
        
        public static ReferentialIntegrityResult Invalid(string message, List<ReferenceInfo> references) => new() 
        { 
            IsValid = false, 
            ErrorMessage = message, 
            References = references 
        };
    }

    public class ReferenceInfo
    {
        public string EntityType { get; set; } = string.Empty;
        public long Count { get; set; }
        public string CollectionName { get; set; } = string.Empty;
    }

    public class ReferentialIntegrityException : Exception
    {
        public List<ReferenceInfo> References { get; }

        public ReferentialIntegrityException(string message, List<ReferenceInfo> references) 
            : base(message)
        {
            References = references;
        }

        public ReferentialIntegrityException(string message, List<ReferenceInfo> references, Exception innerException) 
            : base(message, innerException)
        {
            References = references;
        }
    }

    // ===================================================================
    // 2. REFERENTIAL INTEGRITY SERVICE IMPLEMENTATION
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
                var referencesWithData = references.Where(r => r.Count > 0).ToList();

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {ReferenceCount} references", 
                    duration.TotalMilliseconds, referencesWithData.Sum(r => r.Count));

                if (referencesWithData.Any())
                {
                    var errorMessage = $"Cannot delete ProtocolEntity. Referenced by: {string.Join(", ", referencesWithData.Select(r => $"{r.EntityType} ({r.Count} records)"))}";
                    return ReferentialIntegrityResult.Invalid(errorMessage, referencesWithData);
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

        public async Task<List<ReferenceInfo>> GetProtocolReferencesAsync(Guid protocolId)
        {
            var enableParallel = _configuration.GetValue<bool>("ReferentialIntegrity:EnableParallelValidation", true);
            var timeoutMs = _configuration.GetValue<int>("ReferentialIntegrity:ValidationTimeoutMs", 5000);

            var referenceChecks = new List<Func<Task<ReferenceInfo>>>
            {
                () => CheckEntityReferencesAsync<SourceEntity>("sources", "SourceEntity", protocolId),
                () => CheckEntityReferencesAsync<DestinationEntity>("destinations", "DestinationEntity", protocolId),
                () => CheckEntityReferencesAsync<ImporterEntity>("importers", "ImporterEntity", protocolId),
                () => CheckEntityReferencesAsync<ExporterEntity>("exporters", "ExporterEntity", protocolId),
                () => CheckEntityReferencesAsync<ProcessorEntity>("processors", "ProcessorEntity", protocolId)
            };

            if (enableParallel)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                var tasks = referenceChecks.Select(check => check()).ToArray();
                var results = await Task.WhenAll(tasks);
                return results.ToList();
            }
            else
            {
                var results = new List<ReferenceInfo>();
                foreach (var check in referenceChecks)
                {
                    results.Add(await check());
                }
                return results;
            }
        }

        private async Task<ReferenceInfo> CheckEntityReferencesAsync<T>(string collectionName, string entityType, Guid protocolId)
            where T : class
        {
            try
            {
                var collection = _database.GetCollection<T>(collectionName);
                var filter = Builders<T>.Filter.Eq("protocolId", protocolId);
                var count = await collection.CountDocumentsAsync(filter);

                _logger.LogDebug("Found {Count} references in {EntityType} for ProtocolId {ProtocolId}", count, entityType, protocolId);

                return new ReferenceInfo
                {
                    EntityType = entityType,
                    CollectionName = collectionName,
                    Count = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking references in {EntityType} for ProtocolId {ProtocolId}", entityType, protocolId);
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
                    _logger.LogWarning("Referential integrity violation prevented deletion of ProtocolEntity {Id}: {Error}", 
                        id, validationResult.ErrorMessage);
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
                    _logger.LogWarning("Referential integrity violation in DeleteProtocolCommand for ID {Id}: {Error}", 
                        context.Message.Id, validationResult.ErrorMessage);

                    await context.RespondAsync(new 
                    { 
                        Success = false, 
                        Error = validationResult.ErrorMessage,
                        ErrorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                        References = validationResult.References
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
                    References = ex.References
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
