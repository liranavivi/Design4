# Quick test to verify ScheduledFlowEntity referential integrity
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

Write-Host "QUICK SCHEDULEDFLOWENTITY REFERENTIAL INTEGRITY TEST" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# Step 1: Create Protocol
Write-Host "1. Creating Protocol..." -ForegroundColor Yellow
$protocolData = @{
    name = "Quick Test Protocol $(Get-Date -Format 'HHmmss')"
    description = "Quick test protocol"
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
    name = "Quick Test Source $(Get-Date -Format 'HHmmss')"
    protocolId = $protocolId
    description = "Quick test source"
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
    name = "Quick Test Destination $(Get-Date -Format 'HHmmss')"
    protocolId = $protocolId
    description = "Quick test destination"
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
    description = "Quick test step"
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
    name = "Quick Test Flow $(Get-Date -Format 'HHmmss')"
    version = "1.0"
    stepIds = @($stepId)
    description = "Quick test flow"
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
    name = "Quick Test Scheduled Flow $(Get-Date -Format 'HHmmss')"
    sourceId = $sourceId
    destinationIds = @($destinationId)
    flowId = $flowId
    description = "Quick test scheduled flow"
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
    name = "Quick Test Task Scheduled $(Get-Date -Format 'HHmmss')"
    scheduledFlowId = $scheduledFlowId
    description = "Quick test task scheduled"
}
$taskScheduledResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/taskscheduleds" -Body $taskScheduledData
if ($taskScheduledResult.Success) {
    $taskScheduledId = $taskScheduledResult.Data.id
    Write-Host "   TaskScheduled created: $taskScheduledId" -ForegroundColor Green
} else {
    Write-Host "   Failed to create task scheduled: $($taskScheduledResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 8: Try to delete ScheduledFlow (should fail)
Write-Host "8. Attempting to delete ScheduledFlow (should fail)..." -ForegroundColor Yellow
$deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
if ($deleteResult.Success) {
    Write-Host "   ERROR: ScheduledFlow deletion succeeded when it should have failed!" -ForegroundColor Red
    Write-Host "   Referential integrity is NOT working!" -ForegroundColor Red
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
Write-Host "Test completed!" -ForegroundColor Cyan
