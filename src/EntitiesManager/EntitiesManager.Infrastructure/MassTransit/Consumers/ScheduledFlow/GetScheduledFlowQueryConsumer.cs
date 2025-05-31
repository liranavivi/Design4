using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.ScheduledFlow;

public class GetScheduledFlowQueryConsumer : IConsumer<GetScheduledFlowQuery>
{
    private readonly IScheduledFlowEntityRepository _repository;
    private readonly ILogger<GetScheduledFlowQueryConsumer> _logger;

    public GetScheduledFlowQueryConsumer(IScheduledFlowEntityRepository repository, ILogger<GetScheduledFlowQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetScheduledFlowQuery> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetScheduledFlowQuery");

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
                    await context.RespondAsync(new { Error = "ScheduledFlow not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Id must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed GetScheduledFlowQuery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetScheduledFlowQuery");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
