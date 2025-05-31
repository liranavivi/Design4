# Manual test for ScheduledFlowEntity referential integrity
$baseUrl = "http://localhost:5130"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

function Invoke-ApiCall {
    param($Method, $Endpoint, $Body = $null)
    try {
        $uri = "$baseUrl$Endpoint"
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $headers
            TimeoutSec = 30
        }
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
        }
        
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response; StatusCode = 200 }
    }
    catch {
        $statusCode = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { 0 }
        $errorMessage = $_.Exception.Message
        
        return @{ 
            Success = $false; 
            Error = $errorMessage; 
            StatusCode = $statusCode
        }
    }
}

Write-Host "MANUAL TEST: SCHEDULEDFLOWENTITY REFERENTIAL INTEGRITY" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

# Step 1: Get existing entities to understand the current state
Write-Host "1. Checking existing entities..." -ForegroundColor Yellow

$protocolsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/protocols"
Write-Host "   Protocols: $($protocolsResult.Data.Count)" -ForegroundColor Gray

$sourcesResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/sources"
Write-Host "   Sources: $($sourcesResult.Data.Count)" -ForegroundColor Gray

$destinationsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/destinations"
Write-Host "   Destinations: $($destinationsResult.Data.Count)" -ForegroundColor Gray

$stepsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/steps"
Write-Host "   Steps: $($stepsResult.Data.Count)" -ForegroundColor Gray

$flowsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/flows"
Write-Host "   Flows: $($flowsResult.Data.Count)" -ForegroundColor Gray

$scheduledFlowsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/scheduledflows"
Write-Host "   ScheduledFlows: $($scheduledFlowsResult.Data.Count)" -ForegroundColor Gray

$taskScheduledsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/taskscheduleds"
Write-Host "   TaskScheduleds: $($taskScheduledsResult.Data.Count)" -ForegroundColor Gray

# Step 2: Use existing entities if available, otherwise create new ones
if ($protocolsResult.Data.Count -gt 0 -and $sourcesResult.Data.Count -gt 0 -and $destinationsResult.Data.Count -gt 0 -and $stepsResult.Data.Count -gt 0 -and $flowsResult.Data.Count -gt 0) {
    Write-Host "2. Using existing entities..." -ForegroundColor Yellow
    $protocolId = $protocolsResult.Data[0].id
    $sourceId = $sourcesResult.Data[0].id
    $destinationId = $destinationsResult.Data[0].id
    $stepId = $stepsResult.Data[0].id
    $flowId = $flowsResult.Data[0].id
    
    Write-Host "   Using Protocol: $protocolId" -ForegroundColor Gray
    Write-Host "   Using Source: $sourceId" -ForegroundColor Gray
    Write-Host "   Using Destination: $destinationId" -ForegroundColor Gray
    Write-Host "   Using Step: $stepId" -ForegroundColor Gray
    Write-Host "   Using Flow: $flowId" -ForegroundColor Gray
} else {
    Write-Host "2. Not enough existing entities, need to create new ones..." -ForegroundColor Red
    Write-Host "   This test requires existing entities to avoid conflicts." -ForegroundColor Red
    exit 1
}

# Step 3: Create a new ScheduledFlow
Write-Host "3. Creating new ScheduledFlow..." -ForegroundColor Yellow
$timestamp = Get-Date -Format 'HHmmss'
$scheduledFlowData = @{
    version = "1.0"
    name = "Manual Test Scheduled Flow $timestamp"
    sourceId = $sourceId
    destinationIds = @($destinationId)
    flowId = $flowId
    description = "Manual test scheduled flow"
}
$scheduledFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
if ($scheduledFlowResult.Success) {
    $scheduledFlowId = $scheduledFlowResult.Data.id
    Write-Host "   ScheduledFlow created: $scheduledFlowId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create ScheduledFlow: $($scheduledFlowResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 4: Create a TaskScheduled that references the ScheduledFlow
Write-Host "4. Creating TaskScheduled that references the ScheduledFlow..." -ForegroundColor Yellow
$taskScheduledData = @{
    version = "1.0"
    name = "Manual Test Task Scheduled $timestamp"
    scheduledFlowId = $scheduledFlowId
    description = "Manual test task scheduled"
}
$taskScheduledResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/taskscheduleds" -Body $taskScheduledData
if ($taskScheduledResult.Success) {
    $taskScheduledId = $taskScheduledResult.Data.id
    Write-Host "   TaskScheduled created: $taskScheduledId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create TaskScheduled: $($taskScheduledResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 5: Try to delete the ScheduledFlow (should fail with 409)
Write-Host "5. Attempting to delete ScheduledFlow (should fail with 409)..." -ForegroundColor Yellow
$deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
if ($deleteResult.Success) {
    Write-Host "   ❌ ERROR: ScheduledFlow deletion succeeded when it should have failed!" -ForegroundColor Red
    Write-Host "   The referential integrity is NOT working!" -ForegroundColor Red
    $testPassed = $false
} else {
    if ($deleteResult.StatusCode -eq 409) {
        Write-Host "   ✅ SUCCESS: ScheduledFlow deletion correctly blocked (409 Conflict)" -ForegroundColor Green
        Write-Host "   The referential integrity IS working!" -ForegroundColor Green
        $testPassed = $true
    } else {
        Write-Host "   ⚠️  UNEXPECTED: Got status code $($deleteResult.StatusCode) instead of 409" -ForegroundColor Yellow
        Write-Host "   Error: $($deleteResult.Error)" -ForegroundColor Yellow
        $testPassed = $false
    }
}

# Step 6: Clean up - delete TaskScheduled first, then ScheduledFlow
Write-Host "6. Cleaning up test entities..." -ForegroundColor Yellow
$cleanupTaskResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/taskscheduleds/$taskScheduledId"
if ($cleanupTaskResult.Success) {
    Write-Host "   TaskScheduled deleted successfully" -ForegroundColor Green
    
    # Now try to delete ScheduledFlow (should succeed)
    $cleanupScheduledFlowResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
    if ($cleanupScheduledFlowResult.Success) {
        Write-Host "   ScheduledFlow deleted successfully after removing references" -ForegroundColor Green
    } else {
        Write-Host "   Failed to delete ScheduledFlow after cleanup: $($cleanupScheduledFlowResult.Error)" -ForegroundColor Yellow
    }
} else {
    Write-Host "   Failed to delete TaskScheduled: $($cleanupTaskResult.Error)" -ForegroundColor Yellow
}

Write-Host ""
if ($testPassed) {
    Write-Host "🎉 TEST PASSED: ScheduledFlowEntity referential integrity is working!" -ForegroundColor Green
} else {
    Write-Host "❌ TEST FAILED: ScheduledFlowEntity referential integrity is not working!" -ForegroundColor Red
}
Write-Host ""
Write-Host "Test completed!" -ForegroundColor Cyan
Write-Host "ScheduledFlowId: $scheduledFlowId" -ForegroundColor Cyan
Write-Host "TaskScheduledId: $taskScheduledId" -ForegroundColor Cyan
