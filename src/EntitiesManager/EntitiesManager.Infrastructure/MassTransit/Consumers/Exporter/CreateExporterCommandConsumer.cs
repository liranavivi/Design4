using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Exporter;

public class CreateExporterCommandConsumer : IConsumer<CreateExporterCommand>
{
    private readonly IExporterEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateExporterCommandConsumer> _logger;

    public CreateExporterCommandConsumer(
        IExporterEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateExporterCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateExporterCommand> context)
    {
        _logger.LogInformation("Processing CreateExporterCommand for {Version}_{Name}",
            context.Message.Version, context.Message.Name);

        try
        {
            var entity = new ExporterEntity
            {
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                ProtocolId = context.Message.ProtocolId,
                InputSchema = context.Message.InputSchema,
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new ExporterCreatedEvent
            {
                Id = created.Id,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                ProtocolId = created.ProtocolId,
                InputSchema = created.InputSchema,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateExporterCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateExporterCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateExporterCommand for {Version}_{Name}",
                context.Message.Version, context.Message.Name);
            throw;
        }
    }
}
