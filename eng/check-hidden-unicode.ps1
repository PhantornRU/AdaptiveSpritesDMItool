$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)

$extensions = @(
    '.cs',
    '.xaml',
    '.csproj',
    '.props',
    '.targets',
    '.json',
    '.md',
    '.sln',
    '.ps1',
    '.yml',
    '.yaml'
)

function Get-LineColumn {
    param(
        [string] $Text,
        [int] $Index
    )

    $line = 1
    $column = 1

    for ($i = 0; $i -lt $Index; $i++) {
        if ($Text[$i] -eq "`n") {
            $line++
            $column = 1
        }
        else {
            $column++
        }
    }

    [pscustomobject]@{
        Line = $line
        Column = $column
    }
}

$findings = New-Object System.Collections.Generic.List[object]
$repositoryFiles = @(& git -C $root ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) {
    throw "Unable to enumerate repository files for the hidden Unicode scan."
}

$repositoryFiles |
    Where-Object { $extensions -contains [System.IO.Path]::GetExtension($_).ToLowerInvariant() } |
    ForEach-Object {
        $relativePath = $_
        $fullPath = Join-Path $root $relativePath
        if (-not [System.IO.File]::Exists($fullPath)) {
            return
        }

        $text = [System.IO.File]::ReadAllText($fullPath, $utf8Strict)

        $formatCharacters = [System.Text.RegularExpressions.Regex]::Matches(
            $text,
            '\p{Cf}',
            [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
        foreach ($match in $formatCharacters) {
            $index = $match.Index
            $codePoint = [System.Char]::ConvertToUtf32($text, $index)
            if ($codePoint -eq 0xFEFF -and $index -eq 0) {
                continue
            }

            $location = Get-LineColumn -Text $text -Index $index
            $findings.Add([pscustomobject]@{
                Path = $relativePath
                Line = $location.Line
                Column = $location.Column
                CodePoint = ('U+{0:X4}' -f $codePoint)
                Name = [System.Globalization.UnicodeCategory]::Format
            }) | Out-Null
        }
    }

if ($findings.Count -gt 0) {
    Write-Host 'Hidden Unicode control characters found:'
    $findings | Format-Table Path, Line, Column, CodePoint, Name -AutoSize
    exit 1
}

Write-Host 'No hidden Unicode control characters found.'
