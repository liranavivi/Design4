# Comprehensive CRUD Test for OrchestratedFlowEntity
# Tests the renamed entity with Assignment-focused architecture

param(
    [string]$BaseUrl = "http://localhost:5000",
    [int]$DelaySeconds = 2
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

# Test configuration
$TestData = @{
    Version = "v1.0.0"
    Name = "TestOrchestratedFlow"
    Description = "Test orchestrated flow for CRUD operations"
    AssignmentIds = @()  # Empty for initial test
    FlowId = [System.Guid]::NewGuid()
}

Write-Host "=== OrchestratedFlowEntity CRUD Test Suite ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Test Data: $($TestData | ConvertTo-Json -Compress)" -ForegroundColor Yellow
Write-Host ""

# Function to make HTTP requests with error handling
function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [object]$Body = $null,
        [string]$Description
    )
    
    Write-Host "[$Method] $Description" -ForegroundColor White
    Write-Host "URI: $Uri" -ForegroundColor Gray
    
    try {
        $headers = @{ "Content-Type" = "application/json" }
        $params = @{
            Uri = $Uri
            Method = $Method
            Headers = $headers
            TimeoutSec = 30
        }
        
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            Write-Host "Request Body: $jsonBody" -ForegroundColor Gray
            $params.Body = $jsonBody
        }
        
        $response = Invoke-RestMethod @params
        Write-Host "✅ SUCCESS" -ForegroundColor Green
        
        if ($response) {
            Write-Host "Response: $($response | ConvertTo-Json -Depth 5)" -ForegroundColor Green
        }
        
        return @{ Success = $true; Data = $response; Error = $null }
    }
    catch {
        Write-Host "❌ FAILED: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.Exception.Response) {
            try {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorBody = $reader.ReadToEnd()
                Write-Host "Error Response: $errorBody" -ForegroundColor Red
            }
            catch {
                Write-Host "Could not read error response" -ForegroundColor Red
            }
        }
        
        return @{ Success = $false; Data = $null; Error = $_.Exception.Message }
    }
    finally {
        Write-Host ""
    }
}

# Wait for API to be ready
Write-Host "Waiting for API to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Test 1: Health Check
Write-Host "=== Test 1: Health Check ===" -ForegroundColor Magenta
$healthResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/health" -Description "Health check"

if (-not $healthResult.Success) {
    Write-Host "❌ API is not ready. Exiting tests." -ForegroundColor Red
    exit 1
}

Start-Sleep -Seconds $DelaySeconds

# Test 2: Get All OrchestratedFlows (should be empty initially)
Write-Host "=== Test 2: Get All OrchestratedFlows (Initial) ===" -ForegroundColor Magenta
$getAllResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows" -Description "Get all orchestrated flows"

Start-Sleep -Seconds $DelaySeconds

# Test 3: Create OrchestratedFlow
Write-Host "=== Test 3: Create OrchestratedFlow ===" -ForegroundColor Magenta
$createResult = Invoke-ApiRequest -Method "POST" -Uri "$BaseUrl/api/orchestratedflows" -Body $TestData -Description "Create new orchestrated flow"

if (-not $createResult.Success) {
    Write-Host "❌ Create test failed. Cannot continue with other tests." -ForegroundColor Red
    exit 1
}

$createdEntity = $createResult.Data
$entityId = $createdEntity.id
Write-Host "Created Entity ID: $entityId" -ForegroundColor Yellow

Start-Sleep -Seconds $DelaySeconds

# Test 4: Get OrchestratedFlow by ID
Write-Host "=== Test 4: Get OrchestratedFlow by ID ===" -ForegroundColor Magenta
$getByIdResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows/$entityId" -Description "Get orchestrated flow by ID"

Start-Sleep -Seconds $DelaySeconds

# Test 5: Get All OrchestratedFlows (should contain our entity)
Write-Host "=== Test 5: Get All OrchestratedFlows (After Create) ===" -ForegroundColor Magenta
$getAllAfterCreateResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows" -Description "Get all orchestrated flows after create"

Start-Sleep -Seconds $DelaySeconds

# Test 6: Update OrchestratedFlow
Write-Host "=== Test 6: Update OrchestratedFlow ===" -ForegroundColor Magenta
$updateData = @{
    id = $entityId
    version = "v2.0.0"
    name = "UpdatedOrchestratedFlow"
    description = "Updated test orchestrated flow"
    assignmentIds = @([System.Guid]::NewGuid())  # Add a test assignment ID
    flowId = $TestData.FlowId
}

