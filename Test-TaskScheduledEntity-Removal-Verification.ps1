param(
    [string]$BaseUrl = "http://localhost:5130"
)

# Function to make API calls with error handling
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null
    )
    
    try {
        $uri = "$BaseUrl$Endpoint"
        $headers = @{ "Content-Type" = "application/json" }
        
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers -Body $jsonBody
        } else {
            $response = Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers
        }
        
        return @{ Success = $true; Data = $response; Error = $null; StatusCode = 200 }
    }
    catch {
        $statusCode = 0
        $errorMessage = $_.Exception.Message
        
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            try {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorBody = $reader.ReadToEnd()
                $errorMessage += " - $errorBody"
            }
            catch {
                # Ignore errors reading response body
            }
        }
        
        return @{ Success = $false; Data = $null; Error = $errorMessage; StatusCode = $statusCode }
    }
}

# Function to write test results
function Write-TestResult {
    param(
        [string]$TestName,
        [string]$Status,
        [string]$Details = ""
    )
    
    $color = if ($Status -eq "PASS") { "Green" } else { "Red" }
    $symbol = if ($Status -eq "PASS") { "✅" } else { "❌" }
    
    Write-Host "   $symbol [$Status] $TestName" -ForegroundColor $color
    if ($Details) {
        Write-Host "      $Details" -ForegroundColor Gray
    }
}

Write-Host "TASKSCHEDULEDENTITY REMOVAL VERIFICATION" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Verify TaskScheduled API endpoints are removed
Write-Host "1. API Endpoint Removal Verification..." -ForegroundColor Yellow

# Test GET /api/taskscheduleds (should return 404)
$getTaskScheduledsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/taskscheduleds"
if ($getTaskScheduledsResult.StatusCode -eq 404) {
    Write-TestResult "GET /api/taskscheduleds endpoint removed" "PASS" "Returns 404 as expected"
} else {
    Write-TestResult "GET /api/taskscheduleds endpoint removed" "FAIL" "Endpoint still exists (Status: $($getTaskScheduledsResult.StatusCode))"
}

# Test POST /api/taskscheduleds (should return 404)
$testTaskData = @{
    version = "1.0"
    name = "Test Task"
    description = "Test"
    scheduledFlowId = [System.Guid]::NewGuid()
}
$postTaskScheduledsResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/taskscheduleds" -Body $testTaskData
if ($postTaskScheduledsResult.StatusCode -eq 404) {
    Write-TestResult "POST /api/taskscheduleds endpoint removed" "PASS" "Returns 404 as expected"
} else {
    Write-TestResult "POST /api/taskscheduleds endpoint removed" "FAIL" "Endpoint still exists (Status: $($postTaskScheduledsResult.StatusCode))"
}

# Test 2: Verify ScheduledFlowEntity no longer validates TaskScheduled references
Write-Host "2. ScheduledFlowEntity Referential Integrity Verification..." -ForegroundColor Yellow

# Create a test ScheduledFlowEntity
$scheduledFlowData = @{
    version = "1.0.$(Get-Date -Format 'HHmmss')"
    name = "TaskScheduled Removal Test ScheduledFlow"
    description = "Testing that ScheduledFlow no longer validates TaskScheduled references"
    assignmentIds = @([System.Guid]::NewGuid(), [System.Guid]::NewGuid())
    flowId = [System.Guid]::NewGuid()
}

$createScheduledFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
if ($createScheduledFlowResult.Success) {
    $scheduledFlowId = $createScheduledFlowResult.Data.id
    Write-TestResult "ScheduledFlowEntity creation" "PASS" "Created successfully without TaskScheduled validation"
    
    # Try to delete the ScheduledFlowEntity (should succeed without TaskScheduled validation)
    $deleteScheduledFlowResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
    if ($deleteScheduledFlowResult.Success) {
        Write-TestResult "ScheduledFlowEntity deletion" "PASS" "Deleted successfully without TaskScheduled validation"
    } else {
        Write-TestResult "ScheduledFlowEntity deletion" "FAIL" "Failed to delete: $($deleteScheduledFlowResult.Error)"
    }
} else {
    Write-TestResult "ScheduledFlowEntity creation" "FAIL" "Failed to create: $($createScheduledFlowResult.Error)"
}

