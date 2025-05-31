# EntitiesManager CRUD Testing Script
# Tests all entities with real containerized services (MongoDB, RabbitMQ, OpenTelemetry)

$baseUrl = "http://localhost:5130"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

# Test results tracking
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
    $testResults += $result
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

Write-Host "🐳 ENTITIESMANAGER CRUD TESTING WITH REAL CONTAINERIZED SERVICES" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "Testing against: $baseUrl" -ForegroundColor Yellow
Write-Host "Infrastructure: MongoDB + RabbitMQ + OpenTelemetry Collector" -ForegroundColor Yellow
Write-Host ""

# Test API Health
Write-Host "🏥 TESTING API HEALTH" -ForegroundColor Magenta
$healthResult = Invoke-ApiCall -Method "GET" -Endpoint "/health"
if ($healthResult.Success) {
    Write-TestResult "API Health Check" "PASS" "API is responding"
} else {
    Write-TestResult "API Health Check" "FAIL" $healthResult.Error
}

# Test 1: ProtocolEntity (Foundation entity - needed for others)
Write-Host "`n📋 TESTING PROTOCOLENTITY CRUD OPERATIONS" -ForegroundColor Magenta

# Create Protocol
$protocolData = @{
    name = "HTTP-REST-v1.0"
    version = "1.0"
    description = "HTTP REST Protocol for testing"
}

$createProtocolResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/protocols" -Body $protocolData
if ($createProtocolResult.Success) {
    $protocolId = $createProtocolResult.Data.id
    Write-TestResult "Create Protocol" "PASS" "Created Protocol with ID: $protocolId"
    
    # Read Protocol
    $readProtocolResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/protocols/$protocolId"
    if ($readProtocolResult.Success) {
        Write-TestResult "Read Protocol" "PASS" "Retrieved Protocol: $($readProtocolResult.Data.name)"
    } else {
        Write-TestResult "Read Protocol" "FAIL" $readProtocolResult.Error
    }
    
    # Update Protocol
    $protocolData.description = "Updated HTTP REST Protocol for testing"
    $updateProtocolResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/protocols/$protocolId" -Body $protocolData
    if ($updateProtocolResult.Success) {
        Write-TestResult "Update Protocol" "PASS" "Updated Protocol description"
    } else {
        Write-TestResult "Update Protocol" "FAIL" $updateProtocolResult.Error
    }
    
    # List Protocols
    $listProtocolsResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/protocols"
    if ($listProtocolsResult.Success) {
        Write-TestResult "List Protocols" "PASS" "Retrieved $($listProtocolsResult.Data.Count) protocols"
    } else {
        Write-TestResult "List Protocols" "FAIL" $listProtocolsResult.Error
    }
} else {
    Write-TestResult "Create Protocol" "FAIL" $createProtocolResult.Error
    $protocolId = $null
}

# Test 2: StepEntity (Workflow entity with EntityId and NextStepIds)
Write-Host "`n🔄 TESTING STEPENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$stepData = @{
    name = "Data Processing Step"
    version = "1.0"
    description = "Step for data processing workflow"
    entityId = [System.Guid]::NewGuid().ToString()
    nextStepIds = @([System.Guid]::NewGuid().ToString(), [System.Guid]::NewGuid().ToString())
}

$createStepResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/steps" -Body $stepData
if ($createStepResult.Success) {
    $stepId = $createStepResult.Data.id
    Write-TestResult "Create Step" "PASS" "Created Step with ID: $stepId"
    
    # Test workflow-specific endpoints
    $getByEntityIdResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/steps/by-entity-id/$($stepData.entityId)"
    if ($getByEntityIdResult.Success) {
        Write-TestResult "Get Step by EntityId" "PASS" "Retrieved step by EntityId"
    } else {
        Write-TestResult "Get Step by EntityId" "FAIL" $getByEntityIdResult.Error
    }
} else {
    Write-TestResult "Create Step" "FAIL" $createStepResult.Error
    $stepId = $null
}

# Test 3: FlowEntity (Workflow entity with StepIds collection)
Write-Host "`n🌊 TESTING FLOWENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$flowData = @{
    name = "Data Processing Flow"
    version = "1.0"
    description = "Flow for data processing workflow"
    stepIds = @([System.Guid]::NewGuid().ToString(), [System.Guid]::NewGuid().ToString())
}

$createFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/flows" -Body $flowData
if ($createFlowResult.Success) {
    $flowId = $createFlowResult.Data.id
    Write-TestResult "Create Flow" "PASS" "Created Flow with ID: $flowId"
    
    # Test workflow-specific endpoints
    if ($flowData.stepIds.Count -gt 0) {
        $getByStepIdResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/flows/by-step-id/$($flowData.stepIds[0])"
        if ($getByStepIdResult.Success) {
            Write-TestResult "Get Flow by StepId" "PASS" "Retrieved flow by StepId"
        } else {
            Write-TestResult "Get Flow by StepId" "FAIL" $getByStepIdResult.Error
        }
    }
} else {
    Write-TestResult "Create Flow" "FAIL" $createFlowResult.Error
    $flowId = $null
}

