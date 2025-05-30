using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Source;

public class UpdateSourceCommandConsumer : IConsumer<UpdateSourceCommand>
{
    private readonly ISourceEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UpdateSourceCommandConsumer> _logger;

    public UpdateSourceCommandConsumer(
        ISourceEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateSourceCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateSourceCommand> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("UpdateSourceCommand");
        activity?.SetTag("command.type", "UpdateSource");
        activity?.SetTag("command.id", context.Message.Id.ToString());

        try
        {
            var existing = await _repository.GetByIdAsync(context.Message.Id);
            if (existing == null)
            {
                await context.RespondAsync(new { Error = "Source not found", Type = "NotFound" });
                return;
            }

            existing.Address = context.Message.Address;
            existing.Version = context.Message.Version;
            existing.Name = context.Message.Name;
            existing.Description = context.Message.Description;
            existing.Configuration = context.Message.Configuration ?? new Dictionary<string, object>();
            existing.ProtocolId = context.Message.ProtocolId;
            existing.UpdatedBy = context.Message.RequestedBy;

            var updated = await _repository.UpdateAsync(existing);

            await _publishEndpoint.Publish(new SourceUpdatedEvent
            {
                Id = updated.Id,
                Address = updated.Address,
                Version = updated.Version,
                Name = updated.Name,
                Description = updated.Description,
                Configuration = updated.Configuration,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            await context.RespondAsync(updated);

            _logger.LogInformation("Successfully processed UpdateSourceCommand for ID {Id}", context.Message.Id);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in UpdateSourceCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Type = "DuplicateKey" });
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning("Entity not found in UpdateSourceCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Type = "NotFound" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UpdateSourceCommand");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
