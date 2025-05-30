using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Destination;

public class CreateDestinationCommandConsumer : IConsumer<CreateDestinationCommand>
{
    private readonly IDestinationEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateDestinationCommandConsumer> _logger;

    public CreateDestinationCommandConsumer(
        IDestinationEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateDestinationCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateDestinationCommand> context)
    {
        _logger.LogInformation("Processing CreateDestinationCommand for {Address}_{Version}",
            context.Message.Address, context.Message.Version);

        try
        {
            var entity = new DestinationEntity
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

            await _publishEndpoint.Publish(new DestinationCreatedEvent
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

            _logger.LogInformation("Successfully processed CreateDestinationCommand for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateDestinationCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateDestinationCommand for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
            throw;
        }
    }
}
