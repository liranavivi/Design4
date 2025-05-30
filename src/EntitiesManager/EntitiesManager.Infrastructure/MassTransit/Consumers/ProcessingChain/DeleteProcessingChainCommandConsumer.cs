using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.ProcessingChain;

public class DeleteProcessingChainCommandConsumer : IConsumer<DeleteProcessingChainCommand>
{
    private readonly IProcessingChainEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteProcessingChainCommandConsumer> _logger;

    public DeleteProcessingChainCommandConsumer(
        IProcessingChainEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteProcessingChainCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteProcessingChainCommand> context)
    {
        _logger.LogInformation("Processing DeleteProcessingChainCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new ProcessingChainDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteProcessingChainCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("ProcessingChainEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteProcessingChainCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
