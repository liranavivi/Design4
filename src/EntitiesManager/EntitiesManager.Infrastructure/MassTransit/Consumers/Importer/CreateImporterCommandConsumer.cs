using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Importer;

public class CreateImporterCommandConsumer : IConsumer<CreateImporterCommand>
{
    private readonly IImporterEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateImporterCommandConsumer> _logger;

    public CreateImporterCommandConsumer(
        IImporterEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateImporterCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateImporterCommand> context)
    {
        _logger.LogInformation("Processing CreateImporterCommand for {Address}_{Version}",
            context.Message.Address, context.Message.Version);

        try
        {
            var entity = new ImporterEntity
            {
                Address = context.Message.Address,
                Version = context.Message.Version,
                Name = context.Message.Name,
                Description = context.Message.Description,
                Configuration = context.Message.Configuration ?? new Dictionary<string, object>(),
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new ImporterCreatedEvent
            {
                Id = created.Id,
                Address = created.Address,
                Version = created.Version,
                Name = created.Name,
                Description = created.Description,
                Configuration = created.Configuration,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed CreateImporterCommand for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in CreateImporterCommand: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateImporterCommand for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
            throw;
        }
    }
}
