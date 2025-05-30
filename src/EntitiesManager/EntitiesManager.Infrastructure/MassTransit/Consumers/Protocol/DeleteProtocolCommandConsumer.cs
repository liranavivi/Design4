using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Protocol;

public class DeleteProtocolCommandConsumer : IConsumer<DeleteProtocolCommand>
{
    private readonly IProtocolEntityRepository _repository;
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

    public async Task Consume(ConsumeContext<DeleteProtocolCommand> context)
    {
        _logger.LogInformation("Processing DeleteProtocolCommand for ID {Id}", context.Message.Id);

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteProtocolCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
