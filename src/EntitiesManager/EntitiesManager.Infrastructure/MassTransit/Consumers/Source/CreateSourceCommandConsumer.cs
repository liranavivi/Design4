using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Source;

public class CreateSourceCommandConsumer : IConsumer<CreateSourceCommand>
{
    private readonly ISourceEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateSourceCommandConsumer> _logger;

    public CreateSourceCommandConsumer(
        ISourceEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateSourceCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateSourceCommand> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("CreateSourceCommand");
        activity?.SetTag("command.type", "CreateSource");
        activity?.SetTag("command.address", context.Message.Address);
        activity?.SetTag("command.version", context.Message.Version);

        try
        {
            var entity = new SourceEntity
            {
                Address = context.Message.Address,
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                Configuration = context.Message.Configuration ?? new Dictionary<string, object>(),
                ProtocolId = context.Message.ProtocolId,
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new SourceCreatedEvent
            {
                Id = created.Id,
                Address = created.Address,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                Configuration = created.Configuration,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateSourceCommand for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateSourceCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Type = "DuplicateKey" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateSourceCommand");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw; // Re-throw to trigger retry policy
        }
    }
}