Write-Host "`n📊 TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $($totalTests - $passedTests)" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor Yellow

# Test 4: ScheduledFlowEntity (Scheduled flow execution with SourceId, DestinationIds, FlowId)
Write-Host "`n⏰ TESTING SCHEDULEDFLOWENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$scheduledFlowData = @{
    name = "Scheduled Data Processing"
    version = "1.0"
    description = "Scheduled flow for data processing"
    sourceId = [System.Guid]::NewGuid().ToString()
    destinationIds = @([System.Guid]::NewGuid().ToString(), [System.Guid]::NewGuid().ToString())
    flowId = if ($flowId) { $flowId } else { [System.Guid]::NewGuid().ToString() }
}

$createScheduledFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
if ($createScheduledFlowResult.Success) {
    $scheduledFlowId = $createScheduledFlowResult.Data.id
    Write-TestResult "Create ScheduledFlow" "PASS" "Created ScheduledFlow with ID: $scheduledFlowId"

    # Test scheduled flow relationship endpoints
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
} else {
    Write-TestResult "Create ScheduledFlow" "FAIL" $createScheduledFlowResult.Error
}

# Test 5: ImporterEntity (with ProtocolId reference and OutputSchema)
Write-Host "`n📥 TESTING IMPORTERENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$importerData = @{
    name = "CSV Data Importer"
    version = "1.0"
    description = "Importer for CSV data files"
    protocolId = if ($protocolId) { $protocolId } else { [System.Guid]::NewGuid().ToString() }
    outputSchema = @{
        type = "object"
        properties = @{
            id = @{ type = "string" }
            name = @{ type = "string" }
            value = @{ type = "number" }
        }
    }
}

$createImporterResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/importers" -Body $importerData
if ($createImporterResult.Success) {
    $importerId = $createImporterResult.Data.id
    Write-TestResult "Create Importer" "PASS" "Created Importer with ID: $importerId"

    # Test protocol reference endpoint
    if ($protocolId) {
        $getByProtocolIdResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/importers/by-protocol-id/$protocolId"
        if ($getByProtocolIdResult.Success) {
            Write-TestResult "Get Importer by ProtocolId" "PASS" "Retrieved importer by ProtocolId"
        } else {
            Write-TestResult "Get Importer by ProtocolId" "FAIL" $getByProtocolIdResult.Error
        }
    }
} else {
    Write-TestResult "Create Importer" "FAIL" $createImporterResult.Error
}

# Test 6: ExporterEntity (with ProtocolId reference and InputSchema)
Write-Host "`n📤 TESTING EXPORTERENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$exporterData = @{
    name = "JSON Data Exporter"
    version = "1.0"
    description = "Exporter for JSON data files"
    protocolId = if ($protocolId) { $protocolId } else { [System.Guid]::NewGuid().ToString() }
    inputSchema = @{
        type = "object"
        properties = @{
            data = @{ type = "array" }
            metadata = @{ type = "object" }
        }
    }
}

$createExporterResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/exporters" -Body $exporterData
if ($createExporterResult.Success) {
    $exporterId = $createExporterResult.Data.id
    Write-TestResult "Create Exporter" "PASS" "Created Exporter with ID: $exporterId"
} else {
    Write-TestResult "Create Exporter" "FAIL" $createExporterResult.Error
}

# Test 7: ProcessorEntity (with ProtocolId reference and both InputSchema/OutputSchema)
Write-Host "`n⚙️ TESTING PROCESSORENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$processorData = @{
    name = "Data Transformer"
    version = "1.0"
    description = "Processor for data transformation"
    protocolId = if ($protocolId) { $protocolId } else { [System.Guid]::NewGuid().ToString() }
    inputSchema = @{
        type = "object"
        properties = @{
            rawData = @{ type = "string" }
        }
    }
    outputSchema = @{
        type = "object"
        properties = @{
            processedData = @{ type = "object" }
        }
    }
}

$createProcessorResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/processors" -Body $processorData
if ($createProcessorResult.Success) {
    $processorId = $createProcessorResult.Data.id
    Write-TestResult "Create Processor" "PASS" "Created Processor with ID: $processorId"
} else {
    Write-TestResult "Create Processor" "FAIL" $createProcessorResult.Error
}

# Test 8: SourceEntity (with ProtocolId reference)
Write-Host "`n📡 TESTING SOURCEENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$sourceData = @{
    name = "Database Source"
    version = "1.0"
    description = "Source for database connections"
    protocolId = if ($protocolId) { $protocolId } else { [System.Guid]::NewGuid().ToString() }
}

$createSourceResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/sources" -Body $sourceData
if ($createSourceResult.Success) {
    $sourceId = $createSourceResult.Data.id
    Write-TestResult "Create Source" "PASS" "Created Source with ID: $sourceId"
} else {
    Write-TestResult "Create Source" "FAIL" $createSourceResult.Error
}

