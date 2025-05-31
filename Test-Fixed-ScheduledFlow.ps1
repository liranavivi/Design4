# Test the fixed ScheduledFlowEntity referential integrity
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

Write-Host "TESTING FIXED SCHEDULEDFLOWENTITY REFERENTIAL INTEGRITY" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan

# Step 1: Clean up orphaned TaskScheduled entity
Write-Host "1. Cleaning up orphaned TaskScheduled entity..." -ForegroundColor Yellow
$orphanedTaskId = "a0e5cf19-fbc9-4fb2-b8dd-c04858d60d76"
$deleteOrphanResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/taskscheduleds/$orphanedTaskId"
if ($deleteOrphanResult.Success) {
    Write-Host "   Orphaned TaskScheduled deleted successfully" -ForegroundColor Green
} else {
    Write-Host "   Failed to delete orphaned TaskScheduled (may not exist): $($deleteOrphanResult.Error)" -ForegroundColor Yellow
}

# Step 2: Create Protocol
Write-Host "2. Creating Protocol..." -ForegroundColor Yellow
$protocolData = @{
    name = "Fixed Test Protocol $(Get-Date -Format 'HHmmss')"
    description = "Fixed test protocol"
}
$protocolResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/protocols" -Body $protocolData
if ($protocolResult.Success) {
    $protocolId = $protocolResult.Data.id
    Write-Host "   Protocol created: $protocolId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create protocol: $($protocolResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 3: Create Source
Write-Host "3. Creating Source..." -ForegroundColor Yellow
$sourceData = @{
    address = "test://fixed-source.local"
    version = "1.0"
    name = "Fixed Test Source $(Get-Date -Format 'HHmmss')"
    protocolId = $protocolId
    description = "Fixed test source"
}
$sourceResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/sources" -Body $sourceData
if ($sourceResult.Success) {
    $sourceId = $sourceResult.Data.id
    Write-Host "   Source created: $sourceId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create source: $($sourceResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 4: Create Destination
Write-Host "4. Creating Destination..." -ForegroundColor Yellow
$destinationData = @{
    address = "test://fixed-destination.local"
    version = "1.0"
    name = "Fixed Test Destination $(Get-Date -Format 'HHmmss')"
    protocolId = $protocolId
    description = "Fixed test destination"
}
$destinationResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/destinations" -Body $destinationData
if ($destinationResult.Success) {
    $destinationId = $destinationResult.Data.id
    Write-Host "   Destination created: $destinationId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create destination: $($destinationResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 5: Create Step
Write-Host "5. Creating Step..." -ForegroundColor Yellow
$stepData = @{
    entityId = $sourceId
    nextStepIds = @()
    description = "Fixed test step"
}
$stepResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/steps" -Body $stepData
if ($stepResult.Success) {
    $stepId = $stepResult.Data.id
    Write-Host "   Step created: $stepId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create step: $($stepResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 6: Create Flow
Write-Host "6. Creating Flow..." -ForegroundColor Yellow
$flowData = @{
    name = "Fixed Test Flow $(Get-Date -Format 'HHmmss')"
    version = "1.0"
    stepIds = @($stepId)
    description = "Fixed test flow"
}
$flowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/flows" -Body $flowData
if ($flowResult.Success) {
    $flowId = $flowResult.Data.id
    Write-Host "   Flow created: $flowId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create flow: $($flowResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 7: Create ScheduledFlow
Write-Host "7. Creating ScheduledFlow..." -ForegroundColor Yellow
$scheduledFlowData = @{
    version = "1.0"
    name = "Fixed Test Scheduled Flow $(Get-Date -Format 'HHmmss')"
    sourceId = $sourceId
    destinationIds = @($destinationId)
    flowId = $flowId
    description = "Fixed test scheduled flow"
}
$scheduledFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
if ($scheduledFlowResult.Success) {
    $scheduledFlowId = $scheduledFlowResult.Data.id
    Write-Host "   ScheduledFlow created: $scheduledFlowId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create scheduled flow: $($scheduledFlowResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 8: Create TaskScheduled
Write-Host "8. Creating TaskScheduled..." -ForegroundColor Yellow
$taskScheduledData = @{
    version = "1.0"
    name = "Fixed Test Task Scheduled $(Get-Date -Format 'HHmmss')"
    scheduledFlowId = $scheduledFlowId
    description = "Fixed test task scheduled"
}
$taskScheduledResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/taskscheduleds" -Body $taskScheduledData
if ($taskScheduledResult.Success) {
    $taskScheduledId = $taskScheduledResult.Data.id
    Write-Host "   TaskScheduled created: $taskScheduledId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create task scheduled: $($taskScheduledResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 9: Try to delete ScheduledFlow (should fail now)
Write-Host "9. Attempting to delete ScheduledFlow (should fail with 409)..." -ForegroundColor Yellow
$deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
if ($deleteResult.Success) {
    Write-Host "   ❌ ERROR: ScheduledFlow deletion succeeded when it should have failed!" -ForegroundColor Red
    Write-Host "   The fix did not work - referential integrity is still broken!" -ForegroundColor Red
} else {
    if ($deleteResult.StatusCode -eq 409) {
        Write-Host "   ✅ SUCCESS: ScheduledFlow deletion correctly blocked (409 Conflict)" -ForegroundColor Green
        Write-Host "   The fix worked! Referential integrity is now working!" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  UNEXPECTED: Got status code $($deleteResult.StatusCode) instead of 409" -ForegroundColor Yellow
        Write-Host "   Error: $($deleteResult.Error)" -ForegroundColor Yellow
    }
}

# Step 10: Clean up - delete TaskScheduled first, then ScheduledFlow
Write-Host "10. Cleaning up test entities..." -ForegroundColor Yellow
$cleanupTaskResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/taskscheduleds/$taskScheduledId"
if ($cleanupTaskResult.Success) {
    Write-Host "    TaskScheduled deleted successfully" -ForegroundColor Green
    
    # Now try to delete ScheduledFlow (should succeed)
    $cleanupScheduledFlowResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
    if ($cleanupScheduledFlowResult.Success) {
        Write-Host "    ScheduledFlow deleted successfully after removing references" -ForegroundColor Green
    } else {
        Write-Host "    Failed to delete ScheduledFlow after cleanup: $($cleanupScheduledFlowResult.Error)" -ForegroundColor Yellow
    }
} else {
    Write-Host "    Failed to delete TaskScheduled: $($cleanupTaskResult.Error)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Cyan
Write-Host "ScheduledFlowId: $scheduledFlowId" -ForegroundColor Cyan
Write-Host "TaskScheduledId: $taskScheduledId" -ForegroundColor Cyan
