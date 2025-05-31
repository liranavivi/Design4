# StepEntity Referential Integrity Test
# Tests StepEntity DELETE and UPDATE validation against FlowEntity references
# Follows the same comprehensive testing pattern as ImporterEntity, ExporterEntity, and ProcessorEntity

$baseUrl = "http://localhost:5130"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

# Global test tracking
$script:totalTests = 0
$script:passedTests = 0
$script:testProtocolId = $null
$script:testImporterId = $null
$script:testStepId = $null
$script:testFlowId = $null

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
        $errorDetails = $null
        
        # Try to parse JSON error response
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            try {
                $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
                if ($errorResponse.error) {
                    $errorMessage = $errorResponse.error
                    $errorDetails = $errorResponse
                }
            }
            catch {
                # If JSON parsing fails, try to extract from raw message
                $rawMessage = $_.ErrorDetails.Message
                if ($rawMessage -like "*FlowEntity*") {
                    $errorMessage = $rawMessage
                }
            }
        }
        
        # Additional attempt to get detailed error from response stream
        if (-not $errorDetails -and $_.Exception.Response) {
            try {
                $responseStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($responseStream)
                $responseBody = $reader.ReadToEnd()
                $reader.Close()
                
                if ($responseBody) {
                    try {
                        $errorResponse = $responseBody | ConvertFrom-Json
                        if ($errorResponse.error) {
                            $errorMessage = $errorResponse.error
                            $errorDetails = $errorResponse
                        }
                    }
                    catch {
                        if ($responseBody -like "*FlowEntity*") {
                            $errorMessage = $responseBody
                        }
                    }
                }
            }
            catch {
                # Ignore stream reading errors
            }
        }
        
        return @{ 
            Success = $false; 
            Error = $errorMessage; 
            StatusCode = $statusCode;
            Details = $errorDetails
        }
    }
}

