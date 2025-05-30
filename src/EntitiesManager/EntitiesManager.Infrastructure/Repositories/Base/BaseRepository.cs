using EntitiesManager.Core.Entities.Base;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories.Base;
using EntitiesManager.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.Repositories.Base;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;
    protected readonly ILogger<BaseRepository<T>> _logger;
    protected readonly IEventPublisher _eventPublisher;
    private static readonly ActivitySource ActivitySource = new($"EntitiesManager.Repository.{typeof(T).Name}");

    protected BaseRepository(IMongoDatabase database, string collectionName, ILogger<BaseRepository<T>> logger, IEventPublisher eventPublisher)
    {
        _collection = database.GetCollection<T>(collectionName);
        _logger = logger;
        _eventPublisher = eventPublisher;
        CreateIndexes();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        using var activity = ActivitySource.StartActivity($"GetById{typeof(T).Name}");
        activity?.SetTag("entity.id", id.ToString());
        activity?.SetTag("entity.type", typeof(T).Name);

        try
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var result = await _collection.Find(filter).FirstOrDefaultAsync();

            activity?.SetTag("result.found", result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityType} by ID {Id}", typeof(T).Name, id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public virtual async Task<T?> GetByCompositeKeyAsync(string compositeKey)
    {
        using var activity = ActivitySource.StartActivity($"GetByCompositeKey{typeof(T).Name}");
        activity?.SetTag("entity.compositeKey", compositeKey);
        activity?.SetTag("entity.type", typeof(T).Name);

        try
        {
            var filter = CreateCompositeKeyFilter(compositeKey);
            var result = await _collection.Find(filter).FirstOrDefaultAsync();

            activity?.SetTag("result.found", result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityType} by composite key {CompositeKey}", typeof(T).Name, compositeKey);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        using var activity = ActivitySource.StartActivity($"GetAll{typeof(T).Name}");
        activity?.SetTag("entity.type", typeof(T).Name);

        try
        {
            var result = await _collection.Find(Builders<T>.Filter.Empty).ToListAsync();

            activity?.SetTag("result.count", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all {EntityType}", typeof(T).Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize)
    {
        using var activity = ActivitySource.StartActivity($"GetPaged{typeof(T).Name}");
        activity?.SetTag("entity.type", typeof(T).Name);
        activity?.SetTag("page", page);
        activity?.SetTag("pageSize", pageSize);

        try
        {
            var skip = (page - 1) * pageSize;
            var result = await _collection
                .Find(Builders<T>.Filter.Empty)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            activity?.SetTag("result.count", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged {EntityType} (page: {Page}, size: {PageSize})", typeof(T).Name, page, pageSize);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        using var activity = ActivitySource.StartActivity($"Create{typeof(T).Name}");
        activity?.SetTag("entity.type", typeof(T).Name);
        activity?.SetTag("entity.compositeKey", entity.GetCompositeKey());

        try
        {
            // Set timestamps - MongoDB will auto-generate the ID
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // Validate composite key uniqueness BEFORE insertion
            if (await ExistsAsync(entity.GetCompositeKey()))
            {
                throw new DuplicateKeyException($"{typeof(T).Name} with composite key '{entity.GetCompositeKey()}' already exists");
            }

            // MongoDB will auto-generate the GUID ID during insertion
            await _collection.InsertOneAsync(entity);

            activity?.SetTag("entity.id", entity.Id.ToString());
            _logger.LogInformation("Created {EntityType} with auto-generated ID {Id} and composite key {CompositeKey}",
                typeof(T).Name, entity.Id, entity.GetCompositeKey());

            // Publish created event
            await PublishCreatedEventAsync(entity);

            return entity;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogWarning("Duplicate key error creating {EntityType}: {Error}", typeof(T).Name, ex.WriteError.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.WriteError.Message);
            throw new DuplicateKeyException($"Duplicate key error: {ex.WriteError.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {EntityType}", typeof(T).Name);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        using var activity = ActivitySource.StartActivity($"Update{typeof(T).Name}");
        activity?.SetTag("entity.type", typeof(T).Name);
        activity?.SetTag("entity.id", entity.Id.ToString());
        activity?.SetTag("entity.compositeKey", entity.GetCompositeKey());

        try
        {
            // Validate that entity has an ID (not new)
            if (entity.IsNew)
            {
                throw new InvalidOperationException($"Cannot update {typeof(T).Name} with empty ID. Use CreateAsync for new entities.");
            }

            entity.UpdatedAt = DateTime.UtcNow;

            // Check if we're changing the composite key and if the new key already exists
            var existing = await GetByIdAsync(entity.Id);
            if (existing == null)
            {
                throw new EntityNotFoundException($"{typeof(T).Name} with ID {entity.Id} not found");
            }

            // If composite key is changing, validate uniqueness
            if (existing.GetCompositeKey() != entity.GetCompositeKey())
            {
                if (await ExistsAsync(entity.GetCompositeKey()))
                {
                    throw new DuplicateKeyException($"{typeof(T).Name} with composite key '{entity.GetCompositeKey()}' already exists");
                }
            }

            var filter = Builders<T>.Filter.Eq(x => x.Id, entity.Id);
            var result = await _collection.ReplaceOneAsync(filter, entity);

            if (result.MatchedCount == 0)
            {
                throw new EntityNotFoundException($"{typeof(T).Name} with ID {entity.Id} not found");
            }

            _logger.LogInformation("Updated {EntityType} with ID {Id}", typeof(T).Name, entity.Id);

            // Publish updated event
            await PublishUpdatedEventAsync(entity);

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        using var activity = ActivitySource.StartActivity($"Delete{typeof(T).Name}");
        activity?.SetTag("entity.type", typeof(T).Name);
        activity?.SetTag("entity.id", id.ToString());

        try
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var result = await _collection.DeleteOneAsync(filter);

            var deleted = result.DeletedCount > 0;
            activity?.SetTag("result.deleted", deleted);
            _logger.LogInformation("Deleted {EntityType} with ID {Id}: {Success}", typeof(T).Name, id, deleted);

            // Publish deleted event if entity was actually deleted
            if (deleted)
            {
                await PublishDeletedEventAsync(id, "System"); // TODO: Get actual user context
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityType} with ID {Id}", typeof(T).Name, id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(string compositeKey)
    {
        try
        {
            var filter = CreateCompositeKeyFilter(compositeKey);
            var count = await _collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of {EntityType} with composite key {CompositeKey}", typeof(T).Name, compositeKey);
            throw;
        }
    }

    public virtual async Task<bool> ExistsByIdAsync(Guid id)
    {
        try
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var count = await _collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<long> CountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(Builders<T>.Filter.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting {EntityType}", typeof(T).Name);
            throw;
        }
    }

    protected abstract FilterDefinition<T> CreateCompositeKeyFilter(string compositeKey);
    protected abstract void CreateIndexes();

    // Abstract methods for event publishing
    protected abstract Task PublishCreatedEventAsync(T entity);
    protected abstract Task PublishUpdatedEventAsync(T entity);
    protected abstract Task PublishDeletedEventAsync(Guid id, string deletedBy);
}
