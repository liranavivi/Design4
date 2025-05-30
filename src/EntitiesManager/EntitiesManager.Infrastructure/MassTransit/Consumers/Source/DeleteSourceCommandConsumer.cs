using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Source;

public class DeleteSourceCommandConsumer : IConsumer<DeleteSourceCommand>
{
    private readonly ISourceEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteSourceCommandConsumer> _logger;

    public DeleteSourceCommandConsumer(
        ISourceEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteSourceCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteSourceCommand> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("DeleteSourceCommand");
        activity?.SetTag("command.type", "DeleteSource");
        activity?.SetTag("command.id", context.Message.Id.ToString());

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (!deleted)
            {
                await context.RespondAsync(new { Error = "Source not found", Type = "NotFound" });
                return;
            }

            await _publishEndpoint.Publish(new SourceDeletedEvent
            {
                Id = context.Message.Id,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = context.Message.RequestedBy
            });

            await context.RespondAsync(new { Success = true, Message = "Source deleted successfully" });

            _logger.LogInformation("Successfully processed DeleteSourceCommand for ID {Id}", context.Message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteSourceCommand");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
