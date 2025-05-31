# Verification Script for ScheduledFlowEntity → OrchestratedFlowEntity Rename
# Checks that all references have been properly updated

param(
    [string]$SourcePath = "c:\Users\Administrator\source\repos\Design4\src\EntitiesManager"
)

Write-Host "=== OrchestratedFlowEntity Rename Verification ===" -ForegroundColor Cyan
Write-Host "Source Path: $SourcePath" -ForegroundColor Yellow
Write-Host ""

$ErrorActionPreference = "Continue"

# Function to search for old references
function Search-OldReferences {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Description
    )
    
    Write-Host "Checking: $Description" -ForegroundColor White
    
    try {
        $results = Get-ChildItem -Path $Path -Recurse -Include "*.cs" | 
                   Select-String -Pattern $Pattern -AllMatches
        
        if ($results) {
            Write-Host "❌ Found old references:" -ForegroundColor Red
            foreach ($result in $results) {
                Write-Host "  $($result.Filename):$($result.LineNumber) - $($result.Line.Trim())" -ForegroundColor Red
            }
            return $false
        } else {
            Write-Host "✅ No old references found" -ForegroundColor Green
            return $true
        }
    }
    catch {
        Write-Host "⚠️  Error searching: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

# Function to verify new references exist
function Verify-NewReferences {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Description
    )
    
    Write-Host "Verifying: $Description" -ForegroundColor White
    
    try {
        $results = Get-ChildItem -Path $Path -Recurse -Include "*.cs" | 
                   Select-String -Pattern $Pattern -AllMatches
        
        if ($results) {
            Write-Host "✅ Found new references: $($results.Count) occurrences" -ForegroundColor Green
            return $true
        } else {
            Write-Host "❌ No new references found" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "⚠️  Error searching: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

# Function to check file existence
function Check-FileExists {
    param(
        [string]$FilePath,
        [string]$Description
    )
    
    Write-Host "Checking: $Description" -ForegroundColor White
    
    if (Test-Path $FilePath) {
        Write-Host "✅ File exists: $FilePath" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ File missing: $FilePath" -ForegroundColor Red
        return $false
    }
}

# Function to check file does not exist
function Check-FileNotExists {
    param(
        [string]$FilePath,
        [string]$Description
    )
    
    Write-Host "Checking: $Description" -ForegroundColor White
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "✅ Old file properly removed: $FilePath" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ Old file still exists: $FilePath" -ForegroundColor Red
        return $false
    }
}

$allChecks = @()

Write-Host "=== 1. Checking for Old References ===" -ForegroundColor Magenta

# Check for old entity references
$allChecks += Search-OldReferences -Path $SourcePath -Pattern "ScheduledFlowEntity" -Description "Old ScheduledFlowEntity references"
$allChecks += Search-OldReferences -Path $SourcePath -Pattern "IScheduledFlowEntityRepository" -Description "Old repository interface references"
$allChecks += Search-OldReferences -Path $SourcePath -Pattern "ScheduledFlowEntityRepository" -Description "Old repository class references"
$allChecks += Search-OldReferences -Path $SourcePath -Pattern "CreateScheduledFlowCommand" -Description "Old command references"
$allChecks += Search-OldReferences -Path $SourcePath -Pattern "ScheduledFlowsController" -Description "Old controller references"

Write-Host ""
Write-Host "=== 2. Verifying New References Exist ===" -ForegroundColor Magenta

# Check for new entity references
$allChecks += Verify-NewReferences -Path $SourcePath -Pattern "OrchestratedFlowEntity" -Description "New OrchestratedFlowEntity references"
$allChecks += Verify-NewReferences -Path $SourcePath -Pattern "IOrchestratedFlowEntityRepository" -Description "New repository interface references"
$allChecks += Verify-NewReferences -Path $SourcePath -Pattern "OrchestratedFlowEntityRepository" -Description "New repository class references"
$allChecks += Verify-NewReferences -Path $SourcePath -Pattern "CreateOrchestratedFlowCommand" -Description "New command references"
$allChecks += Verify-NewReferences -Path $SourcePath -Pattern "OrchestratedFlowsController" -Description "New controller references"

Write-Host ""
Write-Host "=== 3. Checking File Structure ===" -ForegroundColor Magenta

# Check that new files exist
$allChecks += Check-FileExists -FilePath "$SourcePath\EntitiesManager.Core\Entities\OrchestratedFlowEntity.cs" -Description "New entity file"
$allChecks += Check-FileExists -FilePath "$SourcePath\EntitiesManager.Core\Interfaces\Repositories\IOrchestratedFlowEntityRepository.cs" -Description "New repository interface file"
$allChecks += Check-FileExists -FilePath "$SourcePath\EntitiesManager.Infrastructure\Repositories\OrchestratedFlowEntityRepository.cs" -Description "New repository implementation file"
$allChecks += Check-FileExists -FilePath "$SourcePath\EntitiesManager.Api\Controllers\OrchestratedFlowsController.cs" -Description "New controller file"

# Check that old files are removed
$allChecks += Check-FileNotExists -FilePath "$SourcePath\EntitiesManager.Core\Entities\ScheduledFlowEntity.cs" -Description "Old entity file removal"
$allChecks += Check-FileNotExists -FilePath "$SourcePath\EntitiesManager.Core\Interfaces\Repositories\IScheduledFlowEntityRepository.cs" -Description "Old repository interface file removal"
$allChecks += Check-FileNotExists -FilePath "$SourcePath\EntitiesManager.Infrastructure\Repositories\ScheduledFlowEntityRepository.cs" -Description "Old repository implementation file removal"
$allChecks += Check-FileNotExists -FilePath "$SourcePath\EntitiesManager.Api\Controllers\ScheduledFlowsController.cs" -Description "Old controller file removal"

Write-Host ""
Write-Host "=== 4. Checking MassTransit Structure ===" -ForegroundColor Magenta

# Check MassTransit files
$allChecks += Check-FileExists -FilePath "$SourcePath\EntitiesManager.Infrastructure\MassTransit\Commands\OrchestratedFlowCommands.cs" -Description "New MassTransit commands file"
$allChecks += Check-FileExists -FilePath "$SourcePath\EntitiesManager.Infrastructure\MassTransit\Consumers\OrchestratedFlow\CreateOrchestratedFlowCommandConsumer.cs" -Description "New MassTransit consumer file"
$allChecks += Check-FileNotExists -FilePath "$SourcePath\EntitiesManager.Infrastructure\MassTransit\Commands\ScheduledFlowCommands.cs" -Description "Old MassTransit commands file removal"
$allChecks += Check-FileNotExists -FilePath "$SourcePath\EntitiesManager.Infrastructure\MassTransit\Consumers\ScheduledFlow" -Description "Old MassTransit consumer directory removal"

Write-Host ""
Write-Host "=== VERIFICATION RESULTS ===" -ForegroundColor Cyan

$passedChecks = ($allChecks | Where-Object { $_ -eq $true }).Count
$totalChecks = $allChecks.Count

Write-Host ""
Write-Host "OVERALL RESULT: $passedChecks/$totalChecks checks passed" -ForegroundColor $(if ($passedChecks -eq $totalChecks) { "Green" } else { "Red" })

if ($passedChecks -eq $totalChecks) {
    Write-Host "🎉 RENAME VERIFICATION SUCCESSFUL!" -ForegroundColor Green
    Write-Host "All ScheduledFlowEntity references have been properly renamed to OrchestratedFlowEntity." -ForegroundColor Green
} else {
    Write-Host "⚠️  Some verification checks failed. Please review the issues above." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Build the solution to ensure compilation success" -ForegroundColor White
Write-Host "2. Run integration tests to verify functionality" -ForegroundColor White
Write-Host "3. Update database collections from 'scheduledflows' to 'orchestratedflows'" -ForegroundColor White
Write-Host "4. Update API documentation and client applications" -ForegroundColor White

Write-Host ""
Write-Host "=== Verification Completed ===" -ForegroundColor Cyan
