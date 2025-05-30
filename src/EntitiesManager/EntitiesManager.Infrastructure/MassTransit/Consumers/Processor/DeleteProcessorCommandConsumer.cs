using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Processor;

public class DeleteProcessorCommandConsumer : IConsumer<DeleteProcessorCommand>
{
    private readonly IProcessorEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteProcessorCommandConsumer> _logger;

    public DeleteProcessorCommandConsumer(
        IProcessorEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteProcessorCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteProcessorCommand> context)
    {
        _logger.LogInformation("Processing DeleteProcessorCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new ProcessorDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteProcessorCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("ProcessorEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteProcessorCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
