using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Step;

public class CreateStepCommandConsumer : IConsumer<CreateStepCommand>
{
    private readonly IStepEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateStepCommandConsumer> _logger;

    public CreateStepCommandConsumer(
        IStepEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateStepCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateStepCommand> context)
    {
        _logger.LogInformation("Processing CreateStepCommand for EntityId: {EntityId}",
            context.Message.EntityId);

        try
        {
            var entity = new StepEntity
            {
                EntityId = context.Message.EntityId,
                NextStepIds = context.Message.NextStepIds ?? new List<Guid>(),
                Description = context.Message.Description,
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new StepCreatedEvent
            {
                Id = created.Id,
                EntityId = created.EntityId,
                NextStepIds = created.NextStepIds,
                Description = created.Description,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateStepCommand for EntityId: {EntityId}",
                context.Message.EntityId);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateStepCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateStepCommand for EntityId: {EntityId}",
                context.Message.EntityId);
            throw;
        }
    }
}
