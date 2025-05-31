using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.OrchestratedFlow;

public class CreateOrchestratedFlowCommandConsumer : IConsumer<CreateOrchestratedFlowCommand>
{
    private readonly IOrchestratedFlowEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateOrchestratedFlowCommandConsumer> _logger;

    public CreateOrchestratedFlowCommandConsumer(
        IOrchestratedFlowEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateOrchestratedFlowCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateOrchestratedFlowCommand> context)
    {
        _logger.LogInformation("Processing CreateOrchestratedFlowCommand for {Version}_{Name}",
            context.Message.Version, context.Message.Name);

        try
        {
            var entity = new OrchestratedFlowEntity
            {
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                AssignmentIds = context.Message.AssignmentIds ?? new List<Guid>(),
                FlowId = context.Message.FlowId,
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new OrchestratedFlowCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                AssignmentIds = created.AssignmentIds,
                FlowId = created.FlowId,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateOrchestratedFlowCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateOrchestratedFlowCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateOrchestratedFlowCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
            throw;
        }
    }
}
