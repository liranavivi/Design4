# SourceEntity-ScheduledFlow Referential Integrity Impact Analysis

## **REQUIRED VALIDATION RULES**

### **1. DELETE SourceEntity Validation**
- **Rule**: Before allowing deletion of a SourceEntity, verify that no ScheduledFlowEntity records reference the SourceEntity.SourceId
- **Action**: If references exist, prevent deletion and return appropriate error message

### **2. UPDATE SourceEntity Validation** 
- **Rule**: Before allowing update of a SourceEntity, verify that no ScheduledFlowEntity records reference the SourceEntity.SourceId
- **Action**: If references exist, prevent update and return appropriate error message

---

## **IMPACT ANALYSIS**

### **🔴 HIGH IMPACT - BREAKING CHANGES REQUIRED**

#### **1. Entity Relationship Analysis**
- **Current Relationship**: ScheduledFlowEntity.SourceId → SourceEntity.Id
- **Cardinality**: Many ScheduledFlow → One Source
- **Reference Type**: Foreign Key relationship (SourceId property)
- **Validation Scope**: Both DELETE and UPDATE operations on SourceEntity

#### **2. Data Integrity Risk**
- **DELETE Risk**: Orphaned ScheduledFlowEntity records with invalid SourceId references
- **UPDATE Risk**: ScheduledFlowEntity records pointing to modified SourceEntity that may break workflow execution
- **Cascade Impact**: Workflow execution failures if SourceEntity is deleted/modified while referenced

---

## **IMPLEMENTATION REQUIREMENTS**

### **🔧 Core Components to Modify**

#### **1. ReferentialIntegrityService Enhancement**

**Following the same patterns as ProtocolEntity validation:**

```csharp
// Interface extension
public interface IReferentialIntegrityService
{
    // Existing methods...
    Task<ReferentialIntegrityResult> ValidateProtocolDeletionAsync(Guid protocolId);

    // NEW METHODS for SourceEntity validation
    Task<ReferentialIntegrityResult> ValidateSourceEntityDeletionAsync(Guid sourceId);
    Task<ReferentialIntegrityResult> ValidateSourceEntityUpdateAsync(Guid sourceId);
    Task<SourceEntityReferenceInfo> GetSourceEntityReferencesAsync(Guid sourceId);
}

// New result type following ProtocolReferenceInfo pattern
public class SourceEntityReferenceInfo
{
    public long ScheduledFlowEntityCount { get; set; }
    public long TotalReferences => ScheduledFlowEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (ScheduledFlowEntityCount > 0)
            types.Add($"ScheduledFlowEntity ({ScheduledFlowEntityCount} records)");
        return types;
    }
}

// Service implementation
public async Task<ReferentialIntegrityResult> ValidateSourceEntityDeletionAsync(Guid sourceId)
{
    var startTime = DateTime.UtcNow;
    _logger.LogInformation("Starting referential integrity validation for SourceEntity {SourceId}", sourceId);

    try
    {
        var references = await GetSourceEntityReferencesAsync(sourceId);
        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation("Referential integrity validation completed in {Duration}ms. Found {TotalReferences} references ({ScheduledFlowCount} scheduled flows)",
            duration.TotalMilliseconds, references.TotalReferences, references.ScheduledFlowEntityCount);

        if (references.HasReferences)
        {
            var referencingTypes = references.GetReferencingEntityTypes();
            var errorMessage = $"Cannot delete SourceEntity. Referenced by: {string.Join(", ", referencingTypes)}";
            return ReferentialIntegrityResult.Invalid(errorMessage, references);
        }

        var result = ReferentialIntegrityResult.Valid();
        result.ValidationDuration = duration;
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during referential integrity validation for SourceEntity {SourceId}", sourceId);
        throw;
    }
}

public async Task<ReferentialIntegrityResult> ValidateSourceEntityUpdateAsync(Guid sourceId)
{
    _logger.LogInformation("Validating SourceEntity update for {SourceId}", sourceId);
    return await ValidateSourceEntityDeletionAsync(sourceId); // Same validation logic
}

public async Task<SourceEntityReferenceInfo> GetSourceEntityReferencesAsync(Guid sourceId)
{
    var references = new SourceEntityReferenceInfo();

    // Count ScheduledFlowEntity references
    var filter = Builders<ScheduledFlowEntity>.Filter.Eq(x => x.SourceId, sourceId);
    references.ScheduledFlowEntityCount = await _scheduledFlowCollection.CountDocumentsAsync(filter);

    return references;
}
```

