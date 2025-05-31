using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Exceptions;
using EntitiesManager.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EntitiesManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StepsController : ControllerBase
{
    private readonly IStepEntityRepository _repository;
    private readonly ILogger<StepsController> _logger;

    public StepsController(
        IStepEntityRepository repository,
        ILogger<StepsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StepEntity>>> GetAll()
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetAll steps request. User: {User}, RequestId: {RequestId}",
            userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetAllAsync();

            _logger.LogInformation("Successfully retrieved all step entities. Count: {Count}, User: {User}, RequestId: {RequestId}",
                entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all step entities. User: {User}, RequestId: {RequestId}",
                userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving step entities");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var originalPage = page;
        var originalPageSize = pageSize;

        _logger.LogInformation("Starting GetPaged steps request. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
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

            _logger.LogInformation("Successfully retrieved paged step entities. Page: {Page}, PageSize: {PageSize}, Count: {Count}, TotalCount: {TotalCount}, TotalPages: {TotalPages}, User: {User}, RequestId: {RequestId}",
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
            _logger.LogError(ex, "Error retrieving paged step entities. Page: {Page}, PageSize: {PageSize}, User: {User}, RequestId: {RequestId}",
                page, pageSize, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving step entities");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StepEntity>> GetById(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetById step request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                _logger.LogWarning("Step entity not found. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Step with ID {id} not found");
            }

            _logger.LogInformation("Successfully retrieved step entity by ID. Id: {Id}, EntityId: {EntityId}, User: {User}, RequestId: {RequestId}",
                id, entity.EntityId, userContext, HttpContext.TraceIdentifier);

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving step entity by ID. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving the step entity");
        }
    }

    // GetByCompositeKey method removed since StepEntity no longer uses composite keys

    [HttpGet("by-entity-id/{entityId:guid}")]
    public async Task<ActionResult<IEnumerable<StepEntity>>> GetByEntityId(Guid entityId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByEntityId step request. EntityId: {EntityId}, User: {User}, RequestId: {RequestId}",
            entityId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByEntityIdAsync(entityId);

            _logger.LogInformation("Successfully retrieved step entities by entity ID. EntityId: {EntityId}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                entityId, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving step entities by entity ID. EntityId: {EntityId}, User: {User}, RequestId: {RequestId}",
                entityId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving step entities");
        }
    }

    [HttpGet("by-next-step-id/{nextStepId:guid}")]
    public async Task<ActionResult<IEnumerable<StepEntity>>> GetByNextStepId(Guid nextStepId)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting GetByNextStepId step request. NextStepId: {NextStepId}, User: {User}, RequestId: {RequestId}",
            nextStepId, userContext, HttpContext.TraceIdentifier);

        try
        {
            var entities = await _repository.GetByNextStepIdAsync(nextStepId);

            _logger.LogInformation("Successfully retrieved step entities by next step ID. NextStepId: {NextStepId}, Count: {Count}, User: {User}, RequestId: {RequestId}",
                nextStepId, entities.Count(), userContext, HttpContext.TraceIdentifier);

            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving step entities by next step ID. NextStepId: {NextStepId}, User: {User}, RequestId: {RequestId}",
                nextStepId, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while retrieving step entities");
        }
    }

    // GetByName and GetByVersion methods removed since StepEntity no longer has these properties

    [HttpPost]
    public async Task<ActionResult<StepEntity>> Create([FromBody] StepEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Create step request. EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            entity?.EntityId, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Create step request. ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        try
        {
            entity!.CreatedBy = userContext;
            entity.Id = Guid.Empty;

            _logger.LogDebug("Creating step entity with details. EntityId: {EntityId}, CreatedBy: {CreatedBy}, User: {User}, RequestId: {RequestId}",
                entity.EntityId, entity.CreatedBy, userContext, HttpContext.TraceIdentifier);

            var created = await _repository.CreateAsync(entity);

            if (created.Id == Guid.Empty)
            {
                _logger.LogError("MongoDB failed to generate ID for new StepEntity. EntityId: {EntityId}, User: {User}, RequestId: {RequestId}",
                    entity.EntityId, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to generate entity ID");
            }

            _logger.LogInformation("Successfully created step entity. Id: {Id}, EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                created.Id, created.EntityId, created.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict creating step entity. EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.EntityId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating step entity. EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                entity?.EntityId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while creating the step");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StepEntity>> Update(Guid id, [FromBody] StepEntity entity)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";
        var compositeKey = entity?.GetCompositeKey() ?? "Unknown";

        _logger.LogInformation("Starting Update step request. Id: {Id}, EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
            id, entity?.EntityId, compositeKey, userContext, HttpContext.TraceIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for Update step request. Id: {Id}, ValidationErrors: {ValidationErrors}, User: {User}, RequestId: {RequestId}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), userContext, HttpContext.TraceIdentifier);
            return BadRequest(ModelState);
        }

        if (id != entity!.Id)
        {
            _logger.LogWarning("ID mismatch in Update step request. UrlId: {UrlId}, BodyId: {BodyId}, User: {User}, RequestId: {RequestId}",
                id, entity.Id, userContext, HttpContext.TraceIdentifier);
            return BadRequest("ID in URL does not match ID in request body");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Step entity not found for update. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Step with ID {id} not found");
            }

            _logger.LogDebug("Updating step entity. Id: {Id}, OldEntityId: {OldEntityId}, NewEntityId: {NewEntityId}, User: {User}, RequestId: {RequestId}",
                id, existing.EntityId, entity.EntityId, userContext, HttpContext.TraceIdentifier);

            // Preserve audit fields
            entity.CreatedAt = existing.CreatedAt;
            entity.CreatedBy = existing.CreatedBy;
            entity.UpdatedBy = userContext;

            var updated = await _repository.UpdateAsync(entity);

            _logger.LogInformation("Successfully updated step entity. Id: {Id}, EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                updated.Id, updated.EntityId, updated.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return Ok(updated);
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented update of step entity. Id: {Id}, Error: {Error}, References: {FlowCount} flows, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.StepEntityReferences?.FlowEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    flowEntityCount = ex.StepEntityReferences?.FlowEntityCount ?? 0,
                    totalReferences = ex.StepEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.StepEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (DuplicateKeyException ex)
        {
            _logger.LogWarning(ex, "Duplicate key conflict updating step entity. Id: {Id}, EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.EntityId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return Conflict(new { message = ex.Message });
        }
        catch (EntityNotFoundException)
        {
            _logger.LogWarning("Step entity not found during update operation. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return NotFound($"Step with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating step entity. Id: {Id}, EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, entity?.EntityId, compositeKey, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while updating the step");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userContext = User.Identity?.Name ?? "Anonymous";

        _logger.LogInformation("Starting Delete step request. Id: {Id}, User: {User}, RequestId: {RequestId}",
            id, userContext, HttpContext.TraceIdentifier);

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Step entity not found for deletion. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return NotFound($"Step with ID {id} not found");
            }

            _logger.LogDebug("Deleting step entity. Id: {Id}, EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.EntityId, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogError("Failed to delete step entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                    id, userContext, HttpContext.TraceIdentifier);
                return StatusCode(500, "Failed to delete the step entity");
            }

            _logger.LogInformation("Successfully deleted step entity. Id: {Id}, EntityId: {EntityId}, CompositeKey: {CompositeKey}, User: {User}, RequestId: {RequestId}",
                id, existing.EntityId, existing.GetCompositeKey(), userContext, HttpContext.TraceIdentifier);

            return NoContent();
        }
        catch (ReferentialIntegrityException ex)
        {
            _logger.LogWarning("Referential integrity violation prevented deletion of step entity. Id: {Id}, Error: {Error}, References: {FlowCount} flows, User: {User}, RequestId: {RequestId}",
                id, ex.Message, ex.StepEntityReferences?.FlowEntityCount ?? 0, userContext, HttpContext.TraceIdentifier);

            return Conflict(new
            {
                error = ex.Message,
                details = ex.GetDetailedMessage(),
                referencingEntities = new
                {
                    flowEntityCount = ex.StepEntityReferences?.FlowEntityCount ?? 0,
                    totalReferences = ex.StepEntityReferences?.TotalReferences ?? 0,
                    entityTypes = ex.StepEntityReferences?.GetReferencingEntityTypes() ?? new List<string>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting step entity. Id: {Id}, User: {User}, RequestId: {RequestId}",
                id, userContext, HttpContext.TraceIdentifier);
            return StatusCode(500, "An error occurred while deleting the step");
        }
    }
}
