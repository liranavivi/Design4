using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Flow;

public class CreateFlowCommandConsumer : IConsumer<CreateFlowCommand>
{
    private readonly IFlowEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateFlowCommandConsumer> _logger;

    public CreateFlowCommandConsumer(
        IFlowEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateFlowCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateFlowCommand> context)
    {
        _logger.LogInformation("Processing CreateFlowCommand for {Address}_{Version}",
            context.Message.Address, context.Message.Version);

        try
        {
            var entity = new FlowEntity
            {
                Address = context.Message.Address,
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                Configuration = context.Message.Configuration ?? new Dictionary<string, object>(),
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new FlowCreatedEvent
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

            _logger.LogInformation("Successfully processed CreateFlowCommand for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateFlowCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateFlowCommand for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
            throw;
        }
    }
}