#### **2. Enhanced ReferentialIntegrityException**

**Support for SourceEntity References (following ProtocolEntity pattern):**

```csharp
public class ReferentialIntegrityException : Exception
{
    public ProtocolReferenceInfo? ProtocolReferences { get; }
    public SourceEntityReferenceInfo? SourceEntityReferences { get; }

    // Existing constructor for ProtocolEntity
    public ReferentialIntegrityException(string message, ProtocolReferenceInfo references)
        : base(message)
    {
        ProtocolReferences = references;
    }

    // NEW constructor for SourceEntity
    public ReferentialIntegrityException(string message, SourceEntityReferenceInfo references)
        : base(message)
    {
        SourceEntityReferences = references;
    }

    public string GetDetailedMessage()
    {
        // Handle SourceEntity references
        if (SourceEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify SourceEntity. Found {SourceEntityReferences.ScheduledFlowEntityCount} ScheduledFlowEntity reference{(SourceEntityReferences.ScheduledFlowEntityCount > 1 ? "s" : "")}.";
        }

        // Handle ProtocolEntity references (existing logic)
        if (ProtocolReferences?.HasReferences == true)
        {
            var referenceDetails = new List<string>();

            if (ProtocolReferences.SourceEntityCount > 0)
                referenceDetails.Add($"{ProtocolReferences.SourceEntityCount} SourceEntity reference{(ProtocolReferences.SourceEntityCount > 1 ? "s" : "")}");

            if (ProtocolReferences.DestinationEntityCount > 0)
                referenceDetails.Add($"{ProtocolReferences.DestinationEntityCount} DestinationEntity reference{(ProtocolReferences.DestinationEntityCount > 1 ? "s" : "")}");

            var details = string.Join(" and ", referenceDetails);
            return $"Cannot delete ProtocolEntity. Found {details}.";
        }

        return Message;
    }
}
```

#### **3. SourceEntity Repository Modifications**

**Following the same pattern as ProtocolEntityRepository:**

```csharp
public class SourceEntityRepository : BaseRepository<SourceEntity>, ISourceEntityRepository
{
    private readonly IReferentialIntegrityService _referentialIntegrityService;

    public SourceEntityRepository(
        IMongoDatabase database,
        ILogger<SourceEntityRepository> logger,
        IEventPublisher eventPublisher,
        IReferentialIntegrityService referentialIntegrityService)
        : base(database, "sources", logger, eventPublisher)
    {
        _referentialIntegrityService = referentialIntegrityService;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Validating referential integrity before deleting SourceEntity {Id}", id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateSourceEntityDeletionAsync(id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented deletion of SourceEntity {Id}: {Error}. References: {ScheduledFlowCount} scheduled flows",
                    id, validationResult.ErrorMessage, validationResult.SourceEntityReferences.ScheduledFlowEntityCount);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.SourceEntityReferences);
            }

            _logger.LogInformation("Referential integrity validation passed for SourceEntity {Id}. Proceeding with deletion", id);
            return await base.DeleteAsync(id);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SourceEntity deletion validation for {Id}", id);
            throw;
        }
    }

    public override async Task<SourceEntity> UpdateAsync(SourceEntity entity)
    {
        _logger.LogInformation("Validating referential integrity before updating SourceEntity {Id}", entity.Id);

        try
        {
            var validationResult = await _referentialIntegrityService.ValidateSourceEntityUpdateAsync(entity.Id);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Referential integrity violation prevented update of SourceEntity {Id}: {Error}",
                    entity.Id, validationResult.ErrorMessage);
                throw new ReferentialIntegrityException(validationResult.ErrorMessage, validationResult.SourceEntityReferences);
            }

            _logger.LogInformation("Referential integrity validation passed for SourceEntity {Id}. Proceeding with update", entity.Id);
            return await base.UpdateAsync(entity);
        }
        catch (ReferentialIntegrityException)
        {
            throw; // Re-throw referential integrity exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SourceEntity update validation for {Id}", entity.Id);
            throw;
        }
    }
}
```

