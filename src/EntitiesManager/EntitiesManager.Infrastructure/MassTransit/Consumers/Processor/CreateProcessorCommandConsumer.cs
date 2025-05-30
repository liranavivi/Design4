using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Processor;

public class CreateProcessorCommandConsumer : IConsumer<CreateProcessorCommand>
{
    private readonly IProcessorEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateProcessorCommandConsumer> _logger;

    public CreateProcessorCommandConsumer(
        IProcessorEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateProcessorCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateProcessorCommand> context)
    {
        _logger.LogInformation("Processing CreateProcessorCommand for {Version}_{Name}",
            context.Message.Version, context.Message.Name);

        try
        {
            var entity = new ProcessorEntity
            {
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                ProtocolId = context.Message.ProtocolId,
                InputSchema = context.Message.InputSchema,
                OutputSchema = context.Message.OutputSchema,
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new ProcessorCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                ProtocolId = created.ProtocolId,
                InputSchema = created.InputSchema,
                OutputSchema = created.OutputSchema,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateProcessorCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateProcessorCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateProcessorCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
            throw;
        }
    }
}
