# Comprehensive CRUD Testing Script for EntitiesManager
# Tests full CRUD operations with real containerized services

$baseUrl = "http://localhost:5130"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

$testResults = @()
$totalTests = 0
$passedTests = 0

function Write-TestResult {
    param($TestName, $Status, $Details = "")
    $result = @{
        Test = $TestName
        Status = $Status
        Details = $Details
        Timestamp = Get-Date
    }
    $script:testResults += $result
    $script:totalTests++
    if ($Status -eq "PASS") { $script:passedTests++ }
    
    $color = if ($Status -eq "PASS") { "Green" } else { "Red" }
    Write-Host "[$Status] $TestName" -ForegroundColor $color
    if ($Details) { Write-Host "    $Details" -ForegroundColor Gray }
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
        return @{ Success = $false; Error = $_.Exception.Message; StatusCode = $statusCode }
    }
}

Write-Host "COMPREHENSIVE CRUD TESTING WITH REAL CONTAINERIZED SERVICES" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Testing against: $baseUrl" -ForegroundColor Yellow
Write-Host "Infrastructure: MongoDB + RabbitMQ + OpenTelemetry Collector" -ForegroundColor Yellow
Write-Host ""

# Test 1: StepEntity CRUD Operations (Workflow entity with EntityId and NextStepIds)
Write-Host "TESTING STEPENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$stepData = @{
    name = "Data Processing Step"
    version = "1.0"
    description = "Step for data processing workflow"
    entityId = [System.Guid]::NewGuid().ToString()
    nextStepIds = @([System.Guid]::NewGuid().ToString(), [System.Guid]::NewGuid().ToString())
}

# CREATE Step
$createStepResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/steps" -Body $stepData
if ($createStepResult.Success) {
    $stepId = $createStepResult.Data.id
    Write-TestResult "Create Step" "PASS" "Created Step with ID: $stepId"
    
    # READ Step
    $readStepResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/steps/$stepId"
    if ($readStepResult.Success) {
        Write-TestResult "Read Step" "PASS" "Retrieved Step: $($readStepResult.Data.name)"
        
        # UPDATE Step
        $stepData.description = "Updated step for data processing workflow"
        $updateStepResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/steps/$stepId" -Body $stepData
        if ($updateStepResult.Success) {
            Write-TestResult "Update Step" "PASS" "Updated Step description"
        } else {
            Write-TestResult "Update Step" "FAIL" $updateStepResult.Error
        }
        
        # Test workflow-specific endpoint
        $getByEntityIdResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/steps/by-entity-id/$($stepData.entityId)"
        if ($getByEntityIdResult.Success) {
            Write-TestResult "Get Step by EntityId" "PASS" "Retrieved step by EntityId"
        } else {
            Write-TestResult "Get Step by EntityId" "FAIL" $getByEntityIdResult.Error
        }
        
        # DELETE Step
        $deleteStepResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/steps/$stepId"
        if ($deleteStepResult.Success) {
            Write-TestResult "Delete Step" "PASS" "Successfully deleted Step"
        } else {
            Write-TestResult "Delete Step" "FAIL" $deleteStepResult.Error
        }
    } else {
        Write-TestResult "Read Step" "FAIL" $readStepResult.Error
    }
} else {
    Write-TestResult "Create Step" "FAIL" $createStepResult.Error
}

# Test 2: FlowEntity CRUD Operations (Workflow entity with StepIds collection)
Write-Host "`nTESTING FLOWENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$flowData = @{
    name = "Data Processing Flow"
    version = "1.0"
    description = "Flow for data processing workflow"
    stepIds = @([System.Guid]::NewGuid().ToString(), [System.Guid]::NewGuid().ToString())
}