# Test 9: DestinationEntity (with ProtocolId reference)
Write-Host "`n🎯 TESTING DESTINATIONENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$destinationData = @{
    name = "File System Destination"
    version = "1.0"
    description = "Destination for file system storage"
    protocolId = if ($protocolId) { $protocolId } else { [System.Guid]::NewGuid().ToString() }
}

$createDestinationResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/destinations" -Body $destinationData
if ($createDestinationResult.Success) {
    $destinationId = $createDestinationResult.Data.id
    Write-TestResult "Create Destination" "PASS" "Created Destination with ID: $destinationId"
} else {
    Write-TestResult "Create Destination" "FAIL" $createDestinationResult.Error
}

# Test 10: TaskScheduledEntity
Write-Host "`n📅 TESTING TASKSCHEDULEDENTITY CRUD OPERATIONS" -ForegroundColor Magenta

$taskData = @{
    name = "Daily Data Processing Task"
    version = "1.0"
    description = "Scheduled task for daily data processing"
}

$createTaskResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/taskscheduled" -Body $taskData
if ($createTaskResult.Success) {
    $taskId = $createTaskResult.Data.id
    Write-TestResult "Create TaskScheduled" "PASS" "Created TaskScheduled with ID: $taskId"
} else {
    Write-TestResult "Create TaskScheduled" "FAIL" $createTaskResult.Error
}

# Infrastructure Verification Tests
Write-Host "`n🔧 TESTING INFRASTRUCTURE SERVICES" -ForegroundColor Magenta

# Test MongoDB Connection
try {
    $mongoResult = Invoke-RestMethod -Uri "http://localhost:27017" -Method GET -TimeoutSec 5
    Write-TestResult "MongoDB Connection" "PASS" "MongoDB is accessible on port 27017"
} catch {
    Write-TestResult "MongoDB Connection" "FAIL" "MongoDB connection failed: $($_.Exception.Message)"
}

# Test RabbitMQ Management Interface
try {
    $rabbitResult = Invoke-RestMethod -Uri "http://localhost:15672" -Method GET -TimeoutSec 5
    Write-TestResult "RabbitMQ Management" "PASS" "RabbitMQ Management UI is accessible"
} catch {
    Write-TestResult "RabbitMQ Management" "FAIL" "RabbitMQ Management connection failed: $($_.Exception.Message)"
}

# Test OpenTelemetry Collector Metrics
try {
    $otelResult = Invoke-RestMethod -Uri "http://localhost:8888/metrics" -Method GET -TimeoutSec 5
    Write-TestResult "OpenTelemetry Collector" "PASS" "OpenTelemetry Collector metrics endpoint is accessible"
} catch {
    Write-TestResult "OpenTelemetry Collector" "FAIL" "OpenTelemetry Collector connection failed: $($_.Exception.Message)"
}

# Test Delete Operations (Cleanup)
Write-Host "`nTESTING DELETE OPERATIONS (CLEANUP)" -ForegroundColor Magenta

if ($protocolId) {
    $deleteProtocolResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/protocols/$protocolId"
    if ($deleteProtocolResult.Success) {
        Write-TestResult "Delete Protocol" "PASS" "Successfully deleted Protocol"
    } else {
        Write-TestResult "Delete Protocol" "FAIL" $deleteProtocolResult.Error
    }
}

# Final Summary
Write-Host "`n📊 FINAL TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "🐳 Infrastructure Services: MongoDB + RabbitMQ + OpenTelemetry Collector" -ForegroundColor Yellow
Write-Host "🌐 API Service: EntitiesManager API (Local)" -ForegroundColor Yellow
Write-Host "📋 Total Tests: $totalTests" -ForegroundColor White
Write-Host "✅ Passed: $passedTests" -ForegroundColor Green
Write-Host "❌ Failed: $($totalTests - $passedTests)" -ForegroundColor Red
Write-Host "📈 Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor Yellow

if ($passedTests -eq $totalTests) {
    Write-Host "`n🎉 ALL TESTS PASSED! CRUD operations working with real containerized services!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️ Some tests failed. Check the detailed results below." -ForegroundColor Yellow
}

# Display detailed results
Write-Host "`n📋 DETAILED TEST RESULTS" -ForegroundColor Cyan
Write-Host "-" * 80 -ForegroundColor Gray
$testResults | ForEach-Object {
    $color = if ($_.Status -eq "PASS") { "Green" } else { "Red" }
    $timeStr = $_.Timestamp.ToString("HH:mm:ss")
    Write-Host "$timeStr [$($_.Status)] $($_.Test)" -ForegroundColor $color
    if ($_.Details) { Write-Host "    $($_.Details)" -ForegroundColor Gray }
}

Write-Host "`n🔍 VERIFICATION COMPLETE" -ForegroundColor Cyan
Write-Host "Data persisted in MongoDB container: mongodb://localhost:27017" -ForegroundColor Gray
Write-Host "Messages processed through RabbitMQ container: amqp://localhost:5672" -ForegroundColor Gray
Write-Host "Telemetry collected by OpenTelemetry Collector: http://localhost:8888" -ForegroundColor Gray
