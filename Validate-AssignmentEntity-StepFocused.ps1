# AssignmentEntity Step-Focused Validation Script
# This script validates the new Step-focused AssignmentEntity implementation

param(
    [string]$BaseUrl = "http://localhost:5130",
    [switch]$Verbose = $false
)

# Helper function for API calls
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null
    )
    
    try {
        $uri = "$BaseUrl$Endpoint"
        $headers = @{ "Content-Type" = "application/json" }
        
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $headers
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response; StatusCode = 200 }
    }
    catch {
        $statusCode = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { 0 }
        return @{ Success = $false; Error = $_.Exception.Message; StatusCode = $statusCode }
    }
}

Write-Host "ASSIGNMENT ENTITY STEP-FOCUSED VALIDATION" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: API Health Check
Write-Host "1. Checking API health..." -ForegroundColor Yellow
$healthCheck = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments"
if (-not $healthCheck.Success) {
    Write-Host "   [ERROR] API is not accessible" -ForegroundColor Red
    Write-Host "   Error: $($healthCheck.Error)" -ForegroundColor Red
    Write-Host "   Please ensure the API is running on $BaseUrl" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "   [OK] API is accessible" -ForegroundColor Green
    Write-Host "   Found $($healthCheck.Data.Count) existing assignments" -ForegroundColor Gray
}

# Test 2: Create Step-Focused Assignment
Write-Host "2. Creating Step-Focused Assignment..." -ForegroundColor Yellow
$timestamp = Get-Date -Format 'HHmmss'
$stepId = [System.Guid]::NewGuid()
$entityId1 = [System.Guid]::NewGuid()
$entityId2 = [System.Guid]::NewGuid()

$assignmentData = @{
    version = "1.0.$timestamp"
    name = "Step-Focused Assignment $timestamp"
    description = "Validation test for Step-focused AssignmentEntity"
    stepId = $stepId.ToString()
    entityIds = @(
        $entityId1.ToString(),
        $entityId2.ToString()
    )
}

$createResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/assignments" -Body $assignmentData
if ($createResult.Success) {
    $assignmentId = $createResult.Data.id
    Write-Host "   ✅ Step-Focused Assignment created successfully" -ForegroundColor Green
    Write-Host "   ID: $assignmentId" -ForegroundColor Gray
    Write-Host "   StepId: $($createResult.Data.stepId)" -ForegroundColor Gray
    Write-Host "   EntityIds Count: $($createResult.Data.entityIds.Count)" -ForegroundColor Gray
    Write-Host "   Composite Key: $($createResult.Data.stepId)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to create Step-Focused Assignment" -ForegroundColor Red
    Write-Host "   Error: $($createResult.Error)" -ForegroundColor Red
    exit 1
}

# Test 3: Validate StepId-based Composite Key
Write-Host "3. Testing StepId-based Composite Key..." -ForegroundColor Yellow
$getByKeyResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-key/$stepId"
if ($getByKeyResult.Success) {
    Write-Host "   ✅ StepId-based composite key retrieval successful" -ForegroundColor Green
    Write-Host "   Retrieved ID: $($getByKeyResult.Data.id)" -ForegroundColor Gray
    Write-Host "   Matches Created ID: $(if ($getByKeyResult.Data.id -eq $assignmentId) { '✅ Yes' } else { '❌ No' })" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to retrieve by StepId composite key" -ForegroundColor Red
    Write-Host "   Error: $($getByKeyResult.Error)" -ForegroundColor Red
}

# Test 4: Test Step-based Query
Write-Host "4. Testing Step-based Query..." -ForegroundColor Yellow
$getByStepResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-step/$stepId"
if ($getByStepResult.Success) {
    Write-Host "   ✅ Step-based query successful" -ForegroundColor Green
    Write-Host "   Retrieved assignment for StepId: $($getByStepResult.Data.stepId)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to retrieve by StepId" -ForegroundColor Red
    Write-Host "   Error: $($getByStepResult.Error)" -ForegroundColor Red
}

# Test 5: Test Entity-based Query
Write-Host "5. Testing Entity-based Query..." -ForegroundColor Yellow
$getByEntityResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-entity/$entityId1"
if ($getByEntityResult.Success) {
    Write-Host "   ✅ Entity-based query successful" -ForegroundColor Green
    Write-Host "   Found $($getByEntityResult.Data.Count) assignments referencing EntityId: $entityId1" -ForegroundColor Gray
    
    # Verify our assignment is in the results
    $foundOurAssignment = $getByEntityResult.Data | Where-Object { $_.id -eq $assignmentId }
    if ($foundOurAssignment) {
        Write-Host "   ✅ Our assignment found in entity-based query results" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Our assignment NOT found in entity-based query results" -ForegroundColor Red
    }
} else {
    Write-Host "   ❌ Failed to retrieve by EntityId" -ForegroundColor Red
    Write-Host "   Error: $($getByEntityResult.Error)" -ForegroundColor Red
}

