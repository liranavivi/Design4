using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.ProcessingChain;

public class CreateProcessingChainCommandConsumer : IConsumer<CreateProcessingChainCommand>
{
    private readonly IProcessingChainEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateProcessingChainCommandConsumer> _logger;

    public CreateProcessingChainCommandConsumer(
        IProcessingChainEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateProcessingChainCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateProcessingChainCommand> context)
    {
        _logger.LogInformation("Processing CreateProcessingChainCommand for {Version}_{Name}",
            context.Message.Version, context.Message.Name);

        try
        {
            var entity = new ProcessingChainEntity
            {
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                StepIds = context.Message.StepIds ?? new List<Guid>(),
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new ProcessingChainCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                StepIds = created.StepIds,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateProcessingChainCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateProcessingChainCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateProcessingChainCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
            throw;
        }
    }
}