#### **4. SourcesController Error Handling**

**Following the same pattern as ProtocolsController:**

```csharp
// In SourcesController.cs - Update method
[HttpPut("{id:guid}")]
public async Task<ActionResult<SourceEntity>> Update(Guid id, [FromBody] SourceEntity entity)
{
    var userContext = User.Identity?.Name ?? "Anonymous";

    try
    {
        // ... existing validation logic ...

        entity.Id = id;
        entity.UpdatedBy = userContext;

        var updated = await _repository.UpdateAsync(entity);
        return Ok(updated);
    }
    catch (ReferentialIntegrityException ex)
    {
        _logger.LogWarning("Referential integrity violation during source update. Id: {Id}, Error: {Error}, User: {User}, RequestId: {RequestId}",
            id, ex.GetDetailedMessage(), userContext, HttpContext.TraceIdentifier);

        return Conflict(new {
            error = ex.GetDetailedMessage(),
            errorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
            referencingEntities = ex.SourceEntityReferences != null ? new {
                scheduledFlowEntityCount = ex.SourceEntityReferences.ScheduledFlowEntityCount,
                totalReferences = ex.SourceEntityReferences.TotalReferences,
                entityTypes = ex.SourceEntityReferences.GetReferencingEntityTypes()
            } : null
        });
    }
    // ... other exception handling ...
}

// In SourcesController.cs - Delete method
[HttpDelete("{id:guid}")]
public async Task<ActionResult> Delete(Guid id)
{
    var userContext = User.Identity?.Name ?? "Anonymous";

    try
    {
        var deleted = await _repository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound($"Source with ID {id} not found");
        }

        return Ok();
    }
    catch (ReferentialIntegrityException ex)
    {
        _logger.LogWarning("Referential integrity violation during source deletion. Id: {Id}, Error: {Error}, User: {User}, RequestId: {RequestId}",
            id, ex.GetDetailedMessage(), userContext, HttpContext.TraceIdentifier);

        return Conflict(new {
            error = ex.GetDetailedMessage(),
            errorCode = "REFERENTIAL_INTEGRITY_VIOLATION",
            referencingEntities = ex.SourceEntityReferences != null ? new {
                scheduledFlowEntityCount = ex.SourceEntityReferences.ScheduledFlowEntityCount,
                totalReferences = ex.SourceEntityReferences.TotalReferences,
                entityTypes = ex.SourceEntityReferences.GetReferencingEntityTypes()
            } : null
        });
    }
    // ... other exception handling ...
}
```

---

## **PERFORMANCE CONSIDERATIONS**

### **🚀 Database Optimization Required**

#### **1. MongoDB Index Creation**
```javascript
// Required index for efficient SourceId lookups
db.scheduledflows.createIndex({ "sourceId": 1 }, { name: "idx_scheduledflows_sourceId", background: true });
```

#### **2. Query Performance**
- **Lookup Pattern**: `db.scheduledflows.find({ "sourceId": ObjectId("...") })`
- **Expected Performance**: Sub-millisecond with proper indexing
- **Scaling**: Linear with number of ScheduledFlowEntity records per SourceEntity

---

## **TESTING REQUIREMENTS**

### **🧪 Comprehensive Test Scenarios**

**Following the same testing patterns as ProtocolEntity validation:**

#### **1. DELETE Validation Tests**
- ✅ **Test 1**: Delete SourceEntity without ScheduledFlow references (should succeed)
- ✅ **Test 2**: Delete SourceEntity with ScheduledFlow references (should fail with 409)
- ✅ **Test 3**: Delete SourceEntity after removing all ScheduledFlow references (should succeed)
- ✅ **Test 4**: Error message content validation (should mention ScheduledFlowEntity)
- ✅ **Test 5**: Multiple ScheduledFlow references error message accuracy

