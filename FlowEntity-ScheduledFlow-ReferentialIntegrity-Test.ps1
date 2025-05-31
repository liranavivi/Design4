# FlowEntity Referential Integrity Test
# Tests FlowEntity DELETE and UPDATE validation against ScheduledFlowEntity references
# Follows the same comprehensive testing pattern as SourceEntity

$baseUrl = "http://localhost:5130"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

# Global test tracking
$script:totalTests = 0
$script:passedTests = 0
$script:testProtocolId = $null
$script:testSourceId = $null
$script:testDestinationId = $null
$script:testStepId = $null
$script:testFlowId = $null
$script:testScheduledFlowId = $null

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
                if ($rawMessage -like "*ScheduledFlowEntity*") {
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
                        if ($responseBody -like "*ScheduledFlowEntity*") {
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

Write-Host "FLOWENTITY REFERENTIAL INTEGRITY TEST" -ForegroundColor Cyan
Write-Host "Focused on ScheduledFlowEntity validation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Setup - Create Protocol for testing
Write-Host "1. SETUP - CREATE PROTOCOL FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$protocolResult = Test-Scenario "Create Test Protocol" {
    $protocolData = @{
        name = "Test Protocol $(Get-Date -Format 'HHmmss')"
        description = "Test protocol for FlowEntity referential integrity"
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

# Test 2: Setup - Create Source for testing
Write-Host ""
Write-Host "2. SETUP - CREATE SOURCE FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$sourceResult = Test-Scenario "Create Test Source" {
    if (-not $script:testProtocolId) {
        Write-Host "    Skipping - no protocol available" -ForegroundColor Yellow
        return $false
    }

    $sourceData = @{
        address = "test://source.local"
        version = "1.0"
        name = "Test Source $(Get-Date -Format 'HHmmss')"
        protocolId = $script:testProtocolId
        description = "Test source for FlowEntity referential integrity"
    }

    $result = Invoke-ApiCall -Method "POST" -Endpoint "/api/sources" -Body $sourceData
    if ($result.Success) {
        $script:testSourceId = $result.Data.id
        Write-Host "    Created source: $($script:testSourceId)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to create source: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($sourceResult) { $script:passedTests++ }

# Test 3: Create Destination for testing
Write-Host ""
Write-Host "3. CREATE DESTINATION FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$destinationResult = Test-Scenario "Create Test Destination" {
    if (-not $script:testProtocolId) {
        Write-Host "    Skipping - no protocol available" -ForegroundColor Yellow
        return $false
    }

    $destinationData = @{
        address = "test://destination.local"
        version = "1.0"
        name = "Test Destination $(Get-Date -Format 'HHmmss')"
        protocolId = $script:testProtocolId
        description = "Test destination for FlowEntity referential integrity"
    }

    $result = Invoke-ApiCall -Method "POST" -Endpoint "/api/destinations" -Body $destinationData
    if ($result.Success) {
        $script:testDestinationId = $result.Data.id
        Write-Host "    Created destination: $($script:testDestinationId)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to create destination: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($destinationResult) { $script:passedTests++ }

# Test 4: Create StepEntity for FlowEntity
Write-Host ""
Write-Host "4. CREATE STEPENTITY FOR FLOWENTITY" -ForegroundColor Cyan
$script:totalTests++
$stepResult = Test-Scenario "Create StepEntity" {
    if (-not $script:testSourceId) {
        Write-Host "    Skipping - no source available" -ForegroundColor Yellow
        return $false
    }
    
    $stepData = @{
        entityId = $script:testSourceId
        nextStepIds = @()
        description = "Test step for FlowEntity referential integrity"
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

# Test 5: Create FlowEntity without references
Write-Host ""
Write-Host "5. CREATE FLOWENTITY FOR TESTING" -ForegroundColor Cyan
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
        description = "Test flow for referential integrity"
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

# Test 6: Delete FlowEntity without references (should succeed)
Write-Host ""
Write-Host "6. DELETE FLOWENTITY WITHOUT REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$deleteWithoutRefsResult = Test-Scenario "Delete FlowEntity (No References)" {
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
if ($deleteWithoutRefsResult) { $script:passedTests++ }

# Test 7: Create new FlowEntity for reference testing
Write-Host ""
Write-Host "7. CREATE FLOWENTITY FOR REFERENCE TESTING" -ForegroundColor Cyan
$script:totalTests++
$flowResult2 = Test-Scenario "Create FlowEntity 2" {
    if (-not $script:testStepId) {
        Write-Host "    Skipping - no step available" -ForegroundColor Yellow
        return $false
    }

    $flowData = @{
        name = "Test Flow 2 $(Get-Date -Format 'HHmmss')"
        version = "2.0"
        stepIds = @($script:testStepId)
        description = "Test flow for referential integrity with references"
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
if ($flowResult2) { $script:passedTests++ }

# Test 8: Create ScheduledFlowEntity with FlowEntity reference
Write-Host ""
Write-Host "8. CREATE SCHEDULEDFLOWENTITY WITH FLOWENTITY REFERENCE" -ForegroundColor Cyan
$script:totalTests++
$scheduledFlowResult = Test-Scenario "Create ScheduledFlowEntity" {
    if (-not $script:testFlowId -or -not $script:testSourceId -or -not $script:testDestinationId) {
        Write-Host "    Skipping - missing required entities" -ForegroundColor Yellow
        return $false
    }

    $scheduledFlowData = @{
        version = "1.0"
        name = "Test Scheduled Flow $(Get-Date -Format 'HHmmss')"
        sourceId = $script:testSourceId
        destinationIds = @($script:testDestinationId)
        flowId = $script:testFlowId
        description = "Test scheduled flow for FlowEntity referential integrity"
    }

    $result = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
    if ($result.Success) {
        $script:testScheduledFlowId = $result.Data.id
        Write-Host "    Created scheduled flow: $($script:testScheduledFlowId)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to create scheduled flow: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($scheduledFlowResult) { $script:passedTests++ }

# Test 9: Delete FlowEntity with references (should fail)
Write-Host ""
Write-Host "9. DELETE FLOWENTITY WITH REFERENCES (SHOULD FAIL)" -ForegroundColor Cyan
$script:totalTests++
$deleteWithRefsResult = Test-Scenario "Delete FlowEntity (With References)" {
    if (-not $script:testFlowId) {
        Write-Host "    Skipping - no flow available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/flows/$($script:testFlowId)"
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        Write-Host "    Correctly prevented deletion (409 Conflict)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Expected 409 Conflict but got: StatusCode=$($result.StatusCode), Success=$($result.Success)" -ForegroundColor Red
        return $false
    }
}
if ($deleteWithRefsResult) { $script:passedTests++ }

# Test 10: Error message content validation
Write-Host ""
Write-Host "10. ERROR MESSAGE CONTENT VALIDATION" -ForegroundColor Cyan
$script:totalTests++
$errorMessageResult = Test-Scenario "Error Message Content" {
    if (-not $script:testFlowId) {
        Write-Host "    Skipping - no flow available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/flows/$($script:testFlowId)"
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        $errorMessage = $result.Error.ToLower()
        if ($errorMessage -like "*scheduledflowentity*" -or $errorMessage -like "*scheduled*") {
            Write-Host "    Error message correctly mentions ScheduledFlowEntity" -ForegroundColor Gray
            return $true
        } else {
            Write-Host "    Error message doesn't mention ScheduledFlowEntity: $($result.Error)" -ForegroundColor Red
            return $false
        }
    } else {
        Write-Host "    Expected 409 Conflict for error message validation" -ForegroundColor Red
        return $false
    }
}
if ($errorMessageResult) { $script:passedTests++ }

# Test 11: Update FlowEntity with references (should fail)
Write-Host ""
Write-Host "11. UPDATE FLOWENTITY WITH REFERENCES (SHOULD FAIL)" -ForegroundColor Cyan
$script:totalTests++
$updateWithRefsResult = Test-Scenario "Update FlowEntity (With References)" {
    if (-not $script:testFlowId) {
        Write-Host "    Skipping - no flow available" -ForegroundColor Yellow
        return $false
    }

    $updateData = @{
        id = $script:testFlowId
        name = "Updated Test Flow"
        version = "2.1"
        stepIds = @($script:testStepId)
        description = "Updated test flow description"
    }

    $result = Invoke-ApiCall -Method "PUT" -Endpoint "/api/flows/$($script:testFlowId)" -Body $updateData
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        Write-Host "    Correctly prevented update (409 Conflict)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Expected 409 Conflict but got: StatusCode=$($result.StatusCode), Success=$($result.Success)" -ForegroundColor Red
        return $false
    }
}
if ($updateWithRefsResult) { $script:passedTests++ }

# Test 12: Delete ScheduledFlowEntity reference
Write-Host ""
Write-Host "12. DELETE SCHEDULEDFLOWENTITY REFERENCE" -ForegroundColor Cyan
$script:totalTests++
$deleteScheduledFlowResult = Test-Scenario "Delete ScheduledFlowEntity" {
    if (-not $script:testScheduledFlowId) {
        Write-Host "    Skipping - no scheduled flow available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$($script:testScheduledFlowId)"
    if ($result.Success) {
        Write-Host "    Scheduled flow deleted successfully" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete scheduled flow: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($deleteScheduledFlowResult) { $script:passedTests++ }

# Test 13: Delete FlowEntity after removing references
Write-Host ""
Write-Host "13. DELETE FLOWENTITY AFTER REMOVING REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$deleteAfterRemovalResult = Test-Scenario "Delete FlowEntity (No References)" {
    if (-not $script:testFlowId) {
        Write-Host "    Skipping - no flow available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/flows/$($script:testFlowId)"
    if ($result.Success) {
        Write-Host "    Flow deleted successfully after removing references" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete flow after removing references: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($deleteAfterRemovalResult) { $script:passedTests++ }

# Test 14: Performance test - Multiple references
Write-Host ""
Write-Host "14. PERFORMANCE TEST - MULTIPLE REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$performanceResult = Test-Scenario "Performance Test" {
    if (-not $script:testStepId -or -not $script:testSourceId -or -not $script:testDestinationId) {
        Write-Host "    Skipping - missing required entities" -ForegroundColor Yellow
        return $false
    }

    # Create a new flow for performance testing
    $flowData = @{
        name = "Performance Test Flow $(Get-Random)"
        version = "3.0"
        stepIds = @($script:testStepId)
        description = "Performance test flow"
    }

    $flowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/flows" -Body $flowData
    if (-not $flowResult.Success) {
        Write-Host "    Failed to create flow for performance test" -ForegroundColor Red
        return $false
    }

    $perfFlowId = $flowResult.Data.id

    # Create multiple scheduled flows referencing this flow
    $scheduledFlowIds = @()
    for ($i = 1; $i -le 5; $i++) {
        $scheduledFlowData = @{
            version = "1.0"
            name = "Performance Test Scheduled Flow $i $(Get-Random)"
            sourceId = $script:testSourceId
            destinationIds = @($script:testDestinationId)
            flowId = $perfFlowId
            description = "Performance test scheduled flow $i"
        }

        $scheduledFlowResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/scheduledflows" -Body $scheduledFlowData
        if ($scheduledFlowResult.Success) {
            $scheduledFlowIds += $scheduledFlowResult.Data.id
        }
    }

    # Measure validation time
    $startTime = Get-Date
    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/flows/$perfFlowId"
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMilliseconds

    # Clean up scheduled flows
    foreach ($scheduledFlowId in $scheduledFlowIds) {
        Invoke-ApiCall -Method "DELETE" -Endpoint "/api/scheduledflows/$scheduledFlowId" | Out-Null
    }

    # Clean up flow
    Invoke-ApiCall -Method "DELETE" -Endpoint "/api/flows/$perfFlowId" | Out-Null

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

# Test 15: Cleanup - Delete test entities
Write-Host ""
Write-Host "15. CLEANUP - DELETE TEST ENTITIES" -ForegroundColor Cyan
$script:totalTests++
$cleanupResult = Test-Scenario "Cleanup Test Entities" {
    $success = $true

    # Delete step
    if ($script:testStepId) {
        $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/steps/$($script:testStepId)"
        if (-not $result.Success) {
            Write-Host "    Failed to delete step: $($result.Error)" -ForegroundColor Red
            $success = $false
        }
    }

    # Delete destination
    if ($script:testDestinationId) {
        $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/destinations/$($script:testDestinationId)"
        if (-not $result.Success) {
            Write-Host "    Failed to delete destination: $($result.Error)" -ForegroundColor Red
            $success = $false
        }
    }

    # Delete source
    if ($script:testSourceId) {
        $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/sources/$($script:testSourceId)"
        if (-not $result.Success) {
            Write-Host "    Failed to delete source: $($result.Error)" -ForegroundColor Red
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
Write-Host "FLOWENTITY REFERENTIAL INTEGRITY TEST RESULTS" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Tests Passed: $script:passedTests / $script:totalTests ($(($script:passedTests / $script:totalTests * 100).ToString('F0'))%)" -ForegroundColor $(if ($script:passedTests -eq $script:totalTests) { 'Green' } else { 'Yellow' })
Write-Host ""

if ($script:passedTests -eq $script:totalTests) {
    Write-Host "TEST COVERAGE VERIFIED:" -ForegroundColor Green
    Write-Host "✅ FlowEntity deletion without references (allowed)" -ForegroundColor Green
    Write-Host "✅ FlowEntity deletion with ScheduledFlow references (blocked)" -ForegroundColor Green
    Write-Host "✅ FlowEntity update with ScheduledFlow references (blocked)" -ForegroundColor Green
    Write-Host "✅ FlowEntity deletion after removing all references (allowed)" -ForegroundColor Green
    Write-Host "✅ Error message accuracy and content validation" -ForegroundColor Green
    Write-Host "✅ Performance validation" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎉 FLOWENTITY REFERENTIAL INTEGRITY VALIDATION SUCCESSFUL!" -ForegroundColor Green
    Write-Host "All critical validation scenarios working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "REFERENTIAL INTEGRITY STATUS: VALIDATED" -ForegroundColor Green
} else {
    Write-Host "❌ SOME TESTS FAILED - REVIEW IMPLEMENTATION" -ForegroundColor Red
    Write-Host "Failed tests: $($script:totalTests - $script:passedTests)" -ForegroundColor Red
}
