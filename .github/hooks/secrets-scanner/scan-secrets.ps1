<#
.SYNOPSIS
    Secrets Scanner Hook (PowerShell port).

.DESCRIPTION
    Scans files modified during a Copilot coding agent session for accidentally
    leaked secrets, credentials, and sensitive data. Functionally equivalent to
    scan-secrets.sh; selected automatically on Windows when referenced from the
    hook's `powershell` field.

    Environment variables (same contract as the bash version):
      SCAN_MODE          - "warn" (log only) or "block" (exit non-zero on
                           findings) (default: warn)
      SCAN_SCOPE         - "diff" (changed files vs HEAD) or "staged" (staged
                           files only) (default: diff)
      SKIP_SECRETS_SCAN  - "true" to disable scanning entirely
      SECRETS_LOG_DIR    - Directory for scan logs (default: logs/copilot/secrets)
      SECRETS_ALLOWLIST  - Comma-separated list of substrings to ignore
#>

$ErrorActionPreference = 'Continue'

# Pattern table: Name | Severity | Regex
# POSIX [[:space:]] is translated to \s for .NET regex; otherwise patterns
# mirror scan-secrets.sh.
$patterns = @(
    @{ Name='AWS_ACCESS_KEY';        Severity='critical'; Regex='AKIA[0-9A-Z]{16}' },
    @{ Name='AWS_SECRET_KEY';        Severity='critical'; Regex='aws_secret_access_key\s*[:=]\s*[''"]?[A-Za-z0-9/+=]{40}' },
    @{ Name='GCP_SERVICE_ACCOUNT';   Severity='critical'; Regex='"type"\s*:\s*"service_account"' },
    @{ Name='GCP_API_KEY';           Severity='high';     Regex='AIza[0-9A-Za-z_-]{35}' },
    @{ Name='AZURE_CLIENT_SECRET';   Severity='critical'; Regex='azure[_-]?client[_-]?secret\s*[:=]\s*[''"]?[A-Za-z0-9_~.-]{34,}' },
    @{ Name='GITHUB_PAT';            Severity='critical'; Regex='ghp_[0-9A-Za-z]{36}' },
    @{ Name='GITHUB_OAUTH';          Severity='critical'; Regex='gho_[0-9A-Za-z]{36}' },
    @{ Name='GITHUB_APP_TOKEN';      Severity='critical'; Regex='ghs_[0-9A-Za-z._-]{36,}' },
    @{ Name='GITHUB_REFRESH_TOKEN';  Severity='critical'; Regex='ghr_[0-9A-Za-z]{36}' },
    @{ Name='GITHUB_FINE_GRAINED_PAT'; Severity='critical'; Regex='github_pat_[0-9A-Za-z_]{82}' },
    @{ Name='PRIVATE_KEY';           Severity='critical'; Regex='-----BEGIN (RSA |EC |OPENSSH |DSA |PGP )?PRIVATE KEY-----' },
    @{ Name='PGP_PRIVATE_BLOCK';     Severity='critical'; Regex='-----BEGIN PGP PRIVATE KEY BLOCK-----' },
    @{ Name='GENERIC_SECRET';        Severity='high';     Regex='(secret|token|password|passwd|pwd|api[_-]?key|apikey|access[_-]?key|auth[_-]?token|client[_-]?secret)\s*[:=]\s*[''"]?[A-Za-z0-9_/+=~.-]{8,}' },
    @{ Name='CONNECTION_STRING';     Severity='high';     Regex='(mongodb(\+srv)?|postgres(ql)?|mysql|redis|amqp|mssql)://[^\s''"]{10,}' },
    @{ Name='BEARER_TOKEN';          Severity='medium';   Regex='[Bb]earer\s+[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}' },
    @{ Name='SLACK_TOKEN';           Severity='high';     Regex='xox[baprs]-[0-9]{10,}-[0-9A-Za-z-]+' },
    @{ Name='SLACK_WEBHOOK';         Severity='high';     Regex='https://hooks\.slack\.com/services/T[0-9A-Z]{8,}/B[0-9A-Z]{8,}/[0-9A-Za-z]{24}' },
    @{ Name='DISCORD_TOKEN';         Severity='high';     Regex='[MN][A-Za-z0-9]{23,}\.[A-Za-z0-9_-]{6}\.[A-Za-z0-9_-]{27,}' },
    @{ Name='TWILIO_API_KEY';        Severity='high';     Regex='SK[0-9a-fA-F]{32}' },
    @{ Name='SENDGRID_API_KEY';      Severity='high';     Regex='SG\.[0-9A-Za-z_-]{22}\.[0-9A-Za-z_-]{43}' },
    @{ Name='STRIPE_SECRET_KEY';     Severity='critical'; Regex='sk_live_[0-9A-Za-z]{24,}' },
    @{ Name='STRIPE_RESTRICTED_KEY'; Severity='high';     Regex='rk_live_[0-9A-Za-z]{24,}' },
    @{ Name='NPM_TOKEN';             Severity='high';     Regex='npm_[0-9A-Za-z]{36}' },
    @{ Name='JWT_TOKEN';             Severity='medium';   Regex='eyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}' },
    @{ Name='INTERNAL_IP_PORT';      Severity='medium';   Regex='(^|[^.0-9])(10\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}|172\.(1[6-9]|2[0-9]|3[01])\.[0-9]{1,3}\.[0-9]{1,3}|192\.168\.[0-9]{1,3}\.[0-9]{1,3}):[0-9]{2,5}([^0-9]|$)' }
)

