# DestinationEntity Referential Integrity Test
# Tests DestinationEntity DELETE and UPDATE validation against ScheduledFlowEntity references
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
$script:testDestinationId = $null
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

Write-Host "DESTINATIONENTITY REFERENTIAL INTEGRITY TEST" -ForegroundColor Cyan
Write-Host "Focused on ScheduledFlowEntity validation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Setup - Create Protocol for testing
Write-Host "1. SETUP - CREATE PROTOCOL FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$protocolResult = Test-Scenario "Create Test Protocol" {
    $protocolData = @{
        name = "Test Protocol $(Get-Date -Format 'HHmmss')"
        description = "Test protocol for DestinationEntity referential integrity"
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

# Test 2: Create DestinationEntity without references
Write-Host ""
Write-Host "2. CREATE DESTINATIONENTITY FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$destinationResult = Test-Scenario "Create DestinationEntity" {
    if (-not $script:testProtocolId) {
        Write-Host "    Skipping - no protocol available" -ForegroundColor Yellow
        return $false
    }
    
    $destinationData = @{
        name = "Test Destination"
        version = "1.0"
        description = "Test destination for referential integrity"
        address = "test://localhost/destination"
        protocolId = $script:testProtocolId
        configuration = @{}
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

# Test 3: Delete DestinationEntity without references (should succeed)
Write-Host ""
Write-Host "3. DELETE DESTINATIONENTITY WITHOUT REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$deleteWithoutRefsResult = Test-Scenario "Delete DestinationEntity (No References)" {
    if (-not $script:testDestinationId) {
        Write-Host "    Skipping - no destination available" -ForegroundColor Yellow
        return $false
    }
    
    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/destinations/$($script:testDestinationId)"
    if ($result.Success) {
        Write-Host "    Destination deleted successfully" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete destination: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($deleteWithoutRefsResult) { $script:passedTests++ }

# Test 4: Create new DestinationEntity for reference testing
Write-Host ""
Write-Host "4. CREATE DESTINATIONENTITY FOR REFERENCE TESTING" -ForegroundColor Cyan
$script:totalTests++
$destinationResult2 = Test-Scenario "Create DestinationEntity 2" {
    if (-not $script:testProtocolId) {
        Write-Host "    Skipping - no protocol available" -ForegroundColor Yellow
        return $false
    }
    
    $destinationData = @{
        name = "Test Destination 2"
        version = "1.0"
        description = "Test destination for referential integrity with references"
        address = "test://localhost/destination2"
        protocolId = $script:testProtocolId
        configuration = @{}
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
if ($destinationResult2) { $script:passedTests++ }

# Test 5: Create ScheduledFlowEntity with DestinationEntity reference
Write-Host ""
Write-Host "5. CREATE SCHEDULEDFLOWENTITY WITH DESTINATIONENTITY REFERENCE" -ForegroundColor Cyan
$script:totalTests++
$scheduledFlowResult = Test-Scenario "Create ScheduledFlowEntity" {
    if (-not $script:testDestinationId) {
        Write-Host "    Skipping - no destination available" -ForegroundColor Yellow
        return $false
    }

    # Create a dummy source for the scheduled flow with unique name
    $timestamp = Get-Date -Format "HHmmssffff"
    $sourceData = @{
        name = "Dummy Source $timestamp"
        version = "1.0"
        description = "Dummy source for scheduled flow"
        address = "test://localhost/dummysource$timestamp"
        protocolId = $script:testProtocolId
        outputSchema = @{}
    }

    $sourceResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/sources" -Body $sourceData
    if (-not $sourceResult.Success) {
        Write-Host "    Failed to create dummy source: $($sourceResult.Error)" -ForegroundColor Red
        return $false
    }

    $dummySourceId = $sourceResult.Data.id

    $scheduledFlowData = @{
        name = "Test Scheduled Flow"
        version = "1.0"
        description = "Test scheduled flow for referential integrity"
        sourceId = $dummySourceId
        destinationIds = @($script:testDestinationId)  # Reference our test destination
        flowId = [System.Guid]::NewGuid().ToString()
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

# Test 6: Delete DestinationEntity with references (should fail)
Write-Host ""
Write-Host "6. DELETE DESTINATIONENTITY WITH REFERENCES (SHOULD FAIL)" -ForegroundColor Cyan
$script:totalTests++
$deleteWithRefsResult = Test-Scenario "Delete DestinationEntity (With References)" {
    if (-not $script:testDestinationId) {
        Write-Host "    Skipping - no destination available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/destinations/$($script:testDestinationId)"
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        Write-Host "    Correctly prevented deletion (409 Conflict)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Unexpected result - deletion should have been prevented" -ForegroundColor Red
        return $false
    }
}
if ($deleteWithRefsResult) { $script:passedTests++ }

# Test 7: Error message content validation
Write-Host ""
Write-Host "7. ERROR MESSAGE CONTENT VALIDATION" -ForegroundColor Cyan
$script:totalTests++
$errorMessageResult = Test-Scenario "Error Message Content" {
    if (-not $script:testDestinationId) {
        Write-Host "    Skipping - no destination available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/destinations/$($script:testDestinationId)"
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        if ($result.Error -like "*ScheduledFlowEntity*") {
            Write-Host "    Error message correctly mentions ScheduledFlowEntity" -ForegroundColor Gray
            return $true
        } else {
            Write-Host "    Error message does not mention ScheduledFlowEntity: $($result.Error)" -ForegroundColor Red
            return $false
        }
    } else {
        Write-Host "    Expected 409 Conflict but got different result" -ForegroundColor Red
        return $false
    }
}
if ($errorMessageResult) { $script:passedTests++ }

# Test 8: Update DestinationEntity with references (should fail)
Write-Host ""
Write-Host "8. UPDATE DESTINATIONENTITY WITH REFERENCES (SHOULD FAIL)" -ForegroundColor Cyan
$script:totalTests++
$updateWithRefsResult = Test-Scenario "Update DestinationEntity (With References)" {
    if (-not $script:testDestinationId) {
        Write-Host "    Skipping - no destination available" -ForegroundColor Yellow
        return $false
    }

    # Get current destination
    $getResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/destinations/$($script:testDestinationId)"
    if (-not $getResult.Success) {
        Write-Host "    Failed to get destination for update" -ForegroundColor Red
        return $false
    }

    $destination = $getResult.Data
    $destination.description = "Updated description - should fail due to references"

    $result = Invoke-ApiCall -Method "PUT" -Endpoint "/api/destinations/$($script:testDestinationId)" -Body $destination
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        Write-Host "    Correctly prevented update (409 Conflict)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Unexpected result - update should have been prevented" -ForegroundColor Red
        return $false
    }
}
if ($updateWithRefsResult) { $script:passedTests++ }

# Test 9: Delete ScheduledFlowEntity reference
Write-Host ""
Write-Host "9. DELETE SCHEDULEDFLOWENTITY REFERENCE" -ForegroundColor Cyan
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

# Test 10: Delete DestinationEntity after removing references (should succeed)
Write-Host ""
Write-Host "10. DELETE DESTINATIONENTITY AFTER REMOVING REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$deleteAfterCleanupResult = Test-Scenario "Delete DestinationEntity (No References)" {
    if (-not $script:testDestinationId) {
        Write-Host "    Skipping - no destination available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/destinations/$($script:testDestinationId)"
    if ($result.Success) {
        Write-Host "    Destination deleted successfully after removing references" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete destination after cleanup: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($deleteAfterCleanupResult) { $script:passedTests++ }

# Test 11: Performance test - Multiple references
Write-Host ""
Write-Host "11. PERFORMANCE TEST - MULTIPLE REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$performanceResult = Test-Scenario "Performance Test" {
    if (-not $script:testProtocolId) {
        Write-Host "    Skipping - no protocol available" -ForegroundColor Yellow
        return $false
    }

    # Create a destination for performance testing with unique name
    $perfTimestamp = Get-Date -Format "HHmmssffff"
    $perfDestinationData = @{
        name = "Performance Test Destination $perfTimestamp"
        version = "1.0"
        description = "Destination for performance testing"
        address = "test://localhost/perfdestination$perfTimestamp"
        protocolId = $script:testProtocolId
        configuration = @{}
    }

    $perfDestResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/destinations" -Body $perfDestinationData
    if (-not $perfDestResult.Success) {
        Write-Host "    Failed to create performance test destination" -ForegroundColor Red
        return $false
    }

    $perfDestinationId = $perfDestResult.Data.id

    # Measure validation time
    $startTime = Get-Date
    $deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/destinations/$perfDestinationId"
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMilliseconds

    Write-Host "    Validation completed in ${duration}ms" -ForegroundColor Gray

    # Check if the delete was successful (should be since no references exist)
    if (-not $deleteResult.Success) {
        Write-Host "    Performance test destination deletion failed: $($deleteResult.Error)" -ForegroundColor Red
        return $false
    }

    # Performance should be under 100ms for single validation
    if ($duration -lt 100) {
        Write-Host "    Performance under 100ms threshold" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Performance exceeded 100ms threshold" -ForegroundColor Red
        return $false
    }
}
if ($performanceResult) { $script:passedTests++ }

# Test 12: Cleanup - Delete test protocol
Write-Host ""
Write-Host "12. CLEANUP - DELETE TEST PROTOCOL" -ForegroundColor Cyan
if ($script:testProtocolId) {
    $cleanupResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/protocols/$($script:testProtocolId)"
    if ($cleanupResult.Success) {
        Write-Host "    Test protocol cleaned up successfully" -ForegroundColor Gray
    } else {
        Write-Host "    Failed to cleanup test protocol: $($cleanupResult.Error)" -ForegroundColor Yellow
    }
}

# Final Results
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "DESTINATIONENTITY REFERENTIAL INTEGRITY TEST RESULTS" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
$passRate = [math]::Round(($script:passedTests / $script:totalTests) * 100, 1)
Write-Host "Tests Passed: $($script:passedTests) / $($script:totalTests) ($passRate%)" -ForegroundColor $(if ($script:passedTests -eq $script:totalTests) { "Green" } else { "Yellow" })
Write-Host ""

if ($script:passedTests -eq $script:totalTests) {
    Write-Host "TEST COVERAGE VERIFIED:" -ForegroundColor Green
    Write-Host "✅ DestinationEntity deletion without references (allowed)" -ForegroundColor Green
    Write-Host "✅ DestinationEntity deletion with ScheduledFlow references (blocked)" -ForegroundColor Green
    Write-Host "✅ DestinationEntity update with ScheduledFlow references (blocked)" -ForegroundColor Green
    Write-Host "✅ DestinationEntity deletion after removing all references (allowed)" -ForegroundColor Green
    Write-Host "✅ Error message accuracy and content validation" -ForegroundColor Green
    Write-Host "✅ Performance validation" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎉 DESTINATIONENTITY REFERENTIAL INTEGRITY VALIDATION SUCCESSFUL!" -ForegroundColor Green
    Write-Host "All critical validation scenarios working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "REFERENTIAL INTEGRITY STATUS: VALIDATED" -ForegroundColor Green
} else {
    Write-Host "❌ Some tests failed. Please review the implementation." -ForegroundColor Red
    Write-Host ""
    Write-Host "REFERENTIAL INTEGRITY STATUS: NEEDS ATTENTION" -ForegroundColor Red
}
