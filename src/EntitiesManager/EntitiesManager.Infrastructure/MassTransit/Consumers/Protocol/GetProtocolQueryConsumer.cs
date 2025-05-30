using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Protocol;

public class GetProtocolQueryConsumer : IConsumer<GetProtocolQuery>
{
    private readonly IProtocolEntityRepository _repository;
    private readonly ILogger<GetProtocolQueryConsumer> _logger;

    public GetProtocolQueryConsumer(IProtocolEntityRepository repository, ILogger<GetProtocolQueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetProtocolQuery> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetProtocolQuery");

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
                    await context.RespondAsync(new { Error = "Protocol not found", Type = "NotFound" });
            }
            else if (!string.IsNullOrEmpty(context.Message.CompositeKey))
            {
                activity?.SetTag("query.type", "ByCompositeKey");
                activity?.SetTag("query.compositeKey", context.Message.CompositeKey);

                var entity = await _repository.GetByCompositeKeyAsync(context.Message.CompositeKey);
                if (entity != null)
                    await context.RespondAsync(entity);
                else
                    await context.RespondAsync(new { Error = "Protocol not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Either Id or CompositeKey must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed GetProtocolQuery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetProtocolQuery");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
