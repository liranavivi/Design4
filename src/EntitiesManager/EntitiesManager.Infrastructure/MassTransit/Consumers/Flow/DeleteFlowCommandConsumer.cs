using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Flow;

public class DeleteFlowCommandConsumer : IConsumer<DeleteFlowCommand>
{
    private readonly IFlowEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteFlowCommandConsumer> _logger;

    public DeleteFlowCommandConsumer(
        IFlowEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteFlowCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteFlowCommand> context)
    {
        _logger.LogInformation("Processing DeleteFlowCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new FlowDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteFlowCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("FlowEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteFlowCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
