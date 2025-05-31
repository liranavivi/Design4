# Test Error Message Parsing
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

        # Try to extract the actual error message from the response body
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            try {
                $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
                if ($errorResponse.error) {
                    $errorMessage = $errorResponse.error
                }
            }
            catch {
                # If JSON parsing fails, use the raw error details
                $errorMessage = $_.ErrorDetails.Message
            }
        }

        # For debugging, let's also try to read the response stream
        if ($_.Exception.Response) {
            try {
                $responseStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($responseStream)
                $responseBody = $reader.ReadToEnd()
                $reader.Close()
                $responseStream.Close()

                if ($responseBody) {
                    try {
                        $jsonResponse = $responseBody | ConvertFrom-Json
                        if ($jsonResponse.error) {
                            $errorMessage = $jsonResponse.error
                        }
                    }
                    catch {
                        # If not JSON, use the raw response body
                        $errorMessage = $responseBody
                    }
                }
            }
            catch {
                # Ignore stream reading errors
            }
        }

        return @{ Success = $false; Error = $errorMessage; StatusCode = $statusCode }
    }
}

Write-Host "Testing Error Message Parsing" -ForegroundColor Cyan

# Create a protocol
$protocolData = @{
    name = "Test Protocol $(Get-Date -Format 'HHmmss')"
    description = "Test protocol for error message parsing"
}

$createResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/protocols" -Body $protocolData
if ($createResult.Success) {
    $protocolId = $createResult.Data.id
    Write-Host "Created protocol: $protocolId" -ForegroundColor Green
    
    # Create a source
    $sourceData = @{
        name = "Test Source"
        version = "1.0"
        description = "Test source"
        address = "test://localhost/source"
        protocolId = $protocolId
        outputSchema = @{}
    }
    
    $sourceResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/sources" -Body $sourceData
    if ($sourceResult.Success) {
        Write-Host "Created source: $($sourceResult.Data.id)" -ForegroundColor Green
        
        # Try to delete the protocol (should fail)
        $deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/protocols/$protocolId"
        
        Write-Host "`nDelete Result:" -ForegroundColor Yellow
        Write-Host "Success: $($deleteResult.Success)" -ForegroundColor $(if ($deleteResult.Success) { "Green" } else { "Red" })
        Write-Host "Status Code: $($deleteResult.StatusCode)" -ForegroundColor Yellow
        Write-Host "Error Message: $($deleteResult.Error)" -ForegroundColor Yellow
        
        # Check if error message contains expected text
        if ($deleteResult.Error -like "*SourceEntity*") {
            Write-Host "✅ Error message contains 'SourceEntity'" -ForegroundColor Green
        } else {
            Write-Host "❌ Error message does NOT contain 'SourceEntity'" -ForegroundColor Red
        }
        
        # Clean up
        $cleanupSource = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/sources/$($sourceResult.Data.id)"
        $cleanupProtocol = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/protocols/$protocolId"
        Write-Host "Cleanup completed" -ForegroundColor Gray
    } else {
        Write-Host "Failed to create source: $($sourceResult.Error)" -ForegroundColor Red
    }
} else {
    Write-Host "Failed to create protocol: $($createResult.Error)" -ForegroundColor Red
}
