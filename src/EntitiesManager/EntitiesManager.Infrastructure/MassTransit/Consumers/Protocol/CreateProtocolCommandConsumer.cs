using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Protocol;

public class CreateProtocolCommandConsumer : IConsumer<CreateProtocolCommand>
{
    private readonly IProtocolEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateProtocolCommandConsumer> _logger;

    public CreateProtocolCommandConsumer(
        IProtocolEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateProtocolCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateProtocolCommand> context)
    {
        _logger.LogInformation("Processing CreateProtocolCommand for {Name}_{Version}",
            context.Message.Name, context.Message.Version);

        try
        {
            var entity = new ProtocolEntity
            {
                // Version property no longer exists on ProtocolEntity
                Name = context.Message.Name,
                Description = context.Message.Description,
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new ProtocolCreatedEvent
            {
                Id = created.Id,
                Version = context.Message.Version, // Use version from command for event compatibility
                Name = created.Name,
                Description = created.Description,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateProtocolCommand for {Name}_{Version}",
                context.Message.Name, context.Message.Version);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateProtocolCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateProtocolCommand for {Name}_{Version}",
                context.Message.Name, context.Message.Version);
            throw;
        }
    }
}
