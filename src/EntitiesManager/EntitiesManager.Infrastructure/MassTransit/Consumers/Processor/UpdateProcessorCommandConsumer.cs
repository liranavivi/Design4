using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Processor;

public class UpdateProcessorCommandConsumer : IConsumer<UpdateProcessorCommand>
{
    private readonly IProcessorEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateProcessorCommandConsumer> _logger;

    public UpdateProcessorCommandConsumer(
        IProcessorEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateProcessorCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateProcessorCommand> context)
    {
        _logger.LogInformation("Processing UpdateProcessorCommand for ID {Id}", context.Message.Id);

        try
        {
            var existing = await _repository.GetByIdAsync(context.Message.Id);
            if (existing == null)
            {
                _logger.LogWarning("ProcessorEntity with ID {Id} not found for update", context.Message.Id);
                await context.RespondAsync(new { Error = "Entity not found", Success = false });
                return;
            }

            // Update properties
            existing.Version = context.Message.Version;
            existing.Name = context.Message.Name;
            existing.Description = context.Message.Description;
            existing.ProtocolId = context.Message.ProtocolId;
            existing.InputSchema = context.Message.InputSchema;
            existing.OutputSchema = context.Message.OutputSchema;
            existing.UpdatedBy = context.Message.RequestedBy;

            var updated = await _repository.UpdateAsync(existing);

            await _publishEndpoint.Publish(new ProcessorUpdatedEvent
            {
                Id = updated.Id,
                Version = updated.Version,
                Name = updated.Name,
                Description = updated.Description,
                ProtocolId = updated.ProtocolId,
                InputSchema = updated.InputSchema,
                OutputSchema = updated.OutputSchema,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            await context.RespondAsync(updated);

            _logger.LogInformation("Successfully processed UpdateProcessorCommand for ID {Id}", context.Message.Id);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in UpdateProcessorCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UpdateProcessorCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
