using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EntitiesManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProtocolsController : ControllerBase
{
    private readonly IProtocolEntityRepository _repository;
    private readonly ILogger<ProtocolsController> _logger;

    public ProtocolsController(
        IProtocolEntityRepository repository,
        ILogger<ProtocolsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProtocolEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll protocols request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all protocol entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all protocol entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving protocol entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged protocols request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved paged protocol entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogError(ex, "Error retrieving paged protocol entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving protocol entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProtocolEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById protocol request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Protocol entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Protocol with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved protocol entity by ID. Id: {Id}, Name: {Name}, User: {User}, RequestId: {RequestId}",
                id, entity.Name, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving protocol entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the protocol entity");
        }
    }

    [HttpGet("by-key/{name}")]
    public async Task<ActionResult<ProtocolEntity>> GetByCompositeKey(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = name; // ProtocolEntity now uses only name as composite key

        _logger.LogInformation("Starting GetByCompositeKey protocol request. Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            name, compositeKey, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByCompositeKeyAsync(compositeKey);

            if (entity == null)
            {
                _logger.LogWarning("Protocol entity not found by composite key. Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                    name, compositeKey, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Protocol with name '{name}' not found");
            }

            _logger.LogInformation("Successfully retrieved protocol entity by composite key. Id: {Id}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity.Id, name, compositeKey, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving protocol entity by composite key. Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the protocol entity");
        }
    }



    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<IEnumerable<ProtocolEntity>>> GetByName(string name)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByName protocol request. Name: {Name}, User: {User}, RequestId: {RequestId}",
            name, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNameAsync(name);

            _logger.LogInformation("Successfully retrieved protocol entities by name. Name: {Name}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                name, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving protocol entities by name. Name: {Name}, User: {User}, RequestId: {RequestId}",
                name, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving protocol entities");
        }
    }

    // GetByVersion endpoint removed since ProtocolEntity no longer has Version property

    [HttpPost]
    public async Task<ActionResult<ProtocolEntity>> Create([FromBody] ProtocolEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create protocol request. Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create protocol request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating protocol entity with details. Name: {Name}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.Name, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new ProtocolEntity. Name: {Name}, User: {User}, RequestId: {RequestId}",
                    entity.Name, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created protocol entity. Id: {Id}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.Name, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict creating protocol entity. Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating protocol entity. Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the protocol");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProtocolEntity>> Update(Guid id, [FromBody] ProtocolEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update protocol request. Id: {Id}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update protocol request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update protocol request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Protocol entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Protocol with ID {id} not found");
            }

            _logger.LogDebug("Updating protocol entity. Id: {Id}, OldName: {OldName}, NewName: {NewName}, User: {User}, RequestId: {RequestId}",
                id, existing.Name, entity.Name, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated protocol entity. Id: {Id}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.Name, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating protocol entity. Id: {Id}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("Protocol entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"Protocol with ID {id} not found");
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented update of protocol entity. Id: {Id}, Error: {Error}, References: {SourceCount} sources, {DestinationCount} destinations, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.References.SourceEntityCount, ex.References.DestinationEntityCount, userContext, HttpContext.TraceIdentifier);

            var detailedMessage = ex.GetDetailedMessage();
            return Conflict(new
            {
                error = detailedMessage,
                errorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                referencingEntities = new
                {
                    sourceEntityCount = ex.References.SourceEntityCount,
                    destinationEntityCount = ex.References.DestinationEntityCount,
                    totalReferences = ex.References.TotalReferences,
                    entityTypes = ex.References.GetReferencingEntityTypes()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating protocol entity. Id: {Id}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.Name, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the protocol");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete protocol request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Protocol entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Protocol with ID {id} not found");
            }

            _logger.LogDebug("Deleting protocol entity. Id: {Id}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete protocol entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the protocol entity");
            }

            _logger.LogInformation("Successfully deleted protocol entity. Id: {Id}, Name: {Name}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.Name, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of protocol entity. Id: {Id}, Error: {Error}, References: {SourceCount} sources, {DestinationCount} destinations, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.References.SourceEntityCount, ex.References.DestinationEntityCount, userContext, HttpContext.TraceIdentifier);

            var detailedMessage = ex.GetDetailedMessage();
            return Conflict(new
            {
                error = detailedMessage,
                errorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
                referencingEntities = new
                {
                    sourceEntityCount = ex.References.SourceEntityCount,
                    destinationEntityCount = ex.References.DestinationEntityCount,
                    totalReferences = ex.References.TotalReferences,
                    entityTypes = ex.References.GetReferencingEntityTypes()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting protocol entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the protocol");
        }
    }
}
