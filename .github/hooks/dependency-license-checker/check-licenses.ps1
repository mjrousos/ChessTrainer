<#
.SYNOPSIS
    Dependency License Checker Hook (PowerShell port).

.DESCRIPTION
    Scans newly added dependencies (npm, pip, Go, Ruby, Rust) for license
    compliance at session end, before they get committed. Functionally
    equivalent to check-licenses.sh; selected automatically on Windows when
    referenced from the hook's `powershell` field.

    Environment variables (same contract as the bash version):
      LICENSE_MODE        - "warn" (log only) or "block" (exit non-zero on
                            violations) (default: warn)
      SKIP_LICENSE_CHECK  - "true" to disable entirely
      LICENSE_LOG_DIR     - Directory for check logs
                            (default: logs/copilot/license-checker)
      BLOCKED_LICENSES    - Comma-separated SPDX IDs to flag
                            (default: copyleft set)
      LICENSE_ALLOWLIST   - Comma-separated package names to skip
#>

$ErrorActionPreference = 'Continue'

if ($env:SKIP_LICENSE_CHECK -eq 'true') {
    Write-Host 'License check skipped (SKIP_LICENSE_CHECK=true)'
    exit 0
}

# Must be inside a git repo
& git rev-parse --is-inside-work-tree 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Not in a git repository, skipping license check'
    exit 0
}

$mode      = if ($env:LICENSE_MODE)    { $env:LICENSE_MODE }    else { 'warn' }
$logDir    = if ($env:LICENSE_LOG_DIR) { $env:LICENSE_LOG_DIR } else { 'logs/copilot/license-checker' }
$timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logFile = Join-Path $logDir 'check.log'

$defaultBlocked = @(
    'GPL-2.0','GPL-2.0-only','GPL-2.0-or-later',
    'GPL-3.0','GPL-3.0-only','GPL-3.0-or-later',
    'AGPL-1.0','AGPL-3.0','AGPL-3.0-only','AGPL-3.0-or-later',
    'LGPL-2.0','LGPL-2.1','LGPL-2.1-only','LGPL-2.1-or-later',
    'LGPL-3.0','LGPL-3.0-only','LGPL-3.0-or-later',
    'SSPL-1.0','EUPL-1.1','EUPL-1.2','OSL-3.0','CPAL-1.0','CPL-1.0',
    'CC-BY-SA-4.0','CC-BY-NC-4.0','CC-BY-NC-SA-4.0'
)
$blocked   = if ($env:BLOCKED_LICENSES)  { @($env:BLOCKED_LICENSES  -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }) } else { $defaultBlocked }
$allowlist = if ($env:LICENSE_ALLOWLIST) { @($env:LICENSE_ALLOWLIST -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }) } else { @() }

function Test-Allowlisted([string]$Package) {
    foreach ($e in $allowlist) { if ($Package -eq $e) { return $true } }
    return $false
}
function Test-BlockedLicense([string]$License) {
    $l = $License.ToLowerInvariant()
    foreach ($b in $blocked) { if ($l.Contains($b.ToLowerInvariant())) { return $true } }
    return $false
}

# Mirror the bash `timeout 5 <cmd>` wrapper. Some package-manager CLIs (npm
# view, pip show, gem spec, cargo metadata) can hang indefinitely on network
# issues; without a per-call cap the whole hook would blow its 60s budget and
# block the session in `block` mode.
function Invoke-WithTimeout {
    param(
        [Parameter(Mandatory)][scriptblock]$Script,
        [int]$TimeoutSec = 10,
        [object[]]$Args = @()
    )
    $job = Start-Job -ScriptBlock $Script -ArgumentList $Args
    try {
        if (Wait-Job -Job $job -Timeout $TimeoutSec) {
            return Receive-Job -Job $job -ErrorAction SilentlyContinue
        }
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        return $null
    } finally {
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    }
}