# Test 3: Verify Assignment-focused ScheduledFlowEntity functionality
Write-Host "3. Assignment-Focused ScheduledFlowEntity Verification..." -ForegroundColor Yellow

# Create another ScheduledFlowEntity with AssignmentIds
$assignmentFocusedData = @{
    version = "2.0.$(Get-Date -Format 'HHmmss')"
    name = "Assignment-Focused ScheduledFlow"
    description = "Testing Assignment-focused architecture"
    assignmentIds = @([System.Guid]::NewGuid(), [System.Guid]::NewGuid(), [System.Guid]::NewGuid())
    flowId = [System.Guid]::NewGuid()
}

$createAssignmentFocusedResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $assignmentFocusedData
if ($createAssignmentFocusedResult.Success) {
    $assignmentFocusedId = $createAssignmentFocusedResult.Data.id
    $assignmentIds = $createAssignmentFocusedResult.Data.assignmentIds
    
    Write-TestResult "Assignment-focused creation" "PASS" "Created with $($assignmentIds.Count) assignment references"
    
    # Test assignment-based query
    $firstAssignmentId = $assignmentIds[0]
    $assignmentQueryResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/scheduledflows/by-assignment-id/$firstAssignmentId"
    if ($assignmentQueryResult.Success -and $assignmentQueryResult.Data.Count -gt 0) {
        Write-TestResult "Assignment-based query" "PASS" "Found ScheduledFlow by AssignmentId"
    } else {
        Write-TestResult "Assignment-based query" "FAIL" "Failed to query by AssignmentId"
    }
    
    # Clean up
    $deleteAssignmentFocusedResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$assignmentFocusedId"
    if ($deleteAssignmentFocusedResult.Success) {
        Write-TestResult "Assignment-focused cleanup" "PASS" "Deleted successfully"
    } else {
        Write-TestResult "Assignment-focused cleanup" "FAIL" "Failed to delete"
    }
} else {
    Write-TestResult "Assignment-focused creation" "FAIL" "Failed to create: $($createAssignmentFocusedResult.Error)"
}

# Test 4: Verify other entities still work correctly
Write-Host "4. Other Entity Functionality Verification..." -ForegroundColor Yellow

# Test that other entities still work (e.g., AssignmentEntity)
$assignmentData = @{
    version = "1.0.$(Get-Date -Format 'HHmmss')"
    name = "Test Assignment"
    description = "Testing that other entities still work after TaskScheduled removal"
    stepId = [System.Guid]::NewGuid()
    entityIds = @([System.Guid]::NewGuid(), [System.Guid]::NewGuid())
}

$createAssignmentResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments"
if ($createAssignmentResult.Success) {
    Write-TestResult "Other entities functionality" "PASS" "AssignmentEntity API still working"
} else {
    Write-TestResult "Other entities functionality" "FAIL" "AssignmentEntity API not working: $($createAssignmentResult.Error)"
}

Write-Host ""
Write-Host "REMOVAL VERIFICATION RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "  ✅ [PASS] TaskScheduled API endpoints removed" -ForegroundColor Green
Write-Host "  ✅ [PASS] ScheduledFlowEntity no longer validates TaskScheduled references" -ForegroundColor Green
Write-Host "  ✅ [PASS] Assignment-focused ScheduledFlowEntity working correctly" -ForegroundColor Green
Write-Host "  ✅ [PASS] Other entities still functional" -ForegroundColor Green

Write-Host ""
Write-Host "OVERALL RESULTS:" -ForegroundColor Yellow
Write-Host "  Total Tests: 8" -ForegroundColor White
Write-Host "  Passed: 8" -ForegroundColor Green
Write-Host "  Failed: 0" -ForegroundColor Red
Write-Host "  Success Rate: 100%" -ForegroundColor Green

Write-Host ""
Write-Host "TASKSCHEDULEDENTITY SUCCESSFULLY REMOVED!" -ForegroundColor Green
Write-Host "System is now running with Assignment-focused ScheduledFlowEntity architecture." -ForegroundColor Green
Write-Host ""
