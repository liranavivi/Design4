using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EntitiesManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskScheduledsController : ControllerBase
{
    private readonly ITaskScheduledEntityRepository _repository;
    private readonly ILogger<TaskScheduledsController> _logger;

    public TaskScheduledsController(
        ITaskScheduledEntityRepository repository,
        ILogger<TaskScheduledsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskScheduledEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll taskscheduleds request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all taskscheduled entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all taskscheduled entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving taskscheduled entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged taskscheduleds request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved paged taskscheduled entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogError(ex, "Error retrieving paged taskscheduled entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving taskscheduled entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskScheduledEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById taskscheduled request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("TaskScheduled entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"TaskScheduled with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved taskscheduled entity by ID. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, entity.Address, entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving taskscheduled entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the taskscheduled entity");
        }
    }

    [HttpGet("by-key/{address}/{version}")]
    public async Task<ActionResult<TaskScheduledEntity>> GetByCompositeKey(string address, string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = $"{address}_{version}";

        _logger.LogInformation("Starting GetByCompositeKey taskscheduled request. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            address, version, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);

            if (entity == null)
            {
                _logger.LogWarning("TaskScheduled entity not found by composite key. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    address, version, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"TaskScheduled with address '{address}' and version '{version}' not found");
            }

            _logger.LogInformation("Successfully retrieved taskscheduled entity by composite key. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, address, version, entity.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving taskscheduled entity by composite key. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                address, version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the taskscheduled entity");
        }
    }

    [HttpGet("by-address/{address}")]
    public async Task<ActionResult<IEnumerable<TaskScheduledEntity>>> GetByAddress(string address)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByAddress taskscheduled request. Address: {Address}, User: {User}, RequestId: {RequestId}",
            address, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByAddressAsync(address);

            _logger.LogInformation("Successfully retrieved taskscheduled entities by address. Address: {Address}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                address, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving taskscheduled entities by address. Address: {Address}, User: {User}, RequestId: {RequestId}",
                address, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving taskscheduled entities");
        }
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<TaskScheduledEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByName taskscheduled request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved taskscheduled entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving taskscheduled entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving taskscheduled entities");
        }
    }

    [HttpGet("by-version/{version}")]
    public async Task<ActionResult<IEnumerable<TaskScheduledEntity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByVersion taskscheduled request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByVersionAsync(version);

            _logger.LogInformation("Successfully retrieved taskscheduled entities by version. Version: {Version}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                version, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving taskscheduled entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving taskscheduled entities");
        }
    }

    [HttpPost]
    public async Task<ActionResult<TaskScheduledEntity>> Create([FromBody] TaskScheduledEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create taskscheduled request. Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Address, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create taskscheduled request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating taskscheduled entity with details. Address: {Address}, Version: {Version}, Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Address, entity.Version, entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new TaskScheduledEntity. Address: {Address}, Version: {Version}, User: {User}, RequestId: {RequestId}",
                    entity.Address, entity.Version, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created taskscheduled entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Address, created.Version, created.Name, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict creating taskscheduled entity. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating taskscheduled entity. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the taskscheduled");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskScheduledEntity>> Update(Guid id, [FromBody] TaskScheduledEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update taskscheduled request. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Address, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update taskscheduled request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update taskscheduled request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("TaskScheduled entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"TaskScheduled with ID {id} not found");
            }

            _logger.LogDebug("Updating taskscheduled entity. Id: {Id}, OldAddress: {OldAddress}, NewAddress: {NewAddress}, OldVersion: {OldVersion}, NewVersion: {NewVersion}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, entity.Address, existing.Version, entity.Version, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated taskscheduled entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Address, updated.Version, updated.Name, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating taskscheduled entity. Id: {Id}, Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("TaskScheduled entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"TaskScheduled with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating taskscheduled entity. Id: {Id}, Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the taskscheduled");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete taskscheduled request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("TaskScheduled entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"TaskScheduled with ID {id} not found");
            }

            _logger.LogDebug("Deleting taskscheduled entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete taskscheduled entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the taskscheduled entity");
            }

            _logger.LogInformation("Successfully deleted taskscheduled entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting taskscheduled entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the taskscheduled");
        }
    }
}
