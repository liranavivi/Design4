using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.OrchestratedFlow;

public class GetOrchestratedFlowQueryConsumer : IConsumer<GetOrchestratedFlowQuery>
{
    private readonly IOrchestratedFlowEntityRepository _repository;
    private readonly ILogger<GetOrchestratedFlowQueryConsumer> _logger;

    public GetOrchestratedFlowQueryConsumer(IOrchestratedFlowEntityRepository repository, ILogger<GetOrchestratedFlowQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetOrchestratedFlowQuery> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetOrchestratedFlowQuery");

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
                    await context.RespondAsync(new { Error = "OrchestratedFlow not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Id must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed GetOrchestratedFlowQuery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetOrchestratedFlowQuery");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
