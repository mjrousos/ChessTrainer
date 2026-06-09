<#
.SYNOPSIS
    Run every required ChessTrainer build and report all warnings.

.DESCRIPTION
    Runs (in order):
      1. dotnet build ChessTrainer.sln -c Debug      (warnings are warnings)
      2. dotnet build ChessTrainer.sln -c Release    (warnings become errors via
                                                      TreatWarningsAsErrors)
      3. npm run webpack-prod in src/ChessTrainerApp (front-end build)

    Parses each build's output, deduplicates by (file, line, column, code),
    and emits a JSON report on stdout. Exits 0 when clean, 1 when any warning
    or error is found.

    Per AGENTS.md the project policy is "fix the underlying cause; do not
    suppress". This script is the gate; suppression workarounds
    (#pragma warning disable, <NoWarn>, [SuppressMessage], ruleset
    relaxations) are not acceptable without explicit user approval.

.PARAMETER SkipDebug
    Skip the Debug build.

.PARAMETER SkipRelease
    Skip the Release build.

.PARAMETER SkipFrontend
    Skip the webpack-prod build.

.EXAMPLE
    pwsh ./.agents/skills/zero-warnings-build/check-warnings.ps1
    pwsh ./.agents/skills/zero-warnings-build/check-warnings.ps1 -SkipFrontend
#>
[CmdletBinding()]
param(
    [switch]$SkipDebug,
    [switch]$SkipRelease,
    [switch]$SkipFrontend
)

$ErrorActionPreference = 'Continue'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location $repoRoot

function Parse-DotnetWarnings {
    param(
        [string]$Output,
        [string]$BuildName
    )
    # MSBuild warning/error line format:
    #   path/to/File.cs(line,col): warning|error CODE: message [project.csproj]
    # The trailing project path is optional in some configurations.
    $pattern = '^(?<file>[^()]+?)\((?<line>\d+),(?<col>\d+)\): (?<level>warning|error) (?<code>[A-Z]+\d+): (?<msg>.+?)(?: \[(?<proj>[^\]]+)\])?\s*$'
    $seen = New-Object System.Collections.Generic.HashSet[string]
    $results = @()
    foreach ($line in ($Output -split "`r?`n")) {
        if ($line -match $pattern) {
            $key = "$($Matches.file):$($Matches.line):$($Matches.col):$($Matches.code)"
            if ($seen.Add($key)) {
                $results += [pscustomobject]@{
                    Build   = $BuildName
                    Level   = $Matches.level
                    Code    = $Matches.code
                    File    = $Matches.file
                    Line    = [int]$Matches.line
                    Column  = [int]$Matches.col
                    Message = $Matches.msg.Trim()
                    Project = $Matches.proj
                }
            }
        }
    }
    return ,$results
}

function Parse-WebpackWarnings {
    param([string]$Output)
    # Webpack emits blocks like:
    #   WARNING in ./src/foo.js
    #   <one or more indented or wrapped lines describing the warning>
    #   <blank line>
    # ERROR blocks have the same shape with "ERROR in" instead.
    $results = @()
    $current = $null
    foreach ($line in ($Output -split "`r?`n")) {
        if ($line -match '^(WARNING|ERROR) in (?<file>.+)$') {
            if ($null -ne $current) { $results += $current }
            $level = if ($Matches[1] -eq 'WARNING') { 'warning' } else { 'error' }
            $current = [pscustomobject]@{
                Build   = 'Webpack'
                Level   = $level
                Code    = $null
                File    = $Matches.file.Trim()
                Line    = $null
                Column  = $null
                Message = ''
                Project = 'ChessTrainerApp'
            }
        } elseif ($null -ne $current -and $line.Trim() -ne '') {
            $current.Message = ($current.Message + ' ' + $line.Trim()).Trim()
        } elseif ($null -ne $current -and $line.Trim() -eq '') {
            $results += $current
            $current = $null
        }
    }
    if ($null -ne $current) { $results += $current }
    return ,$results
}

