using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Destination;

public class DeleteDestinationCommandConsumer : IConsumer<DeleteDestinationCommand>
{
    private readonly IDestinationEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteDestinationCommandConsumer> _logger;

    public DeleteDestinationCommandConsumer(
        IDestinationEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteDestinationCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteDestinationCommand> context)
    {
        _logger.LogInformation("Processing DeleteDestinationCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new DestinationDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteDestinationCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("DestinationEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteDestinationCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
