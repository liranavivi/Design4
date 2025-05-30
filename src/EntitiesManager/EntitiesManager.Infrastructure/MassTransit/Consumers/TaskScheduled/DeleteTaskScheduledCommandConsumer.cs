using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.TaskScheduled;

public class DeleteTaskScheduledCommandConsumer : IConsumer<DeleteTaskScheduledCommand>
{
    private readonly ITaskScheduledEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteTaskScheduledCommandConsumer> _logger;

    public DeleteTaskScheduledCommandConsumer(
        ITaskScheduledEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteTaskScheduledCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteTaskScheduledCommand> context)
    {
        _logger.LogInformation("Processing DeleteTaskScheduledCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new TaskScheduledDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteTaskScheduledCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("TaskScheduledEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteTaskScheduledCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
