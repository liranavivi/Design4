using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.ScheduledFlow;

public class DeleteScheduledFlowCommandConsumer : IConsumer<DeleteScheduledFlowCommand>
{
    private readonly IScheduledFlowEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteScheduledFlowCommandConsumer> _logger;

    public DeleteScheduledFlowCommandConsumer(
        IScheduledFlowEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteScheduledFlowCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteScheduledFlowCommand> context)
    {
        _logger.LogInformation("Processing DeleteScheduledFlowCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new ScheduledFlowDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteScheduledFlowCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("ScheduledFlowEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteScheduledFlowCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
