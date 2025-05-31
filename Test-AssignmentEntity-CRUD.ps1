# AssignmentEntity CRUD Tests - Step-Focused Implementation
# Comprehensive testing of Create, Read, Update, Delete operations

param(
    [string]$BaseUrl = "http://localhost:5130"
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

Write-Host "ASSIGNMENT ENTITY CRUD TESTS - STEP-FOCUSED" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$testResults = @()
$timestamp = Get-Date -Format 'HHmmss'

# Test 1: API Health Check
Write-Host "1. API Health Check..." -ForegroundColor Yellow
$healthCheck = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments"
if ($healthCheck.Success) {
    Write-Host "   [PASS] API is accessible" -ForegroundColor Green
    $testResults += @{ Test = "API Health"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] API not accessible: $($healthCheck.Error)" -ForegroundColor Red
    $testResults += @{ Test = "API Health"; Status = "FAIL" }
    exit 1
}

# Test 2: CREATE - Step-Focused Assignment
Write-Host "2. CREATE Test..." -ForegroundColor Yellow
$stepId = [System.Guid]::NewGuid()
$entityId1 = [System.Guid]::NewGuid()
$entityId2 = [System.Guid]::NewGuid()

$createData = @{
    version = "1.0.$timestamp"
    name = "CRUD Test Assignment $timestamp"
    description = "Test assignment for CRUD validation"
    stepId = $stepId.ToString()
    entityIds = @(
        $entityId1.ToString(),
        $entityId2.ToString()
    )
}

$createResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/assignments" -Body $createData
if ($createResult.Success) {
    $assignmentId = $createResult.Data.id
    Write-Host "   [PASS] Assignment created successfully" -ForegroundColor Green
    Write-Host "   ID: $assignmentId" -ForegroundColor Gray
    Write-Host "   StepId: $($createResult.Data.stepId)" -ForegroundColor Gray
    Write-Host "   EntityIds: $($createResult.Data.entityIds.Count) entities" -ForegroundColor Gray
    $testResults += @{ Test = "CREATE"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Create failed: $($createResult.Error)" -ForegroundColor Red
    $testResults += @{ Test = "CREATE"; Status = "FAIL" }
    exit 1
}

# Test 3: READ by ID
Write-Host "3. READ by ID Test..." -ForegroundColor Yellow
$readResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/$assignmentId"
if ($readResult.Success -and $readResult.Data.id -eq $assignmentId) {
    Write-Host "   [PASS] Read by ID successful" -ForegroundColor Green
    Write-Host "   Retrieved: $($readResult.Data.name)" -ForegroundColor Gray
    $testResults += @{ Test = "READ by ID"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Read by ID failed" -ForegroundColor Red
    $testResults += @{ Test = "READ by ID"; Status = "FAIL" }
}

# Test 4: READ by StepId (Composite Key)
Write-Host "4. READ by StepId (Composite Key) Test..." -ForegroundColor Yellow
$readByKeyResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-key/$stepId"
if ($readByKeyResult.Success -and $readByKeyResult.Data.stepId -eq $stepId.ToString()) {
    Write-Host "   [PASS] Read by StepId successful" -ForegroundColor Green
    Write-Host "   StepId: $($readByKeyResult.Data.stepId)" -ForegroundColor Gray
    $testResults += @{ Test = "READ by StepId"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Read by StepId failed" -ForegroundColor Red
    $testResults += @{ Test = "READ by StepId"; Status = "FAIL" }
}

# Test 5: READ by Step (Dedicated Endpoint)
Write-Host "5. READ by Step Test..." -ForegroundColor Yellow
$readByStepResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-step/$stepId"
if ($readByStepResult.Success -and $readByStepResult.Data.stepId -eq $stepId.ToString()) {
    Write-Host "   [PASS] Read by Step successful" -ForegroundColor Green
    $testResults += @{ Test = "READ by Step"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Read by Step failed" -ForegroundColor Red
    $testResults += @{ Test = "READ by Step"; Status = "FAIL" }
}

# Test 6: READ by EntityId
Write-Host "6. READ by EntityId Test..." -ForegroundColor Yellow
$readByEntityResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-entity/$entityId1"
if ($readByEntityResult.Success -and $readByEntityResult.Data.Count -gt 0) {
    $foundAssignment = $readByEntityResult.Data | Where-Object { $_.id -eq $assignmentId }
    if ($foundAssignment) {
        Write-Host "   [PASS] Read by EntityId successful" -ForegroundColor Green
        Write-Host "   Found $($readByEntityResult.Data.Count) assignments" -ForegroundColor Gray
        $testResults += @{ Test = "READ by EntityId"; Status = "PASS" }
    } else {
        Write-Host "   [FAIL] Assignment not found in EntityId results" -ForegroundColor Red
        $testResults += @{ Test = "READ by EntityId"; Status = "FAIL" }
    }
} else {
    Write-Host "   [FAIL] Read by EntityId failed" -ForegroundColor Red
    $testResults += @{ Test = "READ by EntityId"; Status = "FAIL" }
}

# Test 7: READ by Version
Write-Host "7. READ by Version Test..." -ForegroundColor Yellow
$readByVersionResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-version/$($createData.version)"
if ($readByVersionResult.Success -and $readByVersionResult.Data.Count -gt 0) {
    Write-Host "   [PASS] Read by Version successful" -ForegroundColor Green
    Write-Host "   Found $($readByVersionResult.Data.Count) assignments" -ForegroundColor Gray
    $testResults += @{ Test = "READ by Version"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Read by Version failed" -ForegroundColor Red
    $testResults += @{ Test = "READ by Version"; Status = "FAIL" }
}

# Test 8: UPDATE Assignment
Write-Host "8. UPDATE Test..." -ForegroundColor Yellow
$newEntityId = [System.Guid]::NewGuid()
$updateData = $createResult.Data
$updateData.name = "UPDATED CRUD Test Assignment $timestamp"
$updateData.description = "Updated description for CRUD test"
$updateData.entityIds = @(
    $entityId1.ToString(),
    $newEntityId.ToString()  # Replace second entity
)

$updateResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/assignments/$assignmentId" -Body $updateData
if ($updateResult.Success) {
    Write-Host "   [PASS] Update successful" -ForegroundColor Green
    Write-Host "   Updated Name: $($updateResult.Data.name)" -ForegroundColor Gray
    Write-Host "   EntityIds Count: $($updateResult.Data.entityIds.Count)" -ForegroundColor Gray
    $testResults += @{ Test = "UPDATE"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Update failed: $($updateResult.Error)" -ForegroundColor Red
    $testResults += @{ Test = "UPDATE"; Status = "FAIL" }
}

# Test 9: Verify Update by Reading
Write-Host "9. Verify UPDATE Test..." -ForegroundColor Yellow
$verifyUpdateResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/$assignmentId"
if ($verifyUpdateResult.Success -and $verifyUpdateResult.Data.name.Contains("UPDATED")) {
    Write-Host "   [PASS] Update verification successful" -ForegroundColor Green
    $testResults += @{ Test = "Verify UPDATE"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Update verification failed" -ForegroundColor Red
    $testResults += @{ Test = "Verify UPDATE"; Status = "FAIL" }
}

# Test 10: Test StepId Uniqueness Constraint
Write-Host "10. StepId Uniqueness Constraint Test..." -ForegroundColor Yellow
$duplicateData = @{
    version = "2.0.$timestamp"
    name = "Duplicate StepId Test"
    description = "Should fail due to duplicate StepId"
    stepId = $stepId.ToString()  # Same StepId
    entityIds = @([System.Guid]::NewGuid().ToString())
}

$duplicateResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/assignments" -Body $duplicateData
if (-not $duplicateResult.Success -and $duplicateResult.StatusCode -eq 409) {
    Write-Host "   [PASS] StepId uniqueness constraint working" -ForegroundColor Green
    $testResults += @{ Test = "Uniqueness Constraint"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] StepId uniqueness constraint not working" -ForegroundColor Red
    $testResults += @{ Test = "Uniqueness Constraint"; Status = "FAIL" }
}

# Test 11: DELETE Assignment
Write-Host "11. DELETE Test..." -ForegroundColor Yellow
$deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/assignments/$assignmentId"
if ($deleteResult.Success) {
    Write-Host "   [PASS] Delete successful" -ForegroundColor Green
    $testResults += @{ Test = "DELETE"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Delete failed: $($deleteResult.Error)" -ForegroundColor Red
    $testResults += @{ Test = "DELETE"; Status = "FAIL" }
}

# Test 12: Verify DELETE
Write-Host "12. Verify DELETE Test..." -ForegroundColor Yellow
$verifyDeleteResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/$assignmentId"
if (-not $verifyDeleteResult.Success -and $verifyDeleteResult.StatusCode -eq 404) {
    Write-Host "   [PASS] Delete verification successful (404)" -ForegroundColor Green
    $testResults += @{ Test = "Verify DELETE"; Status = "PASS" }
} else {
    Write-Host "   [FAIL] Delete verification failed" -ForegroundColor Red
    $testResults += @{ Test = "Verify DELETE"; Status = "FAIL" }
}

# Test Results Summary
Write-Host ""
Write-Host "CRUD TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

$passCount = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
$totalCount = $testResults.Count

foreach ($result in $testResults) {
    $status = if ($result.Status -eq "PASS") { "[PASS]" } else { "[FAIL]" }
    $color = if ($result.Status -eq "PASS") { "Green" } else { "Red" }
    Write-Host "  $status $($result.Test)" -ForegroundColor $color
}

Write-Host ""
Write-Host "OVERALL RESULTS:" -ForegroundColor Cyan
Write-Host "  Total Tests: $totalCount" -ForegroundColor White
Write-Host "  Passed: $passCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor Red
Write-Host "  Success Rate: $([math]::Round(($passCount / $totalCount) * 100, 1))%" -ForegroundColor White

if ($failCount -eq 0) {
    Write-Host ""
    Write-Host "ALL CRUD TESTS PASSED!" -ForegroundColor Green
    Write-Host "AssignmentEntity Step-Focused implementation is working correctly." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "SOME TESTS FAILED!" -ForegroundColor Red
    Write-Host "Please review the failed tests and fix any issues." -ForegroundColor Red
}
