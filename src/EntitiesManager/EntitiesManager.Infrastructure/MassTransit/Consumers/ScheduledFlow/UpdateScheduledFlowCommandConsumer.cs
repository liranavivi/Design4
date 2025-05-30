using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.ScheduledFlow;

public class UpdateScheduledFlowCommandConsumer : IConsumer<UpdateScheduledFlowCommand>
{
    private readonly IScheduledFlowEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateScheduledFlowCommandConsumer> _logger;

    public UpdateScheduledFlowCommandConsumer(
        IScheduledFlowEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateScheduledFlowCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateScheduledFlowCommand> context)
    {
        _logger.LogInformation("Processing UpdateScheduledFlowCommand for ID {Id}", context.Message.Id);

        try
        {
            var existing = await _repository.GetByIdAsync(context.Message.Id);
            if (existing == null)
            {
                _logger.LogWarning("ScheduledFlowEntity with ID {Id} not found for update", context.Message.Id);
                await context.RespondAsync(new { Error = "Entity not found", Success = false });
                return;
            }

            // Update properties
            existing.Address = context.Message.Address;
            existing.Version = context.Message.Version;
            existing.Name = context.Message.Name;
            existing.Description = context.Message.Description;
            existing.Configuration = context.Message.Configuration ?? new Dictionary<string, object>();
            existing.UpdatedBy = context.Message.RequestedBy;

            var updated = await _repository.UpdateAsync(existing);

            await _publishEndpoint.Publish(new ScheduledFlowUpdatedEvent
            {
                Id = updated.Id,
                Address = updated.Address,
                Version = updated.Version,
                Name = updated.Name,
                Description = updated.Description,
                Configuration = updated.Configuration,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            await context.RespondAsync(updated);

            _logger.LogInformation("Successfully processed UpdateScheduledFlowCommand for ID {Id}", context.Message.Id);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in UpdateScheduledFlowCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UpdateScheduledFlowCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
