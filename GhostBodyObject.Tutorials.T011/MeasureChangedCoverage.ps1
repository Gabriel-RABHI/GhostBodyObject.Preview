param(
    [string]$Baseline = '0c5a5a1d62b7735038dc1be7837cc7ab01c8754a',
    [Parameter(Mandatory)][string[]]$Reports
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$changed = @{}
$covered = @{}
$production = '^GhostBodyObject\.(Runtime|Generator|Tutorials\.T011\.(Catalog|Comments|Billing))/.*\.cs$'
Push-Location $root
try {
    # The zero-context diff identifies added/replaced lines against the task's starting commit.
    $file = $null
    foreach ($line in (git -c core.safecrlf=false diff --no-ext-diff --unified=0 $Baseline -- '*.cs')) {
        if ($line -match '^\+\+\+ b/(.+)$') {
            $file = $Matches[1]
            if ($file -match $production) { $changed[$file] = [Collections.Generic.HashSet[int]]::new() }
        }
        elseif ($file -and $changed.ContainsKey($file) -and $line -match '^@@ .* \+(\d+)(?:,(\d+))? @@') {
            $first = [int]$Matches[1]
            $count = if ($Matches[2]) { [int]$Matches[2] } else { 1 }
            for ($i = 0; $i -lt $count; $i++) { [void]$changed[$file].Add($first + $i) }
        }
    }
    foreach ($file in (git ls-files --others --exclude-standard -- '*.cs')) {
        if ($file -notmatch $production) { continue }
        $changed[$file] = [Collections.Generic.HashSet[int]]::new()
        for ($i = 1; $i -le [IO.File]::ReadAllLines((Join-Path $root $file)).Length; $i++) { [void]$changed[$file].Add($i) }
    }

    # Union sequence points and hits across test projects; count a source line once.
    # Generated code, test code, attributes and declarations without sequence points are excluded.
    foreach ($report in $Reports) {
        [xml]$xml = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $report))
        foreach ($class in $xml.SelectNodes('/coverage/packages/package/classes/class')) {
            $file = $null
            foreach ($source in $xml.coverage.sources.source) {
                $absolute = [IO.Path]::GetFullPath((Join-Path ([string]$source) $class.filename))
                if ($absolute.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                    $file = $absolute.Substring($root.Length + 1).Replace('\','/')
                    break
                }
            }
            if (-not $file -or -not $changed.ContainsKey($file)) { continue }
            if (-not $covered.ContainsKey($file)) { $covered[$file] = @{} }
            foreach ($point in $class.SelectNodes('lines/line')) {
                $number = [int]$point.number
                if ($changed[$file].Contains($number)) {
                    $covered[$file][$number] = $covered[$file][$number] -or ([int]$point.hits -gt 0)
                }
            }
        }
    }
    $total = 0
    $hit = 0
    foreach ($file in ($covered.Keys | Sort-Object)) {
        $lines = $covered[$file]
        $count = @($lines.Values | Where-Object { $_ }).Count
        $hit += $count
        $total += $lines.Count
        $missed = @($lines.Keys | Where-Object { -not $lines[$_] } | Sort-Object)
        [pscustomobject]@{ File = $file; Covered = $count; Executable = $lines.Count; Missed = $missed -join ',' }
    }
    if ($total -eq 0) { throw 'No changed executable lines found in the supplied reports.' }
    $percentage = [Math]::Round(100 * $hit / $total, 2)
    Write-Output "Baseline $Baseline : $hit / $total changed executable lines covered ($percentage%)."
    if ($percentage -lt 80) { throw 'Changed-line coverage is below 80%.' }
}
finally { Pop-Location }