function Test-Scenario {
    param($Name, $TestBlock)
    
    try {
        $result = & $TestBlock
        if ($result) {
            Write-Host "    [PASS] $Name" -ForegroundColor Green
            return $true
        } else {
            Write-Host "    [FAIL] $Name" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "    [FAIL] $Name - Exception: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

Write-Host "STEPENTITY REFERENTIAL INTEGRITY TEST" -ForegroundColor Cyan
Write-Host "Focused on FlowEntity validation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Setup - Create Protocol for testing
Write-Host "1. SETUP - CREATE PROTOCOL FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$protocolResult = Test-Scenario "Create Test Protocol" {
    $protocolData = @{
        name = "Test Protocol $(Get-Date -Format 'HHmmss')"
        description = "Test protocol for StepEntity referential integrity"
    }
    
    $result = Invoke-ApiCall -Method "POST" -Endpoint "/api/protocols" -Body $protocolData
    if ($result.Success) {
        $script:testProtocolId = $result.Data.id
        Write-Host "    Created protocol: $($script:testProtocolId)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to create protocol: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($protocolResult) { $script:passedTests++ }

# Test 2: Create ImporterEntity for StepEntity
Write-Host ""
Write-Host "2. CREATE IMPORTERENTITY FOR STEPENTITY" -ForegroundColor Cyan
$script:totalTests++
$importerResult = Test-Scenario "Create ImporterEntity" {
    if (-not $script:testProtocolId) {
        Write-Host "    Skipping - no protocol available" -ForegroundColor Yellow
        return $false
    }
    
    $importerData = @{
        name = "Test Importer"
        version = "1.0"
        description = "Test importer for StepEntity referential integrity"
        protocolId = $script:testProtocolId
        outputSchema = "{}"
    }
    
    $result = Invoke-ApiCall -Method "POST" -Endpoint "/api/importers" -Body $importerData
    if ($result.Success) {
        $script:testImporterId = $result.Data.id
        Write-Host "    Created importer: $($script:testImporterId)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to create importer: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($importerResult) { $script:passedTests++ }

# Test 3: Create StepEntity without references
Write-Host ""
Write-Host "3. CREATE STEPENTITY FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$stepResult = Test-Scenario "Create StepEntity" {
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }
    
    $stepData = @{
        entityId = $script:testImporterId
        nextStepIds = @()
        description = "Test step for referential integrity"
    }
    
    $result = Invoke-ApiCall -Method "POST" -Endpoint "/api/steps" -Body $stepData
    if ($result.Success) {
        $script:testStepId = $result.Data.id
        Write-Host "    Created step: $($script:testStepId)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to create step: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($stepResult) { $script:passedTests++ }

# Test 4: Delete StepEntity without references (should succeed)
Write-Host ""
Write-Host "4. DELETE STEPENTITY WITHOUT REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$deleteWithoutRefsResult = Test-Scenario "Delete StepEntity (No References)" {
    if (-not $script:testStepId) {
        Write-Host "    Skipping - no step available" -ForegroundColor Yellow
        return $false
    }
    
    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/steps/$($script:testStepId)"
    if ($result.Success) {
        Write-Host "    Step deleted successfully" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete step: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($deleteWithoutRefsResult) { $script:passedTests++ }

# Test 5: Create new StepEntity for reference testing
Write-Host ""
Write-Host "5. CREATE STEPENTITY FOR REFERENCE TESTING" -ForegroundColor Cyan
$script:totalTests++
$stepResult2 = Test-Scenario "Create StepEntity 2" {
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }
    
    $stepData = @{
        entityId = $script:testImporterId
        nextStepIds = @()
        description = "Test step for referential integrity with references"
    }
    
    $result = Invoke-ApiCall -Method "POST" -Endpoint "/api/steps" -Body $stepData
    if ($result.Success) {
        $script:testStepId = $result.Data.id
        Write-Host "    Created step: $($script:testStepId)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to create step: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($stepResult2) { $script:passedTests++ }

# Test 6: Create FlowEntity with StepEntity reference
Write-Host ""
Write-Host "6. CREATE FLOWENTITY WITH STEPENTITY REFERENCE" -ForegroundColor Cyan
$script:totalTests++
$flowResult = Test-Scenario "Create FlowEntity" {
    if (-not $script:testStepId) {
        Write-Host "    Skipping - no step available" -ForegroundColor Yellow
        return $false
    }

    $flowData = @{
        name = "Test Flow $(Get-Date -Format 'HHmmss')"
        version = "1.0"
        stepIds = @($script:testStepId)
        description = "Test flow for StepEntity referential integrity"
    }

    $result = Invoke-ApiCall -Method "POST" -Endpoint "/api/flows" -Body $flowData
    if ($result.Success) {
        $script:testFlowId = $result.Data.id
        Write-Host "    Created flow: $($script:testFlowId)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to create flow: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($flowResult) { $script:passedTests++ }

# Test 7: Delete StepEntity with references (should fail)
Write-Host ""
Write-Host "7. DELETE STEPENTITY WITH REFERENCES (SHOULD FAIL)" -ForegroundColor Cyan
$script:totalTests++
$deleteWithRefsResult = Test-Scenario "Delete StepEntity (With References)" {
    if (-not $script:testStepId) {
        Write-Host "    Skipping - no step available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/steps/$($script:testStepId)"
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        Write-Host "    Correctly prevented deletion (409 Conflict)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Expected 409 Conflict but got: StatusCode=$($result.StatusCode), Success=$($result.Success)" -ForegroundColor Red
        return $false
    }
}
if ($deleteWithRefsResult) { $script:passedTests++ }

# Test 8: Error message content validation
Write-Host ""
Write-Host "8. ERROR MESSAGE CONTENT VALIDATION" -ForegroundColor Cyan
$script:totalTests++
$errorMessageResult = Test-Scenario "Error Message Content" {
    if (-not $script:testStepId) {
        Write-Host "    Skipping - no step available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/steps/$($script:testStepId)"
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        $errorMessage = $result.Error.ToLower()
        if ($errorMessage -like "*flowentity*" -or $errorMessage -like "*flow*") {
            Write-Host "    Error message correctly mentions FlowEntity" -ForegroundColor Gray
            return $true
        } else {
            Write-Host "    Error message doesn't mention FlowEntity: $($result.Error)" -ForegroundColor Red
            return $false
        }
    } else {
        Write-Host "    Expected 409 Conflict for error message validation" -ForegroundColor Red
        return $false
    }
}
if ($errorMessageResult) { $script:passedTests++ }

# Test 9: Update StepEntity with references (should fail)
Write-Host ""
Write-Host "9. UPDATE STEPENTITY WITH REFERENCES (SHOULD FAIL)" -ForegroundColor Cyan
$script:totalTests++
$updateWithRefsResult = Test-Scenario "Update StepEntity (With References)" {
    if (-not $script:testStepId) {
        Write-Host "    Skipping - no step available" -ForegroundColor Yellow
        return $false
    }

    $updateData = @{
        id = $script:testStepId
        entityId = $script:testImporterId
        nextStepIds = @()
        description = "Updated test step description"
    }

    $result = Invoke-ApiCall -Method "PUT" -Endpoint "/api/steps/$($script:testStepId)" -Body $updateData
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        Write-Host "    Correctly prevented update (409 Conflict)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Expected 409 Conflict but got: StatusCode=$($result.StatusCode), Success=$($result.Success)" -ForegroundColor Red
        return $false
    }
}
if ($updateWithRefsResult) { $script:passedTests++ }

# Test 10: Delete FlowEntity reference
Write-Host ""
Write-Host "10. DELETE FLOWENTITY REFERENCE" -ForegroundColor Cyan
$script:totalTests++
$deleteFlowResult = Test-Scenario "Delete FlowEntity" {
    if (-not $script:testFlowId) {
        Write-Host "    Skipping - no flow available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/flows/$($script:testFlowId)"
    if ($result.Success) {
        Write-Host "    Flow deleted successfully" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete flow: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($deleteFlowResult) { $script:passedTests++ }

# Test 11: Delete StepEntity after removing references
Write-Host ""
Write-Host "11. DELETE STEPENTITY AFTER REMOVING REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$deleteAfterRemovalResult = Test-Scenario "Delete StepEntity (No References)" {
    if (-not $script:testStepId) {
        Write-Host "    Skipping - no step available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/steps/$($script:testStepId)"
    if ($result.Success) {
        Write-Host "    Step deleted successfully after removing references" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete step after removing references: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($deleteAfterRemovalResult) { $script:passedTests++ }

# Test 12: Performance test - Multiple references
Write-Host ""
Write-Host "12. PERFORMANCE TEST - MULTIPLE REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$performanceResult = Test-Scenario "Performance Test" {
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }

    # Create a new step for performance testing
    $stepData = @{
        entityId = $script:testImporterId
        nextStepIds = @()
        description = "Performance test step $(Get-Random)"
    }

    $stepResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/steps" -Body $stepData
    if (-not $stepResult.Success) {
        Write-Host "    Failed to create step for performance test" -ForegroundColor Red
        return $false
    }

    $perfStepId = $stepResult.Data.id

    # Create multiple flows referencing this step
    $flowIds = @()
    for ($i = 1; $i -le 5; $i++) {
        $flowData = @{
            name = "Performance Test Flow $i"
            version = "1.$i"
            stepIds = @($perfStepId)
            description = "Performance test flow $i"
        }

        $flowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/flows" -Body $flowData
        if ($flowResult.Success) {
            $flowIds += $flowResult.Data.id
        }
    }

    # Measure validation time
    $startTime = Get-Date
    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/steps/$perfStepId"
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMilliseconds

    # Clean up flows
    foreach ($flowId in $flowIds) {
        Invoke-ApiCall -Method "DELETE" -Endpoint "/api/flows/$flowId" | Out-Null
    }

    # Clean up step
    Invoke-ApiCall -Method "DELETE" -Endpoint "/api/steps/$perfStepId" | Out-Null

    Write-Host "    Validation completed in ${duration}ms" -ForegroundColor Gray
    if ($duration -lt 100) {
        Write-Host "    Performance under 100ms threshold" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Performance exceeded 100ms threshold" -ForegroundColor Red
        return $false
    }
}
if ($performanceResult) { $script:passedTests++ }

# Test 13: Cleanup - Delete test entities
Write-Host ""
Write-Host "13. CLEANUP - DELETE TEST ENTITIES" -ForegroundColor Cyan
$script:totalTests++
$cleanupResult = Test-Scenario "Cleanup Test Entities" {
    $success = $true

    # Delete importer
    if ($script:testImporterId) {
        $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/importers/$($script:testImporterId)"
        if (-not $result.Success) {
            Write-Host "    Failed to delete importer: $($result.Error)" -ForegroundColor Red
            $success = $false
        }
    }

    # Delete protocol
    if ($script:testProtocolId) {
        $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/protocols/$($script:testProtocolId)"
        if ($result.Success) {
            Write-Host "    Test entities cleaned up successfully" -ForegroundColor Gray
        } else {
            Write-Host "    Failed to delete protocol: $($result.Error)" -ForegroundColor Red
            $success = $false
        }
    }

    return $success
}
if ($cleanupResult) { $script:passedTests++ }

# Final Results
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "STEPENTITY REFERENTIAL INTEGRITY TEST RESULTS" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Tests Passed: $script:passedTests / $script:totalTests ($(($script:passedTests / $script:totalTests * 100).ToString('F0'))%)" -ForegroundColor $(if ($script:passedTests -eq $script:totalTests) { 'Green' } else { 'Yellow' })
Write-Host ""

if ($script:passedTests -eq $script:totalTests) {
    Write-Host "TEST COVERAGE VERIFIED:" -ForegroundColor Green
    Write-Host "✅ StepEntity deletion without references (allowed)" -ForegroundColor Green
    Write-Host "✅ StepEntity deletion with Flow references (blocked)" -ForegroundColor Green
    Write-Host "✅ StepEntity update with Flow references (blocked)" -ForegroundColor Green
    Write-Host "✅ StepEntity deletion after removing all references (allowed)" -ForegroundColor Green
    Write-Host "✅ Error message accuracy and content validation" -ForegroundColor Green
    Write-Host "✅ Performance validation" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎉 STEPENTITY REFERENTIAL INTEGRITY VALIDATION SUCCESSFUL!" -ForegroundColor Green
    Write-Host "All critical validation scenarios working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "REFERENTIAL INTEGRITY STATUS: VALIDATED" -ForegroundColor Green
} else {
    Write-Host "❌ SOME TESTS FAILED - REVIEW IMPLEMENTATION" -ForegroundColor Red
    Write-Host "Failed tests: $($script:totalTests - $script:passedTests)" -ForegroundColor Red
}
