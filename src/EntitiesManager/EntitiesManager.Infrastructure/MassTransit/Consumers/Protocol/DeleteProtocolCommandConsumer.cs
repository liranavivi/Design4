using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Protocol;

public class DeleteProtocolCommandConsumer : IConsumer<DeleteProtocolCommand>
{
    private readonly IProtocolEntityRepository _repository;
    private readonly IReferentialIntegrityService? _integrityService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteProtocolCommandConsumer> _logger;

    public DeleteProtocolCommandConsumer(
        IProtocolEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteProtocolCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public DeleteProtocolCommandConsumer(
        IProtocolEntityRepository repository,
        IReferentialIntegrityService integrityService,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteProtocolCommandConsumer> logger)
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
            // Validate referential integrity before deletion if service is available
            if (_integrityService != null)
            {
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
            }

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
