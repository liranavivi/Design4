using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MongoDB;
using EntitiesManager.Infrastructure.Repositories;
using EntitiesManager.Infrastructure.Services;
using MongoDB.Driver;

namespace EntitiesManager.Api.Configuration;

public static class MongoDbConfiguration
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure BSON serialization
        BsonConfiguration.Configure();

        // Register MongoDB client and database
        services.AddSingleton<IMongoClient>(provider =>
        {
            var connectionString = configuration.GetConnectionString("MongoDB");
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            return new MongoClient(settings);
        });

        services.AddScoped<IMongoDatabase>(provider =>
        {
            var client = provider.GetRequiredService<IMongoClient>();
            var databaseName = configuration.GetValue<string>("MongoDB:DatabaseName") ?? "EntitiesManagerDb";
            return client.GetDatabase(databaseName);
        });

        // Register event publisher
        services.AddScoped<IEventPublisher, EventPublisher>();

        // Register referential integrity service
        services.AddScoped<IReferentialIntegrityService, ReferentialIntegrityService>();

        // Register repositories
        services.AddScoped<ISourceEntityRepository, SourceEntityRepository>();
        services.AddScoped<IStepEntityRepository, StepEntityRepository>();
        services.AddScoped<IDestinationEntityRepository, DestinationEntityRepository>();
        services.AddScoped<IProtocolEntityRepository>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            var logger = provider.GetRequiredService<ILogger<ProtocolEntityRepository>>();
            var eventPublisher = provider.GetRequiredService<IEventPublisher>();
            var integrityService = provider.GetRequiredService<IReferentialIntegrityService>();
            return new ProtocolEntityRepository(database, logger, eventPublisher, integrityService);
        });
        services.AddScoped<IImporterEntityRepository, ImporterEntityRepository>();
        services.AddScoped<IExporterEntityRepository, ExporterEntityRepository>();
        services.AddScoped<IProcessorEntityRepository, ProcessorEntityRepository>();
        services.AddScoped<IFlowEntityRepository, FlowEntityRepository>();
        services.AddScoped<ITaskScheduledEntityRepository, TaskScheduledEntityRepository>();
        services.AddScoped<IScheduledFlowEntityRepository, ScheduledFlowEntityRepository>();

        return services;
    }
}