#### **2. UPDATE Validation Tests**
- ✅ **Test 6**: Update SourceEntity without ScheduledFlow references (should succeed)
- ✅ **Test 7**: Update SourceEntity with ScheduledFlow references (should fail with 409)
- ✅ **Test 8**: Update SourceEntity after removing all ScheduledFlow references (should succeed)
- ✅ **Test 9**: Error message content validation for updates (should mention ScheduledFlowEntity)

#### **3. Performance Tests**
- ✅ **Test 10**: Validation performance with multiple ScheduledFlow references (< 100ms)
- ✅ **Test 11**: Concurrent operation handling
- ✅ **Test 12**: Index utilization verification

#### **4. Expected Error Response Format**
```json
{
  "error": "Cannot modify SourceEntity. Found 3 ScheduledFlowEntity references.",
  "errorCode": "REFERENTIAL_INTEGRITY_VIOLATION",
  "referencingEntities": {
    "scheduledFlowEntityCount": 3,
    "totalReferences": 3,
    "entityTypes": ["ScheduledFlowEntity (3 records)"]
  }
}
```

#### **5. Test Script Structure (PowerShell)**
```powershell
# SourceEntity-ScheduledFlow-ReferentialIntegrity-Test.ps1
# Following the same structure as ProtocolEntity test

# Test scenarios:
# 1. Create SourceEntity without references -> DELETE (should succeed)
# 2. Create SourceEntity with ScheduledFlow references -> DELETE (should fail)
# 3. Create SourceEntity with ScheduledFlow references -> UPDATE (should fail)
# 4. Remove ScheduledFlow references -> DELETE (should succeed)
# 5. Error message validation
# 6. Performance testing with multiple references
```

---

## **MIGRATION STRATEGY**

### **📋 Implementation Steps**

#### **Phase 1: Infrastructure Setup**
1. ✅ Create MongoDB index for ScheduledFlowEntity.SourceId
2. ✅ Extend ReferentialIntegrityService with SourceEntity validation
3. ✅ Add SourceEntityReferences class for detailed error reporting

#### **Phase 2: Repository Integration**
1. ✅ Modify SourceEntityRepository.UpdateAsync with validation
2. ✅ Modify SourceEntityRepository.DeleteAsync with validation
3. ✅ Update MassTransit consumers for validation

#### **Phase 3: Controller Enhancement**
1. ✅ Add ReferentialIntegrityException handling in SourcesController
2. ✅ Implement proper HTTP status codes (409 Conflict)
3. ✅ Add detailed logging for validation failures

#### **Phase 4: Testing & Validation**
1. ✅ Create comprehensive test suite
2. ✅ Validate error message formatting
3. ✅ Performance testing with realistic data volumes

---

## **RISK ASSESSMENT**

### **🔴 HIGH RISK AREAS**

#### **1. Data Consistency**
- **Risk**: Existing orphaned references in production data
- **Mitigation**: Data cleanup script before deployment
- **Validation**: Pre-deployment data integrity check

#### **2. Performance Impact**
- **Risk**: Additional database queries on every UPDATE/DELETE
- **Mitigation**: Proper indexing and query optimization
- **Monitoring**: Performance metrics tracking

#### **3. Workflow Disruption**
- **Risk**: Legitimate operations blocked by referential integrity
- **Mitigation**: Clear error messages and resolution guidance
- **Support**: Documentation for handling validation failures

---

## **SUCCESS CRITERIA**

### **✅ Implementation Complete When:**
1. All SourceEntity DELETE operations validate ScheduledFlow references
2. All SourceEntity UPDATE operations validate ScheduledFlow references  
3. Appropriate 409 Conflict responses with detailed error messages
4. Performance under 100ms for validation operations
5. 100% test coverage for all validation scenarios
6. Zero data integrity violations in production

---

## **CONCLUSION**

This referential integrity implementation for SourceEntity-ScheduledFlow relationships is **CRITICAL** for maintaining data consistency in the workflow execution system. The validation prevents orphaned ScheduledFlowEntity records and ensures reliable workflow operations.

**Estimated Implementation Time**: 4-6 hours
**Testing Time**: 2-3 hours  
**Total Impact**: HIGH - Essential for data integrity
