using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EntitiesManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlowsController : ControllerBase
{
    private readonly IFlowEntityRepository _repository;
    private readonly ILogger<FlowsController> _logger;

    public FlowsController(
        IFlowEntityRepository repository,
        ILogger<FlowsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FlowEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll flows request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all flow entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all flow entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged flows request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved paged flow entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogError(ex, "Error retrieving paged flow entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FlowEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById flow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Flow entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Flow with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved flow entity by ID. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, entity.Address, entity.Version, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the flow entity");
        }
    }

    [HttpGet("by-key/{address}/{version}")]
    public async Task<ActionResult<FlowEntity>> GetByCompositeKey(string address, string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = $"{address}_{version}";

        _logger.LogInformation("Starting GetByCompositeKey flow request. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            address, version, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);

            if (entity == null)
            {
                _logger.LogWarning("Flow entity not found by composite key. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    address, version, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Flow with address '{address}' and version '{version}' not found");
            }

            _logger.LogInformation("Successfully retrieved flow entity by composite key. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, address, version, entity.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entity by composite key. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                address, version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the flow entity");
        }
    }

    [HttpGet("by-address/{address}")]
    public async Task<ActionResult<IEnumerable<FlowEntity>>> GetByAddress(string address)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByAddress flow request. Address: {Address}, User: {User}, RequestId: {RequestId}",
            address, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByAddressAsync(address);

            _logger.LogInformation("Successfully retrieved flow entities by address. Address: {Address}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                address, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entities by address. Address: {Address}, User: {User}, RequestId: {RequestId}",
                address, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<FlowEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByName flow request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved flow entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpGet("by-version/{version}")]
    public async Task<ActionResult<IEnumerable<FlowEntity>>> GetByVersion(string version)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByVersion flow request. Version: {Version}, User: {User}, RequestId: {RequestId}",
            version, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByVersionAsync(version);

            _logger.LogInformation("Successfully retrieved flow entities by version. Version: {Version}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                version, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow entities by version. Version: {Version}, User: {User}, RequestId: {RequestId}",
                version, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving flow entities");
        }
    }

    [HttpPost]
    public async Task<ActionResult<FlowEntity>> Create([FromBody] FlowEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create flow request. Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Address, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create flow request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating flow entity with details. Address: {Address}, Version: {Version}, Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Address, entity.Version, entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new FlowEntity. Address: {Address}, Version: {Version}, User: {User}, RequestId: {RequestId}",
                    entity.Address, entity.Version, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created flow entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Address, created.Version, created.Name, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict creating flow entity. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating flow entity. Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the flow");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FlowEntity>> Update(Guid id, [FromBody] FlowEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update flow request. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Address, entity?.Version, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update flow request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update flow request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Flow entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Flow with ID {id} not found");
            }

            _logger.LogDebug("Updating flow entity. Id: {Id}, OldAddress: {OldAddress}, NewAddress: {NewAddress}, OldVersion: {OldVersion}, NewVersion: {NewVersion}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, entity.Address, existing.Version, entity.Version, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated flow entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Address, updated.Version, updated.Name, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating flow entity. Id: {Id}, Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("Flow entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"Flow with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flow entity. Id: {Id}, Address: {Address}, Version: {Version}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Address, entity?.Version, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the flow");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete flow request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Flow entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Flow with ID {id} not found");
            }

            _logger.LogDebug("Deleting flow entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete flow entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the flow entity");
            }

            _logger.LogInformation("Successfully deleted flow entity. Id: {Id}, Address: {Address}, Version: {Version}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Address, existing.Version, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flow entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the flow");
        }
    }
}