if ($env:SKIP_SECRETS_SCAN -eq 'true') {
    Write-Host 'Secrets scan skipped (SKIP_SECRETS_SCAN=true)'
    exit 0
}

& git rev-parse --is-inside-work-tree 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Not in a git repository, skipping secrets scan'
    exit 0
}

$mode      = if ($env:SCAN_MODE)        { $env:SCAN_MODE }        else { 'warn' }
$scope     = if ($env:SCAN_SCOPE)       { $env:SCAN_SCOPE }       else { 'diff' }
$logDir    = if ($env:SECRETS_LOG_DIR)  { $env:SECRETS_LOG_DIR }  else { 'logs/copilot/secrets' }
$timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logFile = Join-Path $logDir 'scan.log'

# Collect files to scan
$files = New-Object System.Collections.Generic.List[string]
if ($scope -eq 'staged') {
    foreach ($f in (& git diff --cached --name-only --diff-filter=ACMR 2>$null)) {
        if ($f) { $files.Add($f) }
    }
} else {
    foreach ($f in (& git diff --name-only --diff-filter=ACMR HEAD 2>$null)) {
        if ($f) { $files.Add($f) }
    }
    foreach ($f in (& git ls-files --others --exclude-standard 2>$null)) {
        if ($f) { $files.Add($f) }
    }
}

if ($files.Count -eq 0) {
    Write-Host 'No modified files to scan'
    (@{ timestamp=$timestamp; event='scan_complete'; mode=$mode; scope=$scope; status='clean'; files_scanned=0 } | ConvertTo-Json -Compress) | Add-Content $logFile
    exit 0
}

