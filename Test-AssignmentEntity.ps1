# Test script for AssignmentEntity
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

Write-Host "TESTING ASSIGNMENTENTITY CRUD OPERATIONS" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Test 1: Check if API is running
Write-Host "1. Testing API connectivity..." -ForegroundColor Yellow
$healthCheck = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments"
if (-not $healthCheck.Success) {
    Write-Host "   ❌ API is not running or assignments endpoint not available" -ForegroundColor Red
    Write-Host "   Error: $($healthCheck.Error)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start the API first with:" -ForegroundColor Yellow
    Write-Host "   dotnet run --project src/EntitiesManager/EntitiesManager.Api/EntitiesManager.Api.csproj" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "   ✅ API is running and assignments endpoint is available" -ForegroundColor Green
    Write-Host "   Found $($healthCheck.Data.Count) existing assignments" -ForegroundColor Gray
}

# Test 2: Create a new assignment
Write-Host "2. Creating a new assignment..." -ForegroundColor Yellow
$timestamp = Get-Date -Format 'HHmmss'
$assignmentData = @{
    version = "1.0.$timestamp"
    name = "Test Assignment $timestamp"
    description = "Test assignment created by PowerShell script"
    stepId = [System.Guid]::NewGuid().ToString()
    entityIds = @(
        [System.Guid]::NewGuid().ToString(),
        [System.Guid]::NewGuid().ToString()
    )
}

$createResult = Invoke-ApiCall -Method "POST" -Endpoint "/api/assignments" -Body $assignmentData
if ($createResult.Success) {
    $assignmentId = $createResult.Data.id
    Write-Host "   ✅ Assignment created successfully" -ForegroundColor Green
    Write-Host "   ID: $assignmentId" -ForegroundColor Gray
    Write-Host "   Version: $($createResult.Data.version)" -ForegroundColor Gray
    Write-Host "   Name: $($createResult.Data.name)" -ForegroundColor Gray
    Write-Host "   StepId: $($createResult.Data.stepId)" -ForegroundColor Gray
    Write-Host "   EntityIds: $($createResult.Data.entityIds -join ', ')" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to create assignment" -ForegroundColor Red
    Write-Host "   Error: $($createResult.Error)" -ForegroundColor Red
    exit 1
}

# Test 3: Get assignment by ID
Write-Host "3. Retrieving assignment by ID..." -ForegroundColor Yellow
$getResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/$assignmentId"
if ($getResult.Success) {
    Write-Host "   ✅ Assignment retrieved successfully" -ForegroundColor Green
    Write-Host "   Name: $($getResult.Data.name)" -ForegroundColor Gray
    Write-Host "   Created: $($getResult.Data.createdAt)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to retrieve assignment" -ForegroundColor Red
    Write-Host "   Error: $($getResult.Error)" -ForegroundColor Red
}

# Test 4: Get assignment by composite key
Write-Host "4. Retrieving assignment by composite key..." -ForegroundColor Yellow
$stepId = $assignmentData.stepId
$getByKeyResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/by-key/$stepId"
if ($getByKeyResult.Success) {
    Write-Host "   ✅ Assignment retrieved by composite key successfully" -ForegroundColor Green
    Write-Host "   Composite Key: $($getByKeyResult.Data.stepId)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to retrieve assignment by composite key" -ForegroundColor Red
    Write-Host "   Error: $($getByKeyResult.Error)" -ForegroundColor Red
}

# Test 5: Update assignment
Write-Host "5. Updating assignment..." -ForegroundColor Yellow
$updateData = $createResult.Data
$updateData.name = "Updated Test Assignment $timestamp"
$updateData.description = "Updated description"
$updateData.configuration.priority = "medium"

$updateResult = Invoke-ApiCall -Method "PUT" -Endpoint "/api/assignments/$assignmentId" -Body $updateData
if ($updateResult.Success) {
    Write-Host "   ✅ Assignment updated successfully" -ForegroundColor Green
    Write-Host "   New Name: $($updateResult.Data.name)" -ForegroundColor Gray
    Write-Host "   Updated: $($updateResult.Data.updatedAt)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to update assignment" -ForegroundColor Red
    Write-Host "   Error: $($updateResult.Error)" -ForegroundColor Red
}

# Test 6: Get all assignments
Write-Host "6. Retrieving all assignments..." -ForegroundColor Yellow
$getAllResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments"
if ($getAllResult.Success) {
    Write-Host "   ✅ Retrieved all assignments successfully" -ForegroundColor Green
    Write-Host "   Total count: $($getAllResult.Data.Count)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Failed to retrieve all assignments" -ForegroundColor Red
    Write-Host "   Error: $($getAllResult.Error)" -ForegroundColor Red
}

# Test 7: Delete assignment
Write-Host "7. Deleting assignment..." -ForegroundColor Yellow
$deleteResult = Invoke-ApiCall -Method "DELETE" -Endpoint "/api/assignments/$assignmentId"
if ($deleteResult.Success) {
    Write-Host "   ✅ Assignment deleted successfully" -ForegroundColor Green
} else {
    Write-Host "   ❌ Failed to delete assignment" -ForegroundColor Red
    Write-Host "   Error: $($deleteResult.Error)" -ForegroundColor Red
}

# Test 8: Verify deletion
Write-Host "8. Verifying deletion..." -ForegroundColor Yellow
$verifyResult = Invoke-ApiCall -Method "GET" -Endpoint "/api/assignments/$assignmentId"
if (-not $verifyResult.Success -and $verifyResult.StatusCode -eq 404) {
    Write-Host "   ✅ Assignment deletion verified (404 Not Found)" -ForegroundColor Green
} else {
    Write-Host "   ❌ Assignment still exists after deletion" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 AssignmentEntity testing completed!" -ForegroundColor Green
Write-Host "All CRUD operations have been tested." -ForegroundColor Cyan
