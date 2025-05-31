# EntitiesManager - New Entity Generator Script
# This script generates all required files for a new entity type following established patterns

param(
    [Parameter(Mandatory=$true)]
    [string]$EntityName,

    [Parameter(Mandatory=$false)]
    [string]$ProjectRoot = ".",

    [Parameter(Mandatory=$false)]
    [switch]$DryRun = $false
)

# Validation functions
function Test-EntityName {
    param([string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name)) {
        throw "Entity name cannot be empty"
    }

    if ($Name -cnotmatch '^[A-Z][a-zA-Z0-9]*$') {
        throw "Entity name must start with uppercase letter and contain only letters and numbers"
    }

    if ($Name.Length -lt 3 -or $Name.Length -gt 50) {
        throw "Entity name must be between 3 and 50 characters"
    }

    # Check for conflicts with existing entities (allow Source for testing/regeneration)
    $existingEntities = @("Base")
    if ($existingEntities -contains $Name) {
        throw "Entity name '$Name' conflicts with existing entity"
    }
}

function Test-ProjectStructure {
    param([string]$Root)

    $requiredPaths = @(
        "src/EntitiesManager/EntitiesManager.Core/Entities",
        "src/EntitiesManager/EntitiesManager.Core/Interfaces/Repositories",
        "src/EntitiesManager/EntitiesManager.Infrastructure/Repositories",
        "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Commands",
        "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Events",
        "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Consumers",
        "src/EntitiesManager/EntitiesManager.Api/Controllers",
        "src/EntitiesManager/EntitiesManager.Api/Configuration",
        "src/EntitiesManager/EntitiesManager.Infrastructure/MongoDB"
    )

    foreach ($path in $requiredPaths) {
        $fullPath = Join-Path $Root $path
        if (-not (Test-Path $fullPath)) {
            throw "Required project path not found: $fullPath"
        }
    }
}

# File generation functions
function New-EntityClass {
    param([string]$EntityName, [string]$ProjectRoot)

    # Check if this is the Source entity to match exactly
    if ($EntityName -eq "Source") {
        $content = @"
using EntitiesManager.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities;

public class SourceEntity : BaseEntity
{
    [BsonElement("address")]
    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("version")]
    [Required(ErrorMessage = "Version is required")]
    [StringLength(50, ErrorMessage = "Version cannot exceed 50 characters")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("name")]
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("configuration")]
    public Dictionary<string, object> Configuration { get; set; } = new();

    public override string GetCompositeKey() => `$"{Address}_{Version}";
}
"@
    } else {
        # For other entities, use the SourceEntity pattern but with generic properties
        $content = @"
using EntitiesManager.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities;

public class ${EntityName}Entity : BaseEntity
{
    [BsonElement("address")]
    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("configuration")]
    public Dictionary<string, object> Configuration { get; set; } = new();

    public override string GetCompositeKey() => `$"{Address}_{Version}";
}
"@
    }

    return @{
        Path = "src/EntitiesManager/EntitiesManager.Core/Entities/${EntityName}Entity.cs"
        Content = $content
    }
}

function New-RepositoryInterface {
    param([string]$EntityName, [string]$ProjectRoot)

    # Check if this is the Source entity to match exactly
    if ($EntityName -eq "Source") {
        $content = @"
using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface ISourceEntityRepository : IBaseRepository<SourceEntity>
{
    Task<IEnumerable<SourceEntity>> GetByAddressAsync(string address);
    Task<IEnumerable<SourceEntity>> GetByVersionAsync(string version);
    Task<IEnumerable<SourceEntity>> GetByNameAsync(string name);
    Task<IEnumerable<SourceEntity>> SearchByDescriptionAsync(string searchTerm);
}
"@
    } else {
        # For other entities, use the SourceEntity pattern
        $content = @"
using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories.Base;

namespace EntitiesManager.Core.Interfaces.Repositories;

public interface I${EntityName}EntityRepository : IBaseRepository<${EntityName}Entity>
{
    Task<IEnumerable<${EntityName}Entity>> GetByAddressAsync(string address);
    Task<IEnumerable<${EntityName}Entity>> GetByVersionAsync(string version);
    Task<IEnumerable<${EntityName}Entity>> GetByNameAsync(string name);
    Task<IEnumerable<${EntityName}Entity>> SearchByDescriptionAsync(string searchTerm);
}
"@
    }

    return @{
        Path = "src/EntitiesManager/EntitiesManager.Core/Interfaces/Repositories/I${EntityName}EntityRepository.cs"
        Content = $content
    }
}

