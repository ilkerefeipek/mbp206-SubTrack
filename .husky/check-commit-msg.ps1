param([Parameter(Mandatory = $true)][string]$MessageFile)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $MessageFile)) {
    Write-Error "Commit message file not found: $MessageFile"
    exit 1
}

$raw = (Get-Content $MessageFile -Raw)
$firstLine = ($raw -split "`r?`n")[0].Trim()

if ([string]::IsNullOrWhiteSpace($firstLine)) {
    Write-Error "Commit mesaji bos olamaz."
    exit 1
}

# Allow merge / revert / fixup commits to pass through
if ($firstLine -match '^(Merge|Revert|fixup!|squash!)\b') {
    exit 0
}

$pattern = '^(feat|fix|docs|style|refactor|test|chore|perf|build|ci|revert)(\([^)]+\))?!?: .{1,100}$'

if ($firstLine -notmatch $pattern) {
    $msg = @"
Commit mesaji Conventional Commits formatinda olmali.

Format : <tur>(<kapsam>?): <aciklama>
Ornek  : feat(auth): add JWT bearer middleware
         fix(seed): correct unused alert message
         chore(s0): bootstrap dotnet skeleton

Izin verilen turler:
  feat, fix, docs, style, refactor, test, chore, perf, build, ci, revert

Mevcut mesaj: $firstLine
"@
    Write-Error $msg
    exit 1
}

exit 0
