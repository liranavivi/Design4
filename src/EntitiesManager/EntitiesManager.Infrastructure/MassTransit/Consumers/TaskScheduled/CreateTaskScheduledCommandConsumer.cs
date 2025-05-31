using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.TaskScheduled;

public class CreateTaskScheduledCommandConsumer : IConsumer<CreateTaskScheduledCommand>
{
    private readonly ITaskScheduledEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateTaskScheduledCommandConsumer> _logger;

    public CreateTaskScheduledCommandConsumer(
        ITaskScheduledEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateTaskScheduledCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateTaskScheduledCommand> context)
    {
        _logger.LogInformation("Processing CreateTaskScheduledCommand for Version: {Version}, ScheduledFlowId: {ScheduledFlowId}",
            context.Message.Version, context.Message.ScheduledFlowId);

        try
        {
            var entity = new TaskScheduledEntity
            {
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                ScheduledFlowId = context.Message.ScheduledFlowId,
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new TaskScheduledCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                ScheduledFlowId = created.ScheduledFlowId,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateTaskScheduledCommand for Version: {Version}, ScheduledFlowId: {ScheduledFlowId}",
                context.Message.Version, context.Message.ScheduledFlowId);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateTaskScheduledCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateTaskScheduledCommand for Version: {Version}, ScheduledFlowId: {ScheduledFlowId}",
                context.Message.Version, context.Message.ScheduledFlowId);
            throw;
        }
    }
}
