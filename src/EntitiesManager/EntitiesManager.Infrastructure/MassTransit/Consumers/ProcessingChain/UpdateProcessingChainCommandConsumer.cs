using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.ProcessingChain;

public class UpdateProcessingChainCommandConsumer : IConsumer<UpdateProcessingChainCommand>
{
    private readonly IProcessingChainEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateProcessingChainCommandConsumer> _logger;

    public UpdateProcessingChainCommandConsumer(
        IProcessingChainEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateProcessingChainCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateProcessingChainCommand> context)
    {
        _logger.LogInformation("Processing UpdateProcessingChainCommand for ID {Id}", context.Message.Id);

        try
        {
            var existing = await _repository.GetByIdAsync(context.Message.Id);
            if (existing == null)
            {
                _logger.LogWarning("ProcessingChainEntity with ID {Id} not found for update", context.Message.Id);
                await context.RespondAsync(new { Error = "Entity not found", Success = false });
                return;
            }

            // Update properties
            existing.Version = context.Message.Version;
            existing.Name = context.Message.Name;
            existing.Description = context.Message.Description;
            existing.StepIds = context.Message.StepIds ?? new List<Guid>();
            existing.UpdatedBy = context.Message.RequestedBy;

            var updated = await _repository.UpdateAsync(existing);

            await _publishEndpoint.Publish(new ProcessingChainUpdatedEvent
            {
                Id = updated.Id,
                Version = updated.Version,
                Name = updated.Name,
                Description = updated.Description,
                StepIds = updated.StepIds,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            await context.RespondAsync(updated);

            _logger.LogInformation("Successfully processed UpdateProcessingChainCommand for ID {Id}", context.Message.Id);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in UpdateProcessingChainCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UpdateProcessingChainCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
