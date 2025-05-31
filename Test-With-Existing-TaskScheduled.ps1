# Test the fix using the existing orphaned TaskScheduled entity
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

Write-Host "TESTING FIXED REFERENTIAL INTEGRITY WITH EXISTING TASKSCHEDULED" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

# First, let's check if there are any TaskScheduled entities
Write-Host "1. Checking existing TaskScheduled entities..." -ForegroundColor Yellow
$getAllTasksResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/taskscheduleds"
if ($getAllTasksResult.Success) {
    Write-Host "   Found $($getAllTasksResult.Data.Count) TaskScheduled entities:" -ForegroundColor Green
    foreach ($task in $getAllTasksResult.Data) {
        Write-Host "     ID: $($task.id), ScheduledFlowId: $($task.scheduledFlowId), Name: $($task.name)" -ForegroundColor Gray
    }
    
    if ($getAllTasksResult.Data.Count -eq 0) {
        Write-Host "   No TaskScheduled entities found. Creating a test scenario..." -ForegroundColor Yellow
        
        # Create the full chain for testing
        $protocolData = @{
            name = "Test Protocol $(Get-Date -Format 'HHmmss')"
            description = "Test protocol"
        }
        $protocolResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/protocols" -Body $protocolData
        $protocolId = $protocolResult.Data.id
        
        $sourceData = @{
            address = "test://source-$(Get-Date -Format 'HHmmss').local"
            version = "1.0"
            name = "Test Source $(Get-Date -Format 'HHmmss')"
            protocolId = $protocolId
            description = "Test source"
        }
        $sourceResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/sources" -Body $sourceData
        $sourceId = $sourceResult.Data.id
        
        $destinationData = @{
            address = "test://destination-$(Get-Date -Format 'HHmmss').local"
            version = "1.0"
            name = "Test Destination $(Get-Date -Format 'HHmmss')"
            protocolId = $protocolId
            description = "Test destination"
        }
        $destinationResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/destinations" -Body $destinationData
        $destinationId = $destinationResult.Data.id
        
        $stepData = @{
            entityId = $sourceId
            nextStepIds = @()
            description = "Test step"
        }
        $stepResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/steps" -Body $stepData
        $stepId = $stepResult.Data.id
        
        $flowData = @{
            name = "Test Flow $(Get-Date -Format 'HHmmss')"
            version = "1.0"
            stepIds = @($stepId)
            description = "Test flow"
        }
        $flowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/flows" -Body $flowData
        $flowId = $flowResult.Data.id
        
        $scheduledFlowData = @{
            version = "1.0"
            name = "Test Scheduled Flow $(Get-Date -Format 'HHmmss')"
            sourceId = $sourceId
            destinationIds = @($destinationId)
            flowId = $flowId
            description = "Test scheduled flow"
        }
        $scheduledFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
        $scheduledFlowId = $scheduledFlowResult.Data.id
        
        $taskScheduledData = @{
            version = "1.0"
            name = "Test Task Scheduled $(Get-Date -Format 'HHmmss')"
            scheduledFlowId = $scheduledFlowId
            description = "Test task scheduled"
        }
        $taskScheduledResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/taskscheduleds" -Body $taskScheduledData
        $taskScheduledId = $taskScheduledResult.Data.id
        
        Write-Host "   Created test entities:" -ForegroundColor Green
        Write-Host "     ScheduledFlowId: $scheduledFlowId" -ForegroundColor Gray
        Write-Host "     TaskScheduledId: $taskScheduledId" -ForegroundColor Gray
    } else {
        # Use the first existing TaskScheduled entity
        $existingTask = $getAllTasksResult.Data[0]
        $taskScheduledId = $existingTask.id
        $referencedScheduledFlowId = $existingTask.scheduledFlowId
        
        Write-Host "   Using existing TaskScheduled: $taskScheduledId" -ForegroundColor Green
        Write-Host "   References ScheduledFlowId: $referencedScheduledFlowId" -ForegroundColor Gray
        
        # Check if the referenced ScheduledFlow exists
        $getScheduledFlowResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/scheduledflows/$referencedScheduledFlowId"
        if ($getScheduledFlowResult.Success) {
            Write-Host "   Referenced ScheduledFlow exists - perfect for testing!" -ForegroundColor Green
            $scheduledFlowId = $referencedScheduledFlowId
        } else {
            Write-Host "   Referenced ScheduledFlow does not exist - need to create one with the same ID" -ForegroundColor Yellow
            Write-Host "   This is not possible with auto-generated IDs, so we'll test deletion of the orphaned TaskScheduled" -ForegroundColor Yellow
            
            # Delete the orphaned TaskScheduled and exit
            $deleteOrphanResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/taskscheduleds/$taskScheduledId"
            if ($deleteOrphanResult.Success) {
                Write-Host "   Orphaned TaskScheduled deleted successfully" -ForegroundColor Green
            }
            Write-Host "   Please run the test again to create a fresh test scenario" -ForegroundColor Yellow
            exit 0
        }
    }
} else {
    Write-Host "   Failed to get TaskScheduled entities: $($getAllTasksResult.Error)" -ForegroundColor Red
    exit 1
}

# Step 2: Try to delete the ScheduledFlow (should fail with 409)
Write-Host "2. Attempting to delete ScheduledFlow (should fail with 409)..." -ForegroundColor Yellow
$deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
if ($deleteResult.Success) {
    Write-Host "   ❌ ERROR: ScheduledFlow deletion succeeded when it should have failed!" -ForegroundColor Red
    Write-Host "   The fix did not work - referential integrity is still broken!" -ForegroundColor Red
    $testPassed = $false
} else {
    if ($deleteResult.StatusCode -eq 409) {
        Write-Host "   ✅ SUCCESS: ScheduledFlow deletion correctly blocked (409 Conflict)" -ForegroundColor Green
        Write-Host "   The fix worked! Referential integrity is now working!" -ForegroundColor Green
        $testPassed = $true
    } else {
        Write-Host "   ⚠️  UNEXPECTED: Got status code $($deleteResult.StatusCode) instead of 409" -ForegroundColor Yellow
        Write-Host "   Error: $($deleteResult.Error)" -ForegroundColor Yellow
        $testPassed = $false
    }
}

Write-Host ""
if ($testPassed) {
    Write-Host "🎉 TEST PASSED: ScheduledFlowEntity referential integrity is working!" -ForegroundColor Green
} else {
    Write-Host "❌ TEST FAILED: ScheduledFlowEntity referential integrity is not working!" -ForegroundColor Red
}
Write-Host ""
Write-Host "Test completed!" -ForegroundColor Cyan