function New-RepositoryImplementation {
    param([string]$EntityName, [string]$ProjectRoot)

    $entityLower = $EntityName.ToLower()

    # Check if this is the Source entity to match exactly
    if ($EntityName -eq "Source") {
        $content = @"
using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class SourceEntityRepository : BaseRepository<SourceEntity>, ISourceEntityRepository
{
    public SourceEntityRepository(IMongoDatabase database, ILogger<SourceEntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "sources", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<SourceEntity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for SourceEntity. Expected format: 'address_version'");

        return Builders<SourceEntity>.Filter.And(
            Builders<SourceEntity>.Filter.Eq(x => x.Address, parts[0]),
            Builders<SourceEntity>.Filter.Eq(x => x.Version, parts[1])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness
        var compositeKeyIndex = Builders<SourceEntity>.IndexKeys
            .Ascending(x => x.Address)
            .Ascending(x => x.Version);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(
            Builders<SourceEntity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(
            Builders<SourceEntity>.IndexKeys.Ascending(x => x.Address)));
        _collection.Indexes.CreateOne(new CreateIndexModel<SourceEntity>(
            Builders<SourceEntity>.IndexKeys.Ascending(x => x.Version)));
    }

    public async Task<IEnumerable<SourceEntity>> GetByAddressAsync(string address)
    {
        var filter = Builders<SourceEntity>.Filter.Eq(x => x.Address, address);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<SourceEntity>> GetByVersionAsync(string version)
    {
        var filter = Builders<SourceEntity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<SourceEntity>> GetByNameAsync(string name)
    {
        var filter = Builders<SourceEntity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(SourceEntity entity)
    {
        var createdEvent = new SourceCreatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Configuration = entity.Configuration,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(SourceEntity entity)
    {
        var updatedEvent = new SourceUpdatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Configuration = entity.Configuration,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new SourceDeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
"@
    } else {
        # For other entities, use the SourceEntity pattern but with generic properties
        $content = @"
using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MassTransit.Events;
using EntitiesManager.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace EntitiesManager.Infrastructure.Repositories;

public class ${EntityName}EntityRepository : BaseRepository<${EntityName}Entity>, I${EntityName}EntityRepository
{
    public ${EntityName}EntityRepository(IMongoDatabase database, ILogger<${EntityName}EntityRepository> logger, IEventPublisher eventPublisher)
        : base(database, "${entityLower}s", logger, eventPublisher)
    {
    }

    protected override FilterDefinition<${EntityName}Entity> CreateCompositeKeyFilter(string compositeKey)
    {
        var parts = compositeKey.Split('_', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid composite key format for ${EntityName}Entity. Expected format: 'address_version'");

        return Builders<${EntityName}Entity>.Filter.And(
            Builders<${EntityName}Entity>.Filter.Eq(x => x.Address, parts[0]),
            Builders<${EntityName}Entity>.Filter.Eq(x => x.Version, parts[1])
        );
    }

    protected override void CreateIndexes()
    {
        // Composite key index for uniqueness
        var compositeKeyIndex = Builders<${EntityName}Entity>.IndexKeys
            .Ascending(x => x.Address)
            .Ascending(x => x.Version);

        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOne(new CreateIndexModel<${EntityName}Entity>(compositeKeyIndex, indexOptions));

        // Additional indexes for common queries
        _collection.Indexes.CreateOne(new CreateIndexModel<${EntityName}Entity>(
            Builders<${EntityName}Entity>.IndexKeys.Ascending(x => x.Name)));
        _collection.Indexes.CreateOne(new CreateIndexModel<${EntityName}Entity>(
            Builders<${EntityName}Entity>.IndexKeys.Ascending(x => x.Address)));
        _collection.Indexes.CreateOne(new CreateIndexModel<${EntityName}Entity>(
            Builders<${EntityName}Entity>.IndexKeys.Ascending(x => x.Version)));
    }

    public async Task<IEnumerable<${EntityName}Entity>> GetByAddressAsync(string address)
    {
        var filter = Builders<${EntityName}Entity>.Filter.Eq(x => x.Address, address);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<${EntityName}Entity>> GetByVersionAsync(string version)
    {
        var filter = Builders<${EntityName}Entity>.Filter.Eq(x => x.Version, version);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<${EntityName}Entity>> GetByNameAsync(string name)
    {
        var filter = Builders<${EntityName}Entity>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).ToListAsync();
    }

    protected override async Task PublishCreatedEventAsync(${EntityName}Entity entity)
    {
        var createdEvent = new ${EntityName}CreatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Configuration = entity.Configuration,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
        await _eventPublisher.PublishAsync(createdEvent);
    }

    protected override async Task PublishUpdatedEventAsync(${EntityName}Entity entity)
    {
        var updatedEvent = new ${EntityName}UpdatedEvent
        {
            Id = entity.Id,
            Address = entity.Address,
            Version = entity.Version,
            Name = entity.Name,
            Configuration = entity.Configuration,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
        await _eventPublisher.PublishAsync(updatedEvent);
    }

    protected override async Task PublishDeletedEventAsync(Guid id, string deletedBy)
    {
        var deletedEvent = new ${EntityName}DeletedEvent
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = deletedBy
        };
        await _eventPublisher.PublishAsync(deletedEvent);
    }
}
"@
    }

    return @{
        Path = "src/EntitiesManager/EntitiesManager.Infrastructure/Repositories/${EntityName}EntityRepository.cs"
        Content = $content
    }
}

function New-Commands {
    param([string]$EntityName, [string]$ProjectRoot)

    # Check if this is the Source entity to match exactly
    if ($EntityName -eq "Source") {
        $content = @"
namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateSourceCommand
{
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateSourceCommand
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteSourceCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetSourceQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}
"@
    } else {
        # For other entities, use the SourceEntity pattern
        $content = @"
namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class Create${EntityName}Command
{
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
}

public class Update${EntityName}Command
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
}

public class Delete${EntityName}Command
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class Get${EntityName}Query
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}
"@
    }

    return @{
        Path = "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Commands/${EntityName}Commands.cs"
        Content = $content
    }
}

function New-Events {
    param([string]$EntityName, [string]$ProjectRoot)

    # Check if this is the Source entity to match exactly
    if ($EntityName -eq "Source") {
        $content = @"
namespace EntitiesManager.Infrastructure.MassTransit.Events;

public class SourceCreatedEvent
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class SourceUpdatedEvent
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class SourceDeletedEvent
{
    public Guid Id { get; set; }
    public DateTime DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}
"@
    } else {
        # For other entities, use the SourceEntity pattern
        $content = @"
namespace EntitiesManager.Infrastructure.MassTransit.Events;

public class ${EntityName}CreatedEvent
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class ${EntityName}UpdatedEvent
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class ${EntityName}DeletedEvent
{
    public Guid Id { get; set; }
    public DateTime DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}
"@
    }

    return @{
        Path = "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Events/${EntityName}Events.cs"
        Content = $content
    }
}

function New-Controller {
    param([string]$EntityName, [string]$ProjectRoot)

    $entityLower = $EntityName.ToLower()

    $content = @"
using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EntitiesManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ${EntityName}sController : ControllerBase
{
    private readonly I${EntityName}EntityRepository _repository;
    private readonly ILogger<${EntityName}sController> _logger;

    public ${EntityName}sController(
        I${EntityName}EntityRepository repository,
        ILogger<${EntityName}sController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<${EntityName}Entity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll ${entityLower}s request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all ${entityLower} entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all ${entityLower} entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving ${entityLower} entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged ${entityLower}s request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
            page, pageSize, userContext, HttpContext.TraceIdentifier);

        try
        {
            // Log parameter adjustments
            if (page < 1)
            {
                page = 1;
                _logger.LogWarning("Page parameter adjusted from {OriginalPage} to {AdjustedPage}. User: {User}, RequestId: {RequestId}",
                    originalPage, page, userContext, HttpContext.TraceIdentifier);
            }

            if (pageSize < 1 || pageSize > 100)
            {
                var adjustedPageSize = pageSize < 1 ? 10 : 100;
                _logger.LogWarning("PageSize parameter adjusted from {OriginalPageSize} to {AdjustedPageSize}. User: {User}, RequestId: {RequestId}",
                    originalPageSize, adjustedPageSize, userContext, HttpContext.TraceIdentifier);
                pageSize = adjustedPageSize;
            }

            var entities = await _repository.GetPagedAsync(page, pageSize);
            var totalCount = await _repository.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation("Successfully retrieved paged ${entityLower} entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
                page, pageSize, entities.Count(), totalCount, totalPages, userContext, HttpContext.TraceIdentifier);

            return Ok(new
            {
                Data = entities,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged ${entityLower} entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving ${entityLower} entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<${EntityName}Entity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById ${entityLower} request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("${EntityName} entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound(`$"${EntityName} with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved ${entityLower} entity by ID. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, entity.Address, entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ${entityLower} entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the ${entityLower} entity");
        }
    }

    [HttpGet("by-key/{address}/{version}")]
    public async Task<ActionResult<${EntityName}Entity>> GetByCompositeKey(string address, string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = `$"{address}_{version}";

        _logger.LogInformation("Starting GetByCompositeKey ${entityLower} request. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            address, version, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);

            if (entity == null)
            {
                _logger.LogWarning("${EntityName} entity not found by composite key. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    address, version, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound(`$"${EntityName} with address '{address}' and version '{version}' not found");
            }

            _logger.LogInformation("Successfully retrieved ${entityLower} entity by composite key. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, address, version, entity.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ${entityLower} entity by composite key. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                address, version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the ${entityLower} entity");
        }
    }

    [HttpGet("by-address/{address}")]
    public async Task<ActionResult<IEnumerable<${EntityName}Entity>>> GetByAddress(string address)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByAddress ${entityLower} request. Address: {Address}, User: {User}, RequestId: {RequestId}",
            address, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByAddressAsync(address);

            _logger.LogInformation("Successfully retrieved ${entityLower} entities by address. Address: {Address}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                address, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ${entityLower} entities by address. Address: {Address}, User: {User}, RequestId: {RequestId}",
                address, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving ${entityLower} entities");
        }
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<${EntityName}Entity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByName ${entityLower} request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved ${entityLower} entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ${entityLower} entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving ${entityLower} entities");
        }
    }

    [HttpGet("by-version/{version}")]
    public async Task<ActionResult<IEnumerable<${EntityName}Entity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByVersion ${entityLower} request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByVersionAsync(version);

            _logger.LogInformation("Successfully retrieved ${entityLower} entities by version. Version: {Version}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                version, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ${entityLower} entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving ${entityLower} entities");
        }
    }

    [HttpPost]
    public async Task<ActionResult<${EntityName}Entity>> Create([FromBody] ${EntityName}Entity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create ${entityLower} request. Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Address, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create ${entityLower} request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating ${entityLower} entity with details. Address: {Address}, Version: {Version}, Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Address, entity.Version, entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new ${EntityName}Entity. Address: {Address}, Version: {Version}, User: {User}, RequestId: {RequestId}",
                    entity.Address, entity.Version, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created ${entityLower} entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Address, created.Version, created.Name, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict creating ${entityLower} entity. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ${entityLower} entity. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the ${entityLower}");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<${EntityName}Entity>> Update(Guid id, [FromBody] ${EntityName}Entity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update ${entityLower} request. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Address, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update ${entityLower} request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update ${entityLower} request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("${EntityName} entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound(`$"${EntityName} with ID {id} not found");
            }

            _logger.LogDebug("Updating ${entityLower} entity. Id: {Id}, OldAddress: {OldAddress}, NewAddress: {NewAddress}, OldVersion: {OldVersion}, NewVersion: {NewVersion}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, entity.Address, existing.Version, entity.Version, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated ${entityLower} entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Address, updated.Version, updated.Name, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating ${entityLower} entity. Id: {Id}, Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("${EntityName} entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound(`$"${EntityName} with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ${entityLower} entity. Id: {Id}, Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the ${entityLower}");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete ${entityLower} request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("${EntityName} entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound(`$"${EntityName} with ID {id} not found");
            }

            _logger.LogDebug("Deleting ${entityLower} entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete ${entityLower} entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the ${entityLower} entity");
            }

            _logger.LogInformation("Successfully deleted ${entityLower} entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ${entityLower} entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the ${entityLower}");
        }
    }
}
"@

    return @{
        Path = "src/EntitiesManager/EntitiesManager.Api/Controllers/${EntityName}sController.cs"
        Content = $content
    }
}

