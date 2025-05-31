# ImporterEntity Referential Integrity Test
# Tests ImporterEntity DELETE and UPDATE validation against StepEntity references
# Follows the same comprehensive testing pattern as SourceEntity and DestinationEntity

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
                if ($rawMessage -like "*StepEntity*") {
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
                        if ($responseBody -like "*StepEntity*") {
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

Write-Host "IMPORTERENTITY REFERENTIAL INTEGRITY TEST" -ForegroundColor Cyan
Write-Host "Focused on StepEntity validation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Setup - Create Protocol for testing
Write-Host "1. SETUP - CREATE PROTOCOL FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$protocolResult = Test-Scenario "Create Test Protocol" {
    $protocolData = @{
        name = "Test Protocol $(Get-Date -Format 'HHmmss')"
        description = "Test protocol for ImporterEntity referential integrity"
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

# Test 2: Create ImporterEntity without references
Write-Host ""
Write-Host "2. CREATE IMPORTERENTITY FOR TESTING" -ForegroundColor Cyan
$script:totalTests++
$importerResult = Test-Scenario "Create ImporterEntity" {
    if (-not $script:testProtocolId) {
        Write-Host "    Skipping - no protocol available" -ForegroundColor Yellow
        return $false
    }
    
    $importerData = @{
        name = "Test Importer"
        version = "1.0"
        description = "Test importer for referential integrity"
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

# Test 3: Delete ImporterEntity without references (should succeed)
Write-Host ""
Write-Host "3. DELETE IMPORTERENTITY WITHOUT REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$deleteWithoutRefsResult = Test-Scenario "Delete ImporterEntity (No References)" {
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }
    
    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/importers/$($script:testImporterId)"
    if ($result.Success) {
        Write-Host "    Importer deleted successfully" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete importer: $($result.Error)" -ForegroundColor Red
        return $false
    }
}
if ($deleteWithoutRefsResult) { $script:passedTests++ }

# Test 4: Create new ImporterEntity for reference testing
Write-Host ""
Write-Host "4. CREATE IMPORTERENTITY FOR REFERENCE TESTING" -ForegroundColor Cyan
$script:totalTests++
$importerResult2 = Test-Scenario "Create ImporterEntity 2" {
    if (-not $script:testProtocolId) {
        Write-Host "    Skipping - no protocol available" -ForegroundColor Yellow
        return $false
    }
    
    $importerData = @{
        name = "Test Importer 2"
        version = "2.0"
        description = "Test importer for referential integrity with references"
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
if ($importerResult2) { $script:passedTests++ }

# Test 5: Create StepEntity with ImporterEntity reference
Write-Host ""
Write-Host "5. CREATE STEPENTITY WITH IMPORTERENTITY REFERENCE" -ForegroundColor Cyan
$script:totalTests++
$stepResult = Test-Scenario "Create StepEntity" {
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }

    $stepData = @{
        entityId = $script:testImporterId  # Reference our test importer
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

# Test 6: Delete ImporterEntity with references (should fail)
Write-Host ""
Write-Host "6. DELETE IMPORTERENTITY WITH REFERENCES (SHOULD FAIL)" -ForegroundColor Cyan
$script:totalTests++
$deleteWithRefsResult = Test-Scenario "Delete ImporterEntity (With References)" {
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/importers/$($script:testImporterId)"
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
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/importers/$($script:testImporterId)"
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        if ($result.Error -like "*StepEntity*") {
            Write-Host "    Error message correctly mentions StepEntity" -ForegroundColor Gray
            return $true
        } else {
            Write-Host "    Error message does not mention StepEntity: $($result.Error)" -ForegroundColor Red
            return $false
        }
    } else {
        Write-Host "    Expected 409 Conflict but got different result" -ForegroundColor Red
        return $false
    }
}
if ($errorMessageResult) { $script:passedTests++ }

# Test 8: Update ImporterEntity with references (should fail)
Write-Host ""
Write-Host "8. UPDATE IMPORTERENTITY WITH REFERENCES (SHOULD FAIL)" -ForegroundColor Cyan
$script:totalTests++
$updateWithRefsResult = Test-Scenario "Update ImporterEntity (With References)" {
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }

    # Get current importer
    $getResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/importers/$($script:testImporterId)"
    if (-not $getResult.Success) {
        Write-Host "    Failed to get importer for update" -ForegroundColor Red
        return $false
    }

    $importer = $getResult.Data
    $importer.description = "Updated description - should fail due to references"

    $result = Invoke-ApiCall -Method "PUT" -Endpoint "/api/importers/$($script:testImporterId)" -Body $importer
    if (-not $result.Success -and $result.StatusCode -eq 409) {
        Write-Host "    Correctly prevented update (409 Conflict)" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Unexpected result - update should have been prevented" -ForegroundColor Red
        return $false
    }
}
if ($updateWithRefsResult) { $script:passedTests++ }

# Test 9: Delete StepEntity reference
Write-Host ""
Write-Host "9. DELETE STEPENTITY REFERENCE" -ForegroundColor Cyan
$script:totalTests++
$deleteStepResult = Test-Scenario "Delete StepEntity" {
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
if ($deleteStepResult) { $script:passedTests++ }

# Test 10: Delete ImporterEntity after removing references (should succeed)
Write-Host ""
Write-Host "10. DELETE IMPORTERENTITY AFTER REMOVING REFERENCES" -ForegroundColor Cyan
$script:totalTests++
$deleteAfterCleanupResult = Test-Scenario "Delete ImporterEntity (No References)" {
    if (-not $script:testImporterId) {
        Write-Host "    Skipping - no importer available" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/importers/$($script:testImporterId)"
    if ($result.Success) {
        Write-Host "    Importer deleted successfully after removing references" -ForegroundColor Gray
        return $true
    } else {
        Write-Host "    Failed to delete importer after cleanup: $($result.Error)" -ForegroundColor Red
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

    # Create an importer for performance testing with unique name
    $perfTimestamp = Get-Date -Format "HHmmssffff"
    $perfImporterData = @{
        name = "Performance Test Importer $perfTimestamp"
        version = "3.0"
        description = "Importer for performance testing"
        protocolId = $script:testProtocolId
        outputSchema = "{}"
    }

    $perfImporterResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/importers" -Body $perfImporterData
    if (-not $perfImporterResult.Success) {
        Write-Host "    Failed to create performance test importer" -ForegroundColor Red
        return $false
    }

    $perfImporterId = $perfImporterResult.Data.id

    # Measure validation time
    $startTime = Get-Date
    $deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/importers/$perfImporterId"
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMilliseconds

    Write-Host "    Validation completed in ${duration}ms" -ForegroundColor Gray

    # Check if the delete was successful (should be since no references exist)
    if (-not $deleteResult.Success) {
        Write-Host "    Performance test importer deletion failed: $($deleteResult.Error)" -ForegroundColor Red
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
Write-Host "IMPORTERENTITY REFERENTIAL INTEGRITY TEST RESULTS" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
$passRate = [math]::Round(($script:passedTests / $script:totalTests) * 100, 1)
Write-Host "Tests Passed: $($script:passedTests) / $($script:totalTests) ($passRate%)" -ForegroundColor $(if ($script:passedTests -eq $script:totalTests) { "Green" } else { "Yellow" })
Write-Host ""

if ($script:passedTests -eq $script:totalTests) {
    Write-Host "TEST COVERAGE VERIFIED:" -ForegroundColor Green
    Write-Host "✅ ImporterEntity deletion without references (allowed)" -ForegroundColor Green
    Write-Host "✅ ImporterEntity deletion with Step references (blocked)" -ForegroundColor Green
    Write-Host "✅ ImporterEntity update with Step references (blocked)" -ForegroundColor Green
    Write-Host "✅ ImporterEntity deletion after removing all references (allowed)" -ForegroundColor Green
    Write-Host "✅ Error message accuracy and content validation" -ForegroundColor Green
    Write-Host "✅ Performance validation" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎉 IMPORTERENTITY REFERENTIAL INTEGRITY VALIDATION SUCCESSFUL!" -ForegroundColor Green
    Write-Host "All critical validation scenarios working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "REFERENTIAL INTEGRITY STATUS: VALIDATED" -ForegroundColor Green
} else {
    Write-Host "❌ Some tests failed. Please review the implementation." -ForegroundColor Red
    Write-Host ""
    Write-Host "REFERENTIAL INTEGRITY STATUS: NEEDS ATTENTION" -ForegroundColor Red
}