function Get-AddedLines([string]$File) {
    if (-not (Test-Path $File)) { return @() }
    $diff = & git diff HEAD -- $File 2>$null
    if (-not $diff) { return @() }
    return @($diff | Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' })
}

# -----------------------------------------------------------------
# Phase 1: detect new deps per ecosystem
# -----------------------------------------------------------------
$newDeps = New-Object System.Collections.Generic.List[pscustomobject]
$npmReserved = @('name','version','description','main','scripts','dependencies','devDependencies','peerDependencies','optionalDependencies')
$rustReserved = @('name','version','edition','authors','description','license','repository','rust-version')

foreach ($l in (Get-AddedLines 'package.json')) {
    if ($l -match '^\+\s*"([^"]+)"\s*:\s*"[^"]*"') {
        $pkg = $Matches[1]
        if ($pkg -notin $npmReserved) {
            $newDeps.Add([pscustomobject]@{ Ecosystem='npm'; Package=$pkg })
        }
    }
}

foreach ($l in (Get-AddedLines 'requirements.txt')) {
    $clean = $l -replace '^\+',''
    if (-not $clean.Trim() -or $clean -match '^\s*#') { continue }
    $pkg = ($clean -split '[><=!~]')[0].Trim()
    if ($pkg) { $newDeps.Add([pscustomobject]@{ Ecosystem='pip'; Package=$pkg }) }
}

foreach ($l in (Get-AddedLines 'pyproject.toml')) {
    if ($l -match '^\+\s*"([A-Za-z0-9_-]+)') {
        $newDeps.Add([pscustomobject]@{ Ecosystem='pip'; Package=$Matches[1] })
    }
}

foreach ($l in (Get-AddedLines 'go.mod')) {
    if ($l -match '^\+\s*([a-zA-Z0-9._/-]+\.[a-zA-Z0-9._/-]+)\s') {
        $pkg = $Matches[1]
        if ($pkg -notin @('module','go','require')) {
            $newDeps.Add([pscustomobject]@{ Ecosystem='go'; Package=$pkg })
        }
    }
}

foreach ($l in (Get-AddedLines 'Gemfile')) {
    if ($l -match "^\+\s*gem\s*['""``]([^'""``]+)['""``]") {
        $newDeps.Add([pscustomobject]@{ Ecosystem='ruby'; Package=$Matches[1] })
    }
}

foreach ($l in (Get-AddedLines 'Cargo.toml')) {
    if ($l -match '^\+\s*([a-zA-Z0-9_-]+)\s*=') {
        $pkg = $Matches[1]
        if ($pkg -notin $rustReserved) {
            $newDeps.Add([pscustomobject]@{ Ecosystem='rust'; Package=$pkg })
        }
    }
}

if ($newDeps.Count -eq 0) {
    Write-Host 'No new dependencies detected'
    (@{ timestamp=$timestamp; event='license_check_complete'; mode=$mode; status='clean'; dependencies_checked=0 } | ConvertTo-Json -Compress) | Add-Content $logFile
    exit 0
}

Write-Host "Checking licenses for $($newDeps.Count) new dependency(ies)..."

# -----------------------------------------------------------------
# Phase 2: license lookup
# -----------------------------------------------------------------
function Get-License([string]$Ecosystem, [string]$Package) {
    $license = 'UNKNOWN'
    try {
        switch ($Ecosystem) {
            'npm' {
                $pkgJson = "node_modules/$Package/package.json"
                if (Test-Path $pkgJson) {
                    try { $license = (Get-Content $pkgJson -Raw | ConvertFrom-Json).license } catch { }
                }
                if (-not $license -or $license -eq 'UNKNOWN') {
                    if (Get-Command npm -ErrorAction SilentlyContinue) {
                        $out = Invoke-WithTimeout -TimeoutSec 10 -Args @($Package) { param($p) & npm view $p license 2>$null }
                        if ($out) { $license = ($out -join '').Trim() }
                    }
                }
            }
            'pip' {
                $pip = Get-Command pip -ErrorAction SilentlyContinue
                if (-not $pip) { $pip = Get-Command pip3 -ErrorAction SilentlyContinue }
                if ($pip) {
                    $out = Invoke-WithTimeout -TimeoutSec 10 -Args @($pip.Source, $Package) { param($cmd, $p) & $cmd show $p 2>$null }
                    $line = $out | Where-Object { $_ -match '^[Ll]icense:' } | Select-Object -First 1
                    if ($line) { $license = ($line -replace '^[Ll]icense:\s*','').Trim() }
                }
            }
            'go' {
                $gopath = if ($env:GOPATH) { $env:GOPATH } else { Join-Path $env:USERPROFILE 'go' }
                $modCache = Join-Path $gopath 'pkg\mod'
                if (Test-Path $modCache) {
                    # Match the bash version's `find -path "*${pkg}@*"`: filter on
                    # FullName, not -Filter (which only matches the directory leaf
                    # and so loses everything before the last path segment for
                    # nested modules like github.com/foo/bar).
                    $pkgPath = $Package -replace '/','\'
                    $cand = Get-ChildItem -Path $modCache -Directory -Recurse -Depth 4 -ErrorAction SilentlyContinue |
                            Where-Object { $_.FullName -like "*$pkgPath@*" } |
                            Select-Object -First 1
                    if ($cand) {
                        $licFile = Get-ChildItem $cand -Filter 'LICENSE*' -File -ErrorAction SilentlyContinue | Select-Object -First 1
                        if ($licFile) {
                            $c = Get-Content $licFile -Raw -ErrorAction SilentlyContinue
                            if     ($c -match 'GNU GENERAL PUBLIC LICENSE') {
                                if     ($c -match 'Version 3') { $license = 'GPL-3.0' }
                                elseif ($c -match 'Version 2') { $license = 'GPL-2.0' }
                                else                            { $license = 'GPL' }
                            }
                            elseif ($c -match 'GNU LESSER GENERAL PUBLIC')  { $license = 'LGPL' }
                            elseif ($c -match 'GNU AFFERO GENERAL PUBLIC')  { $license = 'AGPL-3.0' }
                            elseif ($c -match 'MIT License')                { $license = 'MIT' }
                            elseif ($c -match 'Apache License')             { $license = 'Apache-2.0' }
                            elseif ($c -match 'BSD')                        { $license = 'BSD' }
                        }
                    }
                }
            }
            'ruby' {
                if (Get-Command gem -ErrorAction SilentlyContinue) {
                    $out = Invoke-WithTimeout -TimeoutSec 10 -Args @($Package) { param($p) & gem spec $p license 2>$null }
                    $line = $out | Where-Object { $_ -notmatch '^---' -and $_ -notmatch '^\.\.\.' } | Select-Object -First 1
                    if ($line) { $license = ($line -replace '^-\s*','').Trim() }
                }
            }
            'rust' {
                if (Get-Command cargo -ErrorAction SilentlyContinue) {
                    $metaJson = Invoke-WithTimeout -TimeoutSec 10 { & cargo metadata --format-version 1 2>$null }
                    if ($metaJson) {
                        try {
                            $meta = ($metaJson -join "`n") | ConvertFrom-Json
                            $pkgMeta = $meta.packages | Where-Object { $_.name -eq $Package } | Select-Object -First 1
                            if ($pkgMeta -and $pkgMeta.license) { $license = $pkgMeta.license }
                        } catch { }
                    }
                }
            }
        }
    } catch { }
    $license = ($license -as [string]).Trim()
    if (-not $license) { $license = 'UNKNOWN' }
    return $license
}

# -----------------------------------------------------------------
# Phases 3-5: classify + report + log
# -----------------------------------------------------------------
$results       = @()
$violations    = @()
$findingCount  = 0

foreach ($d in $newDeps) {
    $license = Get-License $d.Ecosystem $d.Package
    $status  = 'OK'
    if (Test-Allowlisted $d.Package) {
        $status = 'ALLOWLISTED'
    } elseif (Test-BlockedLicense $license) {
        $status = 'BLOCKED'
        $violations += [pscustomobject]@{ package=$d.Package; ecosystem=$d.Ecosystem; license=$license; status='BLOCKED' }
        $findingCount++
    }
    $results += [pscustomobject]@{ Ecosystem=$d.Ecosystem; Package=$d.Package; License=$license; Status=$status }
}

Write-Host ''
Write-Host ("  {0,-30} {1,-12} {2,-30} {3}" -f 'PACKAGE','ECOSYSTEM','LICENSE','STATUS')
Write-Host ("  {0,-30} {1,-12} {2,-30} {3}" -f '-------','---------','-------','------')
foreach ($r in $results) {
    Write-Host ("  {0,-30} {1,-12} {2,-30} {3}" -f $r.Package, $r.Ecosystem, $r.License, $r.Status)
}
Write-Host ''

$logEntry = [ordered]@{
    timestamp            = $timestamp
    event                = 'license_check_complete'
    mode                 = $mode
    dependencies_checked = $results.Count
    violation_count      = $findingCount
    violations           = $violations
} | ConvertTo-Json -Compress -Depth 4
Add-Content -Path $logFile -Value $logEntry

if ($findingCount -gt 0) {
    Write-Host "Found $findingCount license violation(s):"
    Write-Host ''
    foreach ($v in $violations) { Write-Host "  - $($v.package) ($($v.ecosystem)): $($v.license)" }
    Write-Host ''
    if ($mode -eq 'block') {
        Write-Host 'Session blocked: resolve license violations above before committing.'
        Write-Host '   Set LICENSE_MODE=warn to log without blocking, or add packages to LICENSE_ALLOWLIST.'
        exit 1
    } else {
        Write-Host 'Review the violations above. Set LICENSE_MODE=block to prevent commits with license issues.'
    }
} else {
    Write-Host "All $($results.Count) dependencies have compliant licenses"
}

exit 0