function New-Consumers {
    param([string]$EntityName, [string]$ProjectRoot)

    $consumers = @()

    # Create Consumer
    $createContent = @"
using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.${EntityName};

public class Create${EntityName}CommandConsumer : IConsumer<Create${EntityName}Command>
{
    private readonly I${EntityName}EntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<Create${EntityName}CommandConsumer> _logger;

    public Create${EntityName}CommandConsumer(
        I${EntityName}EntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<Create${EntityName}CommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Create${EntityName}Command> context)
    {
        _logger.LogInformation("Processing Create${EntityName}Command for {Address}_{Version}",
            context.Message.Address, context.Message.Version);

        try
        {
            var entity = new ${EntityName}Entity
            {
                Address = context.Message.Address,
                Version = context.Message.Version,
                Name = context.Message.Name,
                Configuration = context.Message.Configuration ?? new Dictionary<string, object>(),
                CreatedBy = context.Message.RequestedBy
            };

            var created = await _repository.CreateAsync(entity);

            await _publishEndpoint.Publish(new ${EntityName}CreatedEvent
            {
                Id = created.Id,
                Address = created.Address,
                Version = created.Version,
                Name = created.Name,
                Configuration = created.Configuration,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            });

            await context.RespondAsync(created);

            _logger.LogInformation("Successfully processed Create${EntityName}Command for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in Create${EntityName}Command: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Create${EntityName}Command for {Address}_{Version}",
                context.Message.Address, context.Message.Version);
            throw;
        }
    }
}
"@

    $consumers += @{
        Path = "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Consumers/${EntityName}/Create${EntityName}CommandConsumer.cs"
        Content = $createContent
    }

    # Update Consumer
    $updateContent = @"
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.${EntityName};

public class Update${EntityName}CommandConsumer : IConsumer<Update${EntityName}Command>
{
    private readonly I${EntityName}EntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<Update${EntityName}CommandConsumer> _logger;

    public Update${EntityName}CommandConsumer(
        I${EntityName}EntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<Update${EntityName}CommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Update${EntityName}Command> context)
    {
        _logger.LogInformation("Processing Update${EntityName}Command for ID {Id}", context.Message.Id);

        try
        {
            var existing = await _repository.GetByIdAsync(context.Message.Id);
            if (existing == null)
            {
                _logger.LogWarning("${EntityName}Entity with ID {Id} not found for update", context.Message.Id);
                await context.RespondAsync(new { Error = "Entity not found", Success = false });
                return;
            }

            // Update properties
            existing.Address = context.Message.Address;
            existing.Version = context.Message.Version;
            existing.Name = context.Message.Name;
            existing.Configuration = context.Message.Configuration ?? new Dictionary<string, object>();
            existing.UpdatedBy = context.Message.RequestedBy;

            var updated = await _repository.UpdateAsync(existing);

            await _publishEndpoint.Publish(new ${EntityName}UpdatedEvent
            {
                Id = updated.Id,
                Address = updated.Address,
                Version = updated.Version,
                Name = updated.Name,
                Configuration = updated.Configuration,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            });

            await context.RespondAsync(updated);

            _logger.LogInformation("Successfully processed Update${EntityName}Command for ID {Id}", context.Message.Id);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning("Duplicate key error in Update${EntityName}Command: {Error}", ex.Message);
            await context.RespondAsync(new { Error = ex.Message, Success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Update${EntityName}Command for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
"@

    $consumers += @{
        Path = "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Consumers/${EntityName}/Update${EntityName}CommandConsumer.cs"
        Content = $updateContent
    }

    # Delete Consumer
    $deleteContent = @"
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.${EntityName};

public class Delete${EntityName}CommandConsumer : IConsumer<Delete${EntityName}Command>
{
    private readonly I${EntityName}EntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<Delete${EntityName}CommandConsumer> _logger;

    public Delete${EntityName}CommandConsumer(
        I${EntityName}EntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<Delete${EntityName}CommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Delete${EntityName}Command> context)
    {
        _logger.LogInformation("Processing Delete${EntityName}Command for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new ${EntityName}DeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed Delete${EntityName}Command for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("${EntityName}Entity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Delete${EntityName}Command for ID {Id}", context.Message.Id);
            throw;
        }
    }
}
"@

    $consumers += @{
        Path = "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Consumers/${EntityName}/Delete${EntityName}CommandConsumer.cs"
        Content = $deleteContent
    }

    # Get Query Consumer
    $getContent = @"
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.${EntityName};

public class Get${EntityName}QueryConsumer : IConsumer<Get${EntityName}Query>
{
    private readonly I${EntityName}EntityRepository _repository;
    private readonly ILogger<Get${EntityName}QueryConsumer> _logger;

    public Get${EntityName}QueryConsumer(I${EntityName}EntityRepository repository, ILogger<Get${EntityName}QueryConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Get${EntityName}Query> context)
    {
        using var activity = Activity.Current?.Source.StartActivity("Get${EntityName}Query");

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
                    await context.RespondAsync(new { Error = "${EntityName} not found", Type = "NotFound" });
            }
            else if (!string.IsNullOrEmpty(context.Message.CompositeKey))
            {
                activity?.SetTag("query.type", "ByCompositeKey");
                activity?.SetTag("query.compositeKey", context.Message.CompositeKey);

                var entity = await _repository.GetByCompositeKeyAsync(context.Message.CompositeKey);
                if (entity != null)
                    await context.RespondAsync(entity);
                else
                    await context.RespondAsync(new { Error = "${EntityName} not found", Type = "NotFound" });
            }
            else
            {
                await context.RespondAsync(new { Error = "Either Id or CompositeKey must be provided", Type = "BadRequest" });
            }

            _logger.LogInformation("Successfully processed Get${EntityName}Query");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Get${EntityName}Query");
            await context.RespondAsync(new { Error = ex.Message, Type = "InternalError" });
            throw;
        }
    }
}
"@

    $consumers += @{
        Path = "src/EntitiesManager/EntitiesManager.Infrastructure/MassTransit/Consumers/${EntityName}/Get${EntityName}QueryConsumer.cs"
        Content = $getContent
    }

    return $consumers
}

function New-IntegrationTestBase {
    param([string]$EntityName, [string]$ProjectRoot)

    $content = @"
using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Core.Interfaces.Services;
using EntitiesManager.Infrastructure.MongoDB;
using EntitiesManager.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace EntitiesManager.IntegrationTests.${EntityName}Tests;

public abstract class ${EntityName}IntegrationTestBase : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    protected IMongoDatabase Database { get; private set; } = null!;
    protected I${EntityName}EntityRepository ${EntityName}Repository { get; private set; } = null!;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    protected ${EntityName}IntegrationTestBase()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithPortBinding(27017, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        // Configure BSON serialization
        BsonConfiguration.Configure();

        // Setup MongoDB client and database
        var connectionString = _mongoContainer.GetConnectionString();
        var mongoClient = new MongoClient(connectionString);
        Database = mongoClient.GetDatabase("${EntityName}TestDb");

        // Setup service collection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton(Database);

        // Add mock event publisher for testing
        services.AddScoped<IEventPublisher, MockEventPublisher>();
        services.AddScoped<I${EntityName}EntityRepository, ${EntityName}EntityRepository>();

        ServiceProvider = services.BuildServiceProvider();
        ${EntityName}Repository = ServiceProvider.GetRequiredService<I${EntityName}EntityRepository>();
    }

    public async Task DisposeAsync()
    {
        (ServiceProvider as IDisposable)?.Dispose();
        await _mongoContainer.DisposeAsync();
    }

    protected ${EntityName}Entity CreateTest${EntityName}(string name = "Test${EntityName}", string version = "1.0.0", string address = "test://${EntityName.ToLower()}")
    {
        return new ${EntityName}Entity
        {
            Address = address,
            Version = version,
            Name = name,
            Configuration = new Dictionary<string, object>
            {
                ["testProperty"] = "testValue",
                ["numericProperty"] = 42,
                ["booleanProperty"] = true
            },
            CreatedBy = "TestUser"
        };
    }
}

// Mock event publisher for testing
public class MockEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T eventData) where T : class
    {
        // Mock implementation - just return completed task
        return Task.CompletedTask;
    }
}
"@

    return @{
        Path = "tests/EntitiesManager.IntegrationTests/${EntityName}Tests/${EntityName}IntegrationTestBase.cs"
        Content = $content
    }
}