$all = @()
$summary = [ordered]@{}

function Add-StageResult {
    param([string]$Stage, [int]$ExitCode, [array]$Items, [string]$RawOutput)
    $script:all += $Items
    $warningCount = (@($Items | Where-Object { $_.Level -eq 'warning' })).Count
    $errorCount   = (@($Items | Where-Object { $_.Level -eq 'error' })).Count

    # Safety net: if the build failed but we didn't parse any structured diagnostics,
    # capture the tail of the raw output so the agent can still see what went wrong
    # (e.g. MSB1003 project-not-found, NU1101 without a file prefix, npm ERR! lines,
    # build-runner crashes that don't follow the standard "file(line,col):" format).
    if ($ExitCode -ne 0 -and ($warningCount + $errorCount) -eq 0) {
        $tail = ($RawOutput -split "`r?`n" | Where-Object { $_.Trim() -ne '' } | Select-Object -Last 20) -join "`n"
        $script:all += [pscustomobject]@{
            Build    = $Stage
            Level    = 'error'
            Code     = 'UNPARSED'
            File     = $null
            Line     = $null
            Column   = $null
            Message  = "Build failed (exit $ExitCode) but no structured diagnostics matched. Last 20 non-blank output lines:`n$tail"
            Project  = $null
        }
        $errorCount = 1
    }

    $script:summary[$Stage] = [ordered]@{
        exit     = $ExitCode
        warnings = $warningCount
        errors   = $errorCount
    }
    Write-Host ("  exit={0}, warnings={1}, errors={2}" -f $ExitCode, $warningCount, $errorCount)
}

if (-not $SkipDebug) {
    Write-Host '=== Debug build ===' -ForegroundColor Cyan
    $out = & dotnet build ChessTrainer.sln -c Debug --nologo --verbosity:minimal 2>&1 | Out-String
    Add-StageResult -Stage 'Debug' -ExitCode $LASTEXITCODE -Items (Parse-DotnetWarnings -Output $out -BuildName 'Debug') -RawOutput $out
}

if (-not $SkipRelease) {
    Write-Host '=== Release build (TreatWarningsAsErrors=true) ===' -ForegroundColor Cyan
    $out = & dotnet build ChessTrainer.sln -c Release --nologo --verbosity:minimal 2>&1 | Out-String
    Add-StageResult -Stage 'Release' -ExitCode $LASTEXITCODE -Items (Parse-DotnetWarnings -Output $out -BuildName 'Release') -RawOutput $out
}

if (-not $SkipFrontend) {
    Write-Host '=== webpack-prod (front-end) ===' -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot 'src\ChessTrainerApp')
    try {
        if (-not (Test-Path 'node_modules')) {
            Write-Host '  node_modules missing - running npm ci first' -ForegroundColor Yellow
            & npm ci 2>&1 | Out-Null
        }
        $out = & npm run webpack-prod 2>&1 | Out-String
        Add-StageResult -Stage 'Webpack' -ExitCode $LASTEXITCODE -Items (Parse-WebpackWarnings -Output $out) -RawOutput $out
    } finally {
        Pop-Location
    }
}

Write-Host ''
Write-Host '=== Report ===' -ForegroundColor Cyan
[ordered]@{
    summary  = $summary
    warnings = $all
} | ConvertTo-Json -Depth 6

$total = (@($all)).Count
if ($total -gt 0) {
    $totalWarnings = (@($all | Where-Object { $_.Level -eq 'warning' })).Count
    $totalErrors   = (@($all | Where-Object { $_.Level -eq 'error' })).Count
    Write-Host ''
    Write-Warning ("{0} diagnostic(s) found ({1} warning(s), {2} error(s)) - fix the underlying causes (no suppressions without explicit approval)." -f $total, $totalWarnings, $totalErrors)
    exit 1
}
Write-Host 'Clean - no warnings or errors.' -ForegroundColor Green
exit 0