$allowlist = if ($env:SECRETS_ALLOWLIST) {
    @($env:SECRETS_ALLOWLIST -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
} else { @() }

function Test-Allowlisted([string]$Match) {
    foreach ($p in $allowlist) {
        if ($Match -like "*$p*") { return $true }
    }
    return $false
}

# Text-file detection: include common text extensions; treat unknown as binary
$textExtensions = @(
    '.md','.txt','.json','.yaml','.yml','.xml','.toml','.ini','.cfg','.conf',
    '.sh','.bash','.zsh','.ps1','.bat','.cmd',
    '.py','.rb','.js','.ts','.jsx','.tsx','.go','.rs','.java','.kt','.cs','.cpp','.c','.h',
    '.php','.swift','.scala','.r','.lua','.pl','.ex','.exs','.hs','.ml',
    '.html','.css','.scss','.less','.svg',
    '.sql','.graphql','.proto',
    '.env','.properties',
    # Razor / Blazor / build / config
    '.razor','.cshtml','.csproj','.props','.targets','.sln','.editorconfig','.gitignore','.gitattributes'
)
$textNames = @('Dockerfile','Makefile','Vagrantfile','Gemfile','Rakefile')
$skipNames = @('package-lock.json','yarn.lock','pnpm-lock.yaml','Cargo.lock','go.sum')

function Test-IsTextFile([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $name = Split-Path -Leaf $Path
    $ext  = [System.IO.Path]::GetExtension($Path).ToLowerInvariant()
    if ($ext -in $textExtensions) { return $true }
    foreach ($n in $textNames) { if ($name -like "$n*") { return $true } }
    # Heuristic: sample first 4KB; if it has many NUL bytes, treat as binary
    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        $sample = if ($bytes.Length -gt 4096) { $bytes[0..4095] } else { $bytes }
        $nullCount = ($sample | Where-Object { $_ -eq 0 }).Count
        return ($nullCount -eq 0)
    } catch { return $false }
}

$findings    = @()
$findingCount = 0
$placeholderRegex = '(?i)(example|placeholder|your[_-]|xxx|changeme|TODO|FIXME|replace[_-]?me|dummy|fake|test[_-]?key|sample)'

function Scan-File([string]$FilePath, [string]$ReadPath = $null) {
    if (-not $ReadPath) { $ReadPath = $FilePath }
    if (-not (Test-Path -LiteralPath $ReadPath)) { return }
    # Self-exclude this hook's own files: pattern definitions in the script
    # and example credentials in the README would otherwise produce false
    # positives every session. Also exclude copilot-customization.md, which
    # documents the same hooks and references the same trigger strings.
    # Normalized to forward slashes so a single check covers Windows + Linux.
    $normalized = $FilePath -replace '\\','/'
    if ($normalized -like '.github/hooks/secrets-scanner/*' -or
        $normalized -eq 'copilot-customization.md') { return }
    $name = Split-Path -Leaf $FilePath
    if ($name -in $skipNames -or $FilePath -match '\.lock$|\.sum$') { return }
    if (-not (Test-IsTextFile $ReadPath)) { return }

    # Force array semantics: Get-Content returns a scalar [string] for a
    # one-line file, which would then be indexed character-by-character and
    # silently miss any secret in single-line files (.env, compact JSON, etc.).
    $lines = @(Get-Content -LiteralPath $ReadPath -ErrorAction SilentlyContinue)
    if ($lines.Count -eq 0) { return }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        foreach ($p in $patterns) {
            $m = [regex]::Match($line, $p.Regex)
            if (-not $m.Success) { continue }
            $match = $m.Value
            if ($p.Name -eq 'INTERNAL_IP_PORT') {
                $ipm = [regex]::Match($match, '\d+\.\d+\.\d+\.\d+:\d+')
                if (-not $ipm.Success) { continue }
                $match = $ipm.Value
            }
            if ($allowlist.Count -gt 0 -and (Test-Allowlisted $match)) { continue }
            if ($match -match $placeholderRegex) { continue }

            $redacted = if ($match.Length -le 12) { '[REDACTED]' } else { "$($match.Substring(0,4))...$($match.Substring($match.Length-4,4))" }

            $script:findings += [pscustomobject]@{
                file     = $FilePath
                line     = $i + 1
                pattern  = $p.Name
                severity = $p.Severity
                match    = $redacted
            }
            $script:findingCount++
        }
    }
}

Write-Host "Scanning $($files.Count) modified file(s) for secrets..."

foreach ($f in $files) {
    if ($scope -eq 'staged') {
        $tmp = [System.IO.Path]::GetTempFileName()
        try {
            & git show ":$f" > $tmp 2>$null
            Scan-File -FilePath $f -ReadPath $tmp
        } finally {
            Remove-Item $tmp -ErrorAction SilentlyContinue
        }
    } else {
        Scan-File -FilePath $f
    }
}

if ($findingCount -gt 0) {
    Write-Host ''
    Write-Host "Found $findingCount potential secret(s) in modified files:"
    Write-Host ''
    Write-Host ("  {0,-40} {1,-6} {2,-28} {3}" -f 'FILE','LINE','PATTERN','SEVERITY')
    Write-Host ("  {0,-40} {1,-6} {2,-28} {3}" -f '----','----','-------','--------')
    foreach ($f in $findings) {
        Write-Host ("  {0,-40} {1,-6} {2,-28} {3}" -f $f.file, $f.line, $f.pattern, $f.severity)
    }
    Write-Host ''

    $logEntry = [ordered]@{
        timestamp     = $timestamp
        event         = 'secrets_found'
        mode          = $mode
        scope         = $scope
        files_scanned = $files.Count
        finding_count = $findingCount
        findings      = $findings
    } | ConvertTo-Json -Compress -Depth 4
    Add-Content -Path $logFile -Value $logEntry

    if ($mode -eq 'block') {
        Write-Host 'Session blocked: resolve the findings above before committing.'
        Write-Host '   Set SCAN_MODE=warn to log without blocking, or add patterns to SECRETS_ALLOWLIST.'
        exit 1
    } else {
        Write-Host 'Review the findings above. Set SCAN_MODE=block to prevent commits with secrets.'
    }
} else {
    Write-Host "No secrets detected in $($files.Count) scanned file(s)"
    (@{ timestamp=$timestamp; event='scan_complete'; mode=$mode; scope=$scope; status='clean'; files_scanned=$files.Count } | ConvertTo-Json -Compress) | Add-Content $logFile
}

exit 0