$updateResult = Invoke-ApiRequest -Method "PUT" -Uri "$BaseUrl/api/orchestratedflows/$entityId" -Body $updateData -Description "Update orchestrated flow"

Start-Sleep -Seconds $DelaySeconds

# Test 7: Verify Update
Write-Host "=== Test 7: Verify Update ===" -ForegroundColor Magenta
$getAfterUpdateResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows/$entityId" -Description "Get orchestrated flow after update"

Start-Sleep -Seconds $DelaySeconds

# Test 8: Search by Name
Write-Host "=== Test 8: Search by Name ===" -ForegroundColor Magenta
$searchByNameResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows/by-name/UpdatedOrchestratedFlow" -Description "Search by name"

Start-Sleep -Seconds $DelaySeconds

# Test 9: Search by Version
Write-Host "=== Test 9: Search by Version ===" -ForegroundColor Magenta
$searchByVersionResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows/by-version/v2.0.0" -Description "Search by version"

Start-Sleep -Seconds $DelaySeconds

# Test 10: Search by FlowId
Write-Host "=== Test 10: Search by FlowId ===" -ForegroundColor Magenta
$searchByFlowIdResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows/by-flow-id/$($TestData.FlowId)" -Description "Search by FlowId"

Start-Sleep -Seconds $DelaySeconds

# Test 11: Delete OrchestratedFlow
Write-Host "=== Test 11: Delete OrchestratedFlow ===" -ForegroundColor Magenta
$deleteResult = Invoke-ApiRequest -Method "DELETE" -Uri "$BaseUrl/api/orchestratedflows/$entityId" -Description "Delete orchestrated flow"

Start-Sleep -Seconds $DelaySeconds

# Test 12: Verify Deletion
Write-Host "=== Test 12: Verify Deletion ===" -ForegroundColor Magenta
$getAfterDeleteResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows/$entityId" -Description "Try to get deleted orchestrated flow"

Start-Sleep -Seconds $DelaySeconds

# Test 13: Get All OrchestratedFlows (should be empty again)
Write-Host "=== Test 13: Get All OrchestratedFlows (After Delete) ===" -ForegroundColor Magenta
$getAllAfterDeleteResult = Invoke-ApiRequest -Method "GET" -Uri "$BaseUrl/api/orchestratedflows" -Description "Get all orchestrated flows after delete"

# Test Results Summary
Write-Host "=== TEST RESULTS SUMMARY ===" -ForegroundColor Cyan
Write-Host ""

$tests = @(
    @{ Name = "Health Check"; Result = $healthResult.Success }
    @{ Name = "Get All (Initial)"; Result = $getAllResult.Success }
    @{ Name = "Create"; Result = $createResult.Success }
    @{ Name = "Get by ID"; Result = $getByIdResult.Success }
    @{ Name = "Get All (After Create)"; Result = $getAllAfterCreateResult.Success }
    @{ Name = "Update"; Result = $updateResult.Success }
    @{ Name = "Verify Update"; Result = $getAfterUpdateResult.Success }
    @{ Name = "Search by Name"; Result = $searchByNameResult.Success }
    @{ Name = "Search by Version"; Result = $searchByVersionResult.Success }
    @{ Name = "Search by FlowId"; Result = $searchByFlowIdResult.Success }
    @{ Name = "Delete"; Result = $deleteResult.Success }
    @{ Name = "Verify Deletion"; Result = $getAfterDeleteResult.Success -eq $false }  # Should fail (404)
    @{ Name = "Get All (After Delete)"; Result = $getAllAfterDeleteResult.Success }
)

$passedTests = 0
$totalTests = $tests.Count

foreach ($test in $tests) {
    $status = if ($test.Result) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($test.Result) { "Green" } else { "Red" }
    Write-Host "$($test.Name): $status" -ForegroundColor $color
    if ($test.Result) { $passedTests++ }
}

Write-Host ""
Write-Host "OVERALL RESULT: $passedTests/$totalTests tests passed" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Red" })

if ($passedTests -eq $totalTests) {
    Write-Host "🎉 ALL TESTS PASSED! OrchestratedFlowEntity CRUD operations are working correctly." -ForegroundColor Green
} else {
    Write-Host "⚠️  Some tests failed. Please check the API logs and fix any issues." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Test Completed ===" -ForegroundColor Cyan
