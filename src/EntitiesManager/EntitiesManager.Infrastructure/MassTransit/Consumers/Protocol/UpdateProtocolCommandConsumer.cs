using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Protocol;

public class UpdateProtocolCommandConsumer : IConsumer<UpdateProtocolCommand>
{
    private readonly IProtocolEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateProtocolCommandConsumer> _logger;

    public UpdateProtocolCommandConsumer(
        IProtocolEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateProtocolCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateProtocolCommand> context)
    {
        _logger.LogInformation("Processing UpdateProtocolCommand for ID {Id}", context.Message.Id);

        try
        {
            var existing = await _repository.GetByIdAsync(context.Message.Id);
            if (existing == null)
            {
                _logger.LogWarning("ProtocolEntity with ID {Id} not found for update", context.Message.Id);
                await context.RespondAsync(new { Error = "Entity not found", Success = false });
                return;
            }

            // Update properties
            // Version property no longer exists on ProtocolEntity
            existing.Name = context.Message.Name;
            existing.Description = context.Message.Description;
            existing.UpdatedBy = context.Message.RequestedBy;

            var updated = await _repository.UpdateAsync(existing);

            await _publishEndpoint.Publish(new ProtocolUpdatedEvent
            {
                Id = updated.Id,
                Version = context.Message.Version, // Use version from command for event compatibility
                Name = updated.Name,
                Description = updated.Description,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            await context.RespondAsync(updated);

            _logger.LogInformation("Successfully processed UpdateProtocolCommand for ID {Id}", context.Message.Id);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in UpdateProtocolCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UpdateProtocolCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
