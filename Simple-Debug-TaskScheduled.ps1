# Simple debug script to check TaskScheduledEntity references
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

Write-Host "SIMPLE DEBUG: TASKSCHEDULEDENTITY QUERY" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

# From the previous test, we know these IDs:
$scheduledFlowId = "d8956100-3c43-41b2-8569-bb3d082e0eb9"
$taskScheduledId = "a0e5cf19-fbc9-4fb2-b8dd-c04858d60d76"

Write-Host "Using known IDs from previous test:" -ForegroundColor Yellow
Write-Host "  ScheduledFlowId: $scheduledFlowId" -ForegroundColor Gray
Write-Host "  TaskScheduledId: $taskScheduledId" -ForegroundColor Gray
Write-Host ""

# Step 1: Check if the TaskScheduled entity still exists
Write-Host "1. Checking if TaskScheduled entity exists..." -ForegroundColor Yellow
$getTaskResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/taskscheduleds/$taskScheduledId"
if ($getTaskResult.Success) {
    Write-Host "   ✅ TaskScheduled entity exists:" -ForegroundColor Green
    Write-Host "     ID: $($getTaskResult.Data.id)" -ForegroundColor Gray
    Write-Host "     Name: $($getTaskResult.Data.name)" -ForegroundColor Gray
    Write-Host "     ScheduledFlowId: $($getTaskResult.Data.scheduledFlowId)" -ForegroundColor Gray
    Write-Host "     Version: $($getTaskResult.Data.version)" -ForegroundColor Gray
    
    if ($getTaskResult.Data.scheduledFlowId -eq $scheduledFlowId) {
        Write-Host "   ✅ TaskScheduled correctly references the ScheduledFlow!" -ForegroundColor Green
    } else {
        Write-Host "   ❌ TaskScheduled references wrong ScheduledFlow!" -ForegroundColor Red
        Write-Host "     Expected: $scheduledFlowId" -ForegroundColor Red
        Write-Host "     Actual: $($getTaskResult.Data.scheduledFlowId)" -ForegroundColor Red
    }
} else {
    Write-Host "   ❌ TaskScheduled entity does not exist (probably deleted in previous test)" -ForegroundColor Red
    Write-Host "   Error: $($getTaskResult.Error)" -ForegroundColor Red
}

Write-Host ""

# Step 2: Check if the ScheduledFlow entity still exists
Write-Host "2. Checking if ScheduledFlow entity exists..." -ForegroundColor Yellow
$getScheduledFlowResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/scheduledflows/$scheduledFlowId"
if ($getScheduledFlowResult.Success) {
    Write-Host "   ✅ ScheduledFlow entity exists:" -ForegroundColor Green
    Write-Host "     ID: $($getScheduledFlowResult.Data.id)" -ForegroundColor Gray
    Write-Host "     Name: $($getScheduledFlowResult.Data.name)" -ForegroundColor Gray
    Write-Host "     Version: $($getScheduledFlowResult.Data.version)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ ScheduledFlow entity does not exist (probably deleted in previous test)" -ForegroundColor Red
    Write-Host "   Error: $($getScheduledFlowResult.Error)" -ForegroundColor Red
}

Write-Host ""

# Step 3: Get all TaskScheduled entities to see what's in the database
Write-Host "3. Getting all TaskScheduled entities..." -ForegroundColor Yellow
$getAllResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/taskscheduleds"
if ($getAllResult.Success) {
    Write-Host "   Found $($getAllResult.Data.Count) TaskScheduled entities:" -ForegroundColor Green
    if ($getAllResult.Data.Count -eq 0) {
        Write-Host "     No TaskScheduled entities found in database" -ForegroundColor Gray
    } else {
        foreach ($task in $getAllResult.Data) {
            Write-Host "     ID: $($task.id)" -ForegroundColor Gray
            Write-Host "       Name: $($task.name)" -ForegroundColor Gray
            Write-Host "       ScheduledFlowId: $($task.scheduledFlowId)" -ForegroundColor Gray
            Write-Host "       Version: $($task.version)" -ForegroundColor Gray
            Write-Host ""
        }
    }
} else {
    Write-Host "   Failed to get TaskScheduled entities: $($getAllResult.Error)" -ForegroundColor Red
}

Write-Host ""

# Step 4: Get all ScheduledFlow entities to see what's in the database
Write-Host "4. Getting all ScheduledFlow entities..." -ForegroundColor Yellow
$getAllScheduledFlowsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/scheduledflows"
if ($getAllScheduledFlowsResult.Success) {
    Write-Host "   Found $($getAllScheduledFlowsResult.Data.Count) ScheduledFlow entities:" -ForegroundColor Green
    if ($getAllScheduledFlowsResult.Data.Count -eq 0) {
        Write-Host "     No ScheduledFlow entities found in database" -ForegroundColor Gray
    } else {
        foreach ($flow in $getAllScheduledFlowsResult.Data) {
            Write-Host "     ID: $($flow.id)" -ForegroundColor Gray
            Write-Host "       Name: $($flow.name)" -ForegroundColor Gray
            Write-Host "       Version: $($flow.version)" -ForegroundColor Gray
            Write-Host ""
        }
    }
} else {
    Write-Host "   Failed to get ScheduledFlow entities: $($getAllScheduledFlowsResult.Error)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Debug completed!" -ForegroundColor Cyan
