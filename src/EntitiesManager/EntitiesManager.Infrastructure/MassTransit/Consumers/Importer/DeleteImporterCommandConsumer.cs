using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Importer;

public class DeleteImporterCommandConsumer : IConsumer<DeleteImporterCommand>
{
    private readonly IImporterEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteImporterCommandConsumer> _logger;

    public DeleteImporterCommandConsumer(
        IImporterEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteImporterCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteImporterCommand> context)
    {
        _logger.LogInformation("Processing DeleteImporterCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new ImporterDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteImporterCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("ImporterEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteImporterCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
