using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Exporter;

public class DeleteExporterCommandConsumer : IConsumer<DeleteExporterCommand>
{
    private readonly IExporterEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteExporterCommandConsumer> _logger;

    public DeleteExporterCommandConsumer(
        IExporterEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteExporterCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteExporterCommand> context)
    {
        _logger.LogInformation("Processing DeleteExporterCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new ExporterDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteExporterCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("ExporterEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteExporterCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