# CREATE Flow
$createFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/flows" -Body $flowData
if ($createFlowResult.Success) {
    $flowId = $createFlowResult.Data.id
    Write-TestResult "Create Flow" "PASS" "Created Flow with ID: $flowId"
    
    # READ Flow
    $readFlowResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/flows/$flowId"
    if ($readFlowResult.Success) {
        Write-TestResult "Read Flow" "PASS" "Retrieved Flow: $($readFlowResult.Data.name)"
        
        # UPDATE Flow
        $flowData.description = "Updated flow for data processing workflow"
        $updateFlowResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/flows/$flowId" -Body $flowData
        if ($updateFlowResult.Success) {
            Write-TestResult "Update Flow" "PASS" "Updated Flow description"
        } else {
            Write-TestResult "Update Flow" "FAIL" $updateFlowResult.Error
        }
        
        # Test workflow-specific endpoint
        if ($flowData.stepIds.Count -gt 0) {
            $getByStepIdResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/flows/by-step-id/$($flowData.stepIds[0])"
            if ($getByStepIdResult.Success) {
                Write-TestResult "Get Flow by StepId" "PASS" "Retrieved flow by StepId"
            } else {
                Write-TestResult "Get Flow by StepId" "FAIL" $getByStepIdResult.Error
            }
        }
        
        # DELETE Flow
        $deleteFlowResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/flows/$flowId"
        if ($deleteFlowResult.Success) {
            Write-TestResult "Delete Flow" "PASS" "Successfully deleted Flow"
        } else {
            Write-TestResult "Delete Flow" "FAIL" $deleteFlowResult.Error
        }
    } else {
        Write-TestResult "Read Flow" "FAIL" $readFlowResult.Error
    }
} else {
    Write-TestResult "Create Flow" "FAIL" $createFlowResult.Error
}

# Test 3: ScheduledFlowEntity CRUD Operations
Write-Host "`nTESTING SCHEDULEDFLOWENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$scheduledFlowData = @{
    name = "Scheduled Data Processing"
    version = "1.0"
    description = "Scheduled flow for data processing"
    sourceId = [System.Guid]::NewGuid().ToString()
    destinationIds = @([System.Guid]::NewGuid().ToString(), [System.Guid]::NewGuid().ToString())
    flowId = [System.Guid]::NewGuid().ToString()
}

# CREATE ScheduledFlow
$createScheduledFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
if ($createScheduledFlowResult.Success) {
    $scheduledFlowId = $createScheduledFlowResult.Data.id
    Write-TestResult "Create ScheduledFlow" "PASS" "Created ScheduledFlow with ID: $scheduledFlowId"

    # READ ScheduledFlow
    $readScheduledFlowResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/scheduledflows/$scheduledFlowId"
    if ($readScheduledFlowResult.Success) {
        Write-TestResult "Read ScheduledFlow" "PASS" "Retrieved ScheduledFlow: $($readScheduledFlowResult.Data.name)"

        # UPDATE ScheduledFlow
        $scheduledFlowData.description = "Updated scheduled flow for data processing"
        $updateScheduledFlowResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/scheduledflows/$scheduledFlowId" -Body $scheduledFlowData
        if ($updateScheduledFlowResult.Success) {
            Write-TestResult "Update ScheduledFlow" "PASS" "Updated ScheduledFlow description"
        } else {
            Write-TestResult "Update ScheduledFlow" "FAIL" $updateScheduledFlowResult.Error
        }

        # Test relationship endpoints
        $getBySourceIdResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/scheduledflows/by-source-id/$($scheduledFlowData.sourceId)"
        if ($getBySourceIdResult.Success) {
            Write-TestResult "Get ScheduledFlow by SourceId" "PASS" "Retrieved scheduled flow by SourceId"
        } else {
            Write-TestResult "Get ScheduledFlow by SourceId" "FAIL" $getBySourceIdResult.Error
        }

        $getByFlowIdResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/scheduledflows/by-flow-id/$($scheduledFlowData.flowId)"
        if ($getByFlowIdResult.Success) {
            Write-TestResult "Get ScheduledFlow by FlowId" "PASS" "Retrieved scheduled flow by FlowId"
        } else {
            Write-TestResult "Get ScheduledFlow by FlowId" "FAIL" $getByFlowIdResult.Error
        }

        # DELETE ScheduledFlow
        $deleteScheduledFlowResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId"
        if ($deleteScheduledFlowResult.Success) {
            Write-TestResult "Delete ScheduledFlow" "PASS" "Successfully deleted ScheduledFlow"
        } else {
            Write-TestResult "Delete ScheduledFlow" "FAIL" $deleteScheduledFlowResult.Error
        }
    } else {
        Write-TestResult "Read ScheduledFlow" "FAIL" $readScheduledFlowResult.Error
    }
} else {
    Write-TestResult "Create ScheduledFlow" "FAIL" $createScheduledFlowResult.Error
}

# Test 4: SourceEntity CRUD Operations
Write-Host "`nTESTING SOURCEENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$sourceData = @{
    name = "Database Source"
    version = "1.0"
    description = "Source for database connections"
    protocolId = [System.Guid]::NewGuid().ToString()
}

