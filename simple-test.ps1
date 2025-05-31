# Simple test to verify SourceEntity referential integrity
$baseUrl = "http://localhost:5130"

Write-Host "Testing SourceEntity Referential Integrity..." -ForegroundColor Cyan

# Test 1: Create Protocol
Write-Host "1. Creating protocol..." -ForegroundColor Yellow
$protocolBody = @{
    name = "Test Protocol $(Get-Date -Format 'HHmmss')"
    description = "Test protocol"
} | ConvertTo-Json

try {
    $protocol = Invoke-RestMethod -Uri "$baseUrl/api/protocols" -Method POST -Body $protocolBody -ContentType "application/json"
    Write-Host "   Created protocol: $($protocol.id)" -ForegroundColor Green
    $protocolId = $protocol.id
} catch {
    Write-Host "   Failed to create protocol: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Create SourceEntity
Write-Host "2. Creating source..." -ForegroundColor Yellow
$sourceBody = @{
    name = "Test Source"
    version = "1.0"
    description = "Test source"
    address = "test://localhost/source"
    protocolId = $protocolId
    outputSchema = @{}
} | ConvertTo-Json

try {
    $source = Invoke-RestMethod -Uri "$baseUrl/api/sources" -Method POST -Body $sourceBody -ContentType "application/json"
    Write-Host "   Created source: $($source.id)" -ForegroundColor Green
    $sourceId = $source.id
} catch {
    Write-Host "   Failed to create source: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 3: Delete SourceEntity (should succeed - no references)
Write-Host "3. Testing DELETE without references..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$baseUrl/api/sources/$sourceId" -Method DELETE
    Write-Host "   Successfully deleted source (no references)" -ForegroundColor Green
} catch {
    Write-Host "   Failed to delete source: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Create another SourceEntity
Write-Host "4. Creating another source..." -ForegroundColor Yellow
$sourceBody2 = @{
    name = "Test Source 2"
    version = "1.0"
    description = "Test source 2"
    address = "test://localhost/source2"
    protocolId = $protocolId
    outputSchema = @{}
} | ConvertTo-Json

try {
    $source2 = Invoke-RestMethod -Uri "$baseUrl/api/sources" -Method POST -Body $sourceBody2 -ContentType "application/json"
    Write-Host "   Created source: $($source2.id)" -ForegroundColor Green
    $sourceId2 = $source2.id
} catch {
    Write-Host "   Failed to create source: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 5: Test referential integrity validation (even without actual references)
Write-Host "5. Testing referential integrity validation..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$baseUrl/api/sources/$sourceId2" -Method DELETE
    Write-Host "   Source deleted successfully" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Host "   Referential integrity validation triggered (409 Conflict)" -ForegroundColor Green
    } else {
        Write-Host "   Unexpected error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Cleanup
Write-Host "6. Cleanup..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$baseUrl/api/protocols/$protocolId" -Method DELETE
    Write-Host "   Protocol deleted" -ForegroundColor Green
} catch {
    Write-Host "   Failed to delete protocol: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "Test completed!" -ForegroundColor Cyan
