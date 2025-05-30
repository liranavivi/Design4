using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.ProcessingChain;

public class GetProcessingChainQueryConsumer : IConsumer<GetProcessingChainQuery>
{
    private readonly IProcessingChainEntityRepository _repository;
    private readonly ILogger<GetProcessingChainQueryConsumer> _logger;

    public GetProcessingChainQueryConsumer(IProcessingChainEntityRepository repository, ILogger<GetProcessingChainQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetProcessingChainQuery> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetProcessingChainQuery");

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
                    await context.RespondAsync(new { Error = "ProcessingChain not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Id must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed GetProcessingChainQuery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetProcessingChainQuery");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
