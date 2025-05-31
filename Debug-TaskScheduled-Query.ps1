# Debug script to check TaskScheduledEntity references
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

Write-Host "DEBUG: TASKSCHEDULEDENTITY QUERY TEST" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Step 1: Create Protocol
Write-Host "1. Creating Protocol..." -ForegroundColor Yellow
$protocolData = @{
    name = "Debug Protocol $(Get-Date -Format 'HHmmss')"
    description = "Debug protocol"
}
$protocolResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/protocols" -Body $protocolData
if ($protocolResult.Success) {
    $protocolId = $protocolResult.Data.id
    Write-Host "   Protocol created: $protocolId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create protocol: $($protocolResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 2: Create Source
Write-Host "2. Creating Source..." -ForegroundColor Yellow
$sourceData = @{
    address = "test://source.local"
    version = "1.0"
    name = "Debug Source $(Get-Date -Format 'HHmmss')"
    protocolId = $protocolId
    description = "Debug source"
}
$sourceResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/sources" -Body $sourceData
if ($sourceResult.Success) {
    $sourceId = $sourceResult.Data.id
    Write-Host "   Source created: $sourceId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create source: $($sourceResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 3: Create Destination
Write-Host "3. Creating Destination..." -ForegroundColor Yellow
$destinationData = @{
    address = "test://destination.local"
    version = "1.0"
    name = "Debug Destination $(Get-Date -Format 'HHmmss')"
    protocolId = $protocolId
    description = "Debug destination"
}
$destinationResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/destinations" -Body $destinationData
if ($destinationResult.Success) {
    $destinationId = $destinationResult.Data.id
    Write-Host "   Destination created: $destinationId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create destination: $($destinationResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 4: Create Step
Write-Host "4. Creating Step..." -ForegroundColor Yellow
$stepData = @{
    entityId = $sourceId
    nextStepIds = @()
    description = "Debug step"
}
$stepResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/steps" -Body $stepData
if ($stepResult.Success) {
    $stepId = $stepResult.Data.id
    Write-Host "   Step created: $stepId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create step: $($stepResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 5: Create Flow
Write-Host "5. Creating Flow..." -ForegroundColor Yellow
$flowData = @{
    name = "Debug Flow $(Get-Date -Format 'HHmmss')"
    version = "1.0"
    stepIds = @($stepId)
    description = "Debug flow"
}
$flowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/flows" -Body $flowData
if ($flowResult.Success) {
    $flowId = $flowResult.Data.id
    Write-Host "   Flow created: $flowId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create flow: $($flowResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 6: Create ScheduledFlow
Write-Host "6. Creating ScheduledFlow..." -ForegroundColor Yellow
$scheduledFlowData = @{
    version = "1.0"
    name = "Debug Scheduled Flow $(Get-Date -Format 'HHmmss')"
    sourceId = $sourceId
    destinationIds = @($destinationId)
    flowId = $flowId
    description = "Debug scheduled flow"
}
$scheduledFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
if ($scheduledFlowResult.Success) {
    $scheduledFlowId = $scheduledFlowResult.Data.id
    Write-Host "   ScheduledFlow created: $scheduledFlowId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create scheduled flow: $($scheduledFlowResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 7: Create TaskScheduled
Write-Host "7. Creating TaskScheduled..." -ForegroundColor Yellow
$taskScheduledData = @{
    version = "1.0"
    name = "Debug Task Scheduled $(Get-Date -Format 'HHmmss')"
    scheduledFlowId = $scheduledFlowId
    description = "Debug task scheduled"
}
$taskScheduledResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/taskscheduleds" -Body $taskScheduledData
if ($taskScheduledResult.Success) {
    $taskScheduledId = $taskScheduledResult.Data.id
    Write-Host "   TaskScheduled created: $taskScheduledId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create task scheduled: $($taskScheduledResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 8: Get all TaskScheduled entities to verify
Write-Host "8. Getting all TaskScheduled entities..." -ForegroundColor Yellow
$getAllResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/taskscheduleds"
if ($getAllResult.Success) {
    Write-Host "   Found $($getAllResult.Data.Count) TaskScheduled entities:" -ForegroundColor Green
    foreach ($task in $getAllResult.Data) {
        Write-Host "     ID: $($task.id), ScheduledFlowId: $($task.scheduledFlowId), Name: $($task.name)" -ForegroundColor Gray
        if ($task.scheduledFlowId -eq $scheduledFlowId) {
            Write-Host "     ✅ MATCH: This TaskScheduled references our ScheduledFlow!" -ForegroundColor Green
        }
    }
} else {
    Write-Host "   Failed to get TaskScheduled entities: $($getAllResult.Error)" -ForegroundColor Red
}

# Step 9: Try to delete ScheduledFlow (should fail)
Write-Host "9. Attempting to delete ScheduledFlow (should fail)..." -ForegroundColor Yellow
$deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
if ($deleteResult.Success) {
    Write-Host "   ERROR: ScheduledFlow deletion succeeded when it should have failed!" -ForegroundColor Red
    Write-Host "   This means the referential integrity query is not working correctly!" -ForegroundColor Red
} else {
    if ($deleteResult.StatusCode -eq 409) {
        Write-Host "   SUCCESS: ScheduledFlow deletion correctly blocked (409 Conflict)" -ForegroundColor Green
        Write-Host "   Referential integrity is working!" -ForegroundColor Green
    } else {
        Write-Host "   UNEXPECTED: Got status code $($deleteResult.StatusCode) instead of 409" -ForegroundColor Red
        Write-Host "   Error: $($deleteResult.Error)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Debug test completed!" -ForegroundColor Cyan
Write-Host "ScheduledFlowId: $scheduledFlowId" -ForegroundColor Cyan
Write-Host "TaskScheduledId: $taskScheduledId" -ForegroundColor Cyan