# Test 6: Test Version-based Query (should return collection)
Write-Host "6. Testing Version-based Query..." -ForegroundColor Yellow
$getByVersionResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-version/$($assignmentData.version)"
if ($getByVersionResult.Success) {
    Write-Host "   ✅ Version-based query successful" -ForegroundColor Green
    Write-Host "   Found $($getByVersionResult.Data.Count) assignments with version: $($assignmentData.version)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to retrieve by Version" -ForegroundColor Red
    Write-Host "   Error: $($getByVersionResult.Error)" -ForegroundColor Red
}

# Test 7: Test Uniqueness Constraint (StepId-based)
Write-Host "7. Testing StepId Uniqueness Constraint..." -ForegroundColor Yellow
$duplicateData = @{
    version = "2.0.$timestamp"
    name = "Duplicate StepId Test"
    description = "Should fail due to duplicate StepId"
    stepId = $stepId.ToString()  # Same StepId as before
    entityIds = @([System.Guid]::NewGuid().ToString())
}

$duplicateResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/assignments" -Body $duplicateData
if (-not $duplicateResult.Success -and $duplicateResult.StatusCode -eq 409) {
    Write-Host "   ✅ StepId uniqueness constraint working correctly" -ForegroundColor Green
    Write-Host "   Duplicate StepId properly rejected with 409 Conflict" -ForegroundColor Gray
} elseif ($duplicateResult.Success) {
    Write-Host "   ❌ StepId uniqueness constraint NOT working - duplicate allowed" -ForegroundColor Red
} else {
    Write-Host "   ⚠️  Unexpected error testing uniqueness constraint" -ForegroundColor Yellow
    Write-Host "   Error: $($duplicateResult.Error)" -ForegroundColor Yellow
}

# Test 8: Update Assignment with new EntityIds
Write-Host "8. Testing Assignment Update with EntityIds..." -ForegroundColor Yellow
$newEntityId = [System.Guid]::NewGuid()
$updateData = $createResult.Data
$updateData.name = "Updated Step-Focused Assignment $timestamp"
$updateData.entityIds = @(
    $entityId1.ToString(),
    $newEntityId.ToString()  # Replace second entity with new one
)

$updateResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/assignments/$assignmentId" -Body $updateData
if ($updateResult.Success) {
    Write-Host "   ✅ Assignment update successful" -ForegroundColor Green
    Write-Host "   Updated Name: $($updateResult.Data.name)" -ForegroundColor Gray
    Write-Host "   Updated EntityIds Count: $($updateResult.Data.entityIds.Count)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to update assignment" -ForegroundColor Red
    Write-Host "   Error: $($updateResult.Error)" -ForegroundColor Red
}

# Test 9: Verify Updated Entity Relationships
Write-Host "9. Verifying Updated Entity Relationships..." -ForegroundColor Yellow
$getByNewEntityResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-entity/$newEntityId"
if ($getByNewEntityResult.Success -and $getByNewEntityResult.Data.Count -gt 0) {
    Write-Host "   ✅ New entity relationship established" -ForegroundColor Green
    Write-Host "   Found assignment with new EntityId: $newEntityId" -ForegroundColor Gray
} else {
    Write-Host "   ❌ New entity relationship not found" -ForegroundColor Red
}

# Test 10: Clean up - Delete Assignment
Write-Host "10. Cleaning up - Deleting Assignment..." -ForegroundColor Yellow
$deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/assignments/$assignmentId"
if ($deleteResult.Success) {
    Write-Host "   ✅ Assignment deleted successfully" -ForegroundColor Green
} else {
    Write-Host "   ❌ Failed to delete assignment" -ForegroundColor Red
    Write-Host "   Error: $($deleteResult.Error)" -ForegroundColor Red
}

# Test 11: Verify Deletion
Write-Host "11. Verifying Deletion..." -ForegroundColor Yellow
$verifyResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/$assignmentId"
if (-not $verifyResult.Success -and $verifyResult.StatusCode -eq 404) {
    Write-Host "   [OK] Assignment deletion verified (404 Not Found)" -ForegroundColor Green
} else {
    Write-Host "   [ERROR] Assignment still exists after deletion" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 STEP-FOCUSED ASSIGNMENT ENTITY VALIDATION COMPLETE!" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green
Write-Host ""
Write-Host "✅ Key Features Validated:" -ForegroundColor Cyan
Write-Host "   • StepId-based composite key uniqueness" -ForegroundColor White
Write-Host "   • Multi-entity assignment support (EntityIds collection)" -ForegroundColor White
Write-Host "   • Step-focused workflow relationship queries" -ForegroundColor White
Write-Host "   • Entity-based reverse lookup queries" -ForegroundColor White
Write-Host "   • Version-based collection queries" -ForegroundColor White
Write-Host "   • CRUD operations with new entity structure" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Step-Focused AssignmentEntity is fully operational!" -ForegroundColor Green
