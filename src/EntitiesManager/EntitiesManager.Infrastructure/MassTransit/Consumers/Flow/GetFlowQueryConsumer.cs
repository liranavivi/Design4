using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Flow;

public class GetFlowQueryConsumer : IConsumer<GetFlowQuery>
{
    private readonly IFlowEntityRepository _repository;
    private readonly ILogger<GetFlowQueryConsumer> _logger;

    public GetFlowQueryConsumer(IFlowEntityRepository repository, ILogger<GetFlowQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetFlowQuery> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetFlowQuery");

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
                    await context.RespondAsync(new { Error = "Flow not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Id must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed GetFlowQuery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetFlowQuery");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