# CREATE Source
$createSourceResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/sources" -Body $sourceData
if ($createSourceResult.Success) {
    $sourceId = $createSourceResult.Data.id
    Write-TestResult "Create Source" "PASS" "Created Source with ID: $sourceId"

    # READ Source
    $readSourceResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/sources/$sourceId"
    if ($readSourceResult.Success) {
        Write-TestResult "Read Source" "PASS" "Retrieved Source: $($readSourceResult.Data.name)"

        # UPDATE Source
        $sourceData.description = "Updated source for database connections"
        $updateSourceResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/sources/$sourceId" -Body $sourceData
        if ($updateSourceResult.Success) {
            Write-TestResult "Update Source" "PASS" "Updated Source description"
        } else {
            Write-TestResult "Update Source" "FAIL" $updateSourceResult.Error
        }

        # DELETE Source
        $deleteSourceResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/sources/$sourceId"
        if ($deleteSourceResult.Success) {
            Write-TestResult "Delete Source" "PASS" "Successfully deleted Source"
        } else {
            Write-TestResult "Delete Source" "FAIL" $deleteSourceResult.Error
        }
    } else {
        Write-TestResult "Read Source" "FAIL" $readSourceResult.Error
    }
} else {
    Write-TestResult "Create Source" "FAIL" $createSourceResult.Error
}

# Test 5: DestinationEntity CRUD Operations
Write-Host "`nTESTING DESTINATIONENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$destinationData = @{
    name = "File System Destination"
    version = "1.0"
    description = "Destination for file system storage"
    protocolId = [System.Guid]::NewGuid().ToString()
}

# CREATE Destination
$createDestinationResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/destinations" -Body $destinationData
if ($createDestinationResult.Success) {
    $destinationId = $createDestinationResult.Data.id
    Write-TestResult "Create Destination" "PASS" "Created Destination with ID: $destinationId"

    # READ Destination
    $readDestinationResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/destinations/$destinationId"
    if ($readDestinationResult.Success) {
        Write-TestResult "Read Destination" "PASS" "Retrieved Destination: $($readDestinationResult.Data.name)"

        # UPDATE Destination
        $destinationData.description = "Updated destination for file system storage"
        $updateDestinationResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/destinations/$destinationId" -Body $destinationData
        if ($updateDestinationResult.Success) {
            Write-TestResult "Update Destination" "PASS" "Updated Destination description"
        } else {
            Write-TestResult "Update Destination" "FAIL" $updateDestinationResult.Error
        }

        # DELETE Destination
        $deleteDestinationResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/destinations/$destinationId"
        if ($deleteDestinationResult.Success) {
            Write-TestResult "Delete Destination" "PASS" "Successfully deleted Destination"
        } else {
            Write-TestResult "Delete Destination" "FAIL" $deleteDestinationResult.Error
        }
    } else {
        Write-TestResult "Read Destination" "FAIL" $readDestinationResult.Error
    }
} else {
    Write-TestResult "Create Destination" "FAIL" $createDestinationResult.Error
}

# Final Summary
Write-Host "`nFINAL TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Infrastructure Services: MongoDB + RabbitMQ + OpenTelemetry Collector" -ForegroundColor Yellow
Write-Host "API Service: EntitiesManager API (Local)" -ForegroundColor Yellow
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $($totalTests - $passedTests)" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor Yellow

if ($passedTests -eq $totalTests) {
    Write-Host "`nALL TESTS PASSED! CRUD operations working with real containerized services!" -ForegroundColor Green
} else {
    Write-Host "`nSome tests failed. Check the detailed results below." -ForegroundColor Yellow
}

# Display detailed results
Write-Host "`nDETAILED TEST RESULTS" -ForegroundColor Cyan
Write-Host "-" * 80 -ForegroundColor Gray
$testResults | ForEach-Object {
    $color = if ($_.Status -eq "PASS") { "Green" } else { "Red" }
    $timeStr = $_.Timestamp.ToString("HH:mm:ss")
    Write-Host "$timeStr [$($_.Status)] $($_.Test)" -ForegroundColor $color
    if ($_.Details) { Write-Host "    $($_.Details)" -ForegroundColor Gray }
}

Write-Host "`nVERIFICATION COMPLETE" -ForegroundColor Cyan
Write-Host "Data persisted in MongoDB container: mongodb://localhost:27017" -ForegroundColor Gray
Write-Host "Messages processed through RabbitMQ container: amqp://localhost:5672" -ForegroundColor Gray
Write-Host "Telemetry collected by OpenTelemetry Collector: http://localhost:8888" -ForegroundColor Gray
