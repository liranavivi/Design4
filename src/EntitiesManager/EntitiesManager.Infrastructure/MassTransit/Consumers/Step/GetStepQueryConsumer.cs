using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Step;

public class GetStepQueryConsumer : IConsumer<GetStepQuery>
{
    private readonly IStepEntityRepository _repository;
    private readonly ILogger<GetStepQueryConsumer> _logger;

    public GetStepQueryConsumer(IStepEntityRepository repository, ILogger<GetStepQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetStepQuery> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetStepQuery");

        try
        {
            if (context.Message.Id.HasValue)
            {
                activity?.SetTag("query.type", "ById");
                activity?.SetTag("query.id", context.Message.Id.Value.ToString());

                var entity = await _repository.GetByIdAsync(context.Message.Id.Value);
                if (entity != null)
                    await context.RespondAsync(entity);
                else
                    await context.RespondAsync(new { Error = "Step not found", Type = "NotFound" });
            }
            else if (context.Message.EntityId.HasValue && context.Message.EntityId != Guid.Empty)
            {
                activity?.SetTag("query.type", "ByEntityId");
                activity?.SetTag("query.entityId", context.Message.EntityId.ToString());

                var entities = await _repository.GetByEntityIdAsync(context.Message.EntityId.Value);
                if (entities.Any())
                    await context.RespondAsync(entities);
                else
                    await context.RespondAsync(new { Error = "Steps not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Either Id or EntityId must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed GetStepQuery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetStepQuery");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