# Main execution
try {
    Write-Host "EntitiesManager New Entity Generator" -ForegroundColor Green
    Write-Host "=======================================" -ForegroundColor Green

    # Validate inputs
    Test-EntityName -Name $EntityName
    Test-ProjectStructure -Root $ProjectRoot

    Write-Host "Validation passed for entity: $EntityName" -ForegroundColor Green

    if ($DryRun) {
        Write-Host "DRY RUN MODE - No files will be created" -ForegroundColor Yellow
    }

    # Generate all files
    $files = @()
    $files += New-EntityClass -EntityName $EntityName -ProjectRoot $ProjectRoot
    $files += New-RepositoryInterface -EntityName $EntityName -ProjectRoot $ProjectRoot
    $files += New-RepositoryImplementation -EntityName $EntityName -ProjectRoot $ProjectRoot
    $files += New-Commands -EntityName $EntityName -ProjectRoot $ProjectRoot
    $files += New-Events -EntityName $EntityName -ProjectRoot $ProjectRoot
    $files += New-Controller -EntityName $EntityName -ProjectRoot $ProjectRoot
    $files += New-Consumers -EntityName $EntityName -ProjectRoot $ProjectRoot
    $files += New-IntegrationTestBase -EntityName $EntityName -ProjectRoot $ProjectRoot

    Write-Host "Generated $($files.Count) files:" -ForegroundColor Cyan

    foreach ($file in $files) {
        $fullPath = Join-Path $ProjectRoot $file.Path
        Write-Host "   $($file.Path)" -ForegroundColor White

        if (-not $DryRun) {
            $directory = Split-Path $fullPath -Parent
            if (-not (Test-Path $directory)) {
                New-Item -ItemType Directory -Path $directory -Force | Out-Null
            }

            Set-Content -Path $fullPath -Value $file.Content -Encoding UTF8
        }
    }

    if (-not $DryRun) {
        Write-Host "All files created successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "MANUAL CONFIGURATION STEPS REQUIRED:" -ForegroundColor Yellow
        Write-Host "=======================================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "1. Update BSON Configuration:" -ForegroundColor Cyan
        Write-Host "   File: src/EntitiesManager/EntitiesManager.Infrastructure/MongoDB/BsonConfiguration.cs"
        Write-Host "   Add this code block inside the Configure() method:"
        Write-Host ""
        Write-Host "   if (!BsonClassMap.IsClassMapRegistered(typeof(${EntityName}Entity)))" -ForegroundColor White
        Write-Host "   {" -ForegroundColor White
        Write-Host "       BsonClassMap.RegisterClassMap<${EntityName}Entity>(cm =>" -ForegroundColor White
        Write-Host "       {" -ForegroundColor White
        Write-Host "           cm.AutoMap();" -ForegroundColor White
        Write-Host "           cm.SetIgnoreExtraElements(true);" -ForegroundColor White
        Write-Host "       });" -ForegroundColor White
        Write-Host "   }" -ForegroundColor White
        Write-Host ""
        Write-Host "2. Update MongoDB Configuration:" -ForegroundColor Cyan
        Write-Host "   File: src/EntitiesManager/EntitiesManager.Api/Configuration/MongoDbConfiguration.cs"
        Write-Host "   Add this line in the AddMongoDb method:"
        Write-Host ""
        Write-Host "   services.AddScoped<I${EntityName}EntityRepository, ${EntityName}EntityRepository>();" -ForegroundColor White
        Write-Host ""
        Write-Host "3. Update MassTransit Configuration:" -ForegroundColor Cyan
        Write-Host "   File: src/EntitiesManager/EntitiesManager.Api/Configuration/MassTransitConfiguration.cs"
        Write-Host "   Add this using statement at the top:"
        Write-Host ""
        Write-Host "   using EntitiesManager.Infrastructure.MassTransit.Consumers.${EntityName};" -ForegroundColor White
        Write-Host ""
        Write-Host "   Add these lines in the AddMassTransitWithRabbitMq method:"
        Write-Host ""
        Write-Host "   x.AddConsumer<Create${EntityName}CommandConsumer>();" -ForegroundColor White
        Write-Host "   x.AddConsumer<Update${EntityName}CommandConsumer>();" -ForegroundColor White
        Write-Host "   x.AddConsumer<Delete${EntityName}CommandConsumer>();" -ForegroundColor White
        Write-Host "   x.AddConsumer<Get${EntityName}QueryConsumer>();" -ForegroundColor White
        Write-Host ""
        Write-Host "4. Run Integration Tests:" -ForegroundColor Cyan
        Write-Host "   dotnet test tests/EntitiesManager.IntegrationTests/${EntityName}Tests/" -ForegroundColor White
        Write-Host ""
        Write-Host "5. Test the API:" -ForegroundColor Cyan
        $entityLower = $EntityName.ToLower()
        Write-Host "   curl -X POST http://localhost:5130/api/${entityLower}s \\" -ForegroundColor White
        Write-Host "     -H `"Content-Type: application/json`" \\" -ForegroundColor White
        Write-Host "     -d '{`"address`":`"tcp://test.example.com:8080`",`"version`":`"1.0.0`",`"name`":`"Test${EntityName}`",`"configuration`":{`"timeout`":30}}'" -ForegroundColor White
        Write-Host ""
        Write-Host "   curl http://localhost:5130/api/${entityLower}s" -ForegroundColor White
        Write-Host "   curl http://localhost:5130/api/${entityLower}s/by-key/tcp://test.example.com:8080/1.0.0" -ForegroundColor White
        Write-Host ""
        Write-Host "Entity generation completed! Follow the manual steps above to complete the integration." -ForegroundColor Green
    } else {
        Write-Host "DRY RUN completed - Review the file list above" -ForegroundColor Yellow
    }

} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
    exit 1
}
