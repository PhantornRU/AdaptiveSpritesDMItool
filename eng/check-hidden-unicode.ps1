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

$excludedSegments = @(
    '.git',
    'bin',
    'obj'
)

function Test-IsExcludedPath {
    param([string] $Path)

    $relativePath = Get-RelativePath -Path $Path
    $segments = $relativePath -split '[\\/]+'

    foreach ($segment in $segments) {
        if ($excludedSegments -contains $segment) {
            return $true
        }
    }

    return $false
}

function Get-RelativePath {
    param([string] $Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath
    }

    return $fullPath.Substring($root.Length).TrimStart([char[]]@('\', '/'))
}

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

Get-ChildItem -Path $root -Recurse -File |
    Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() } |
    Where-Object { -not (Test-IsExcludedPath $_.FullName) } |
    ForEach-Object {
        $file = $_
        $relativePath = Get-RelativePath -Path $file.FullName
        $text = [System.IO.File]::ReadAllText($file.FullName, $utf8Strict)

        for ($i = 0; $i -lt $text.Length; $i++) {
            if ([System.Char]::IsLowSurrogate($text[$i])) {
                continue
            }

            $category = [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($text, $i)

            if ([System.Char]::IsHighSurrogate($text[$i])) {
                if ($i + 1 -ge $text.Length -or -not [System.Char]::IsLowSurrogate($text[$i + 1])) {
                    throw "Invalid UTF-16 surrogate pair in '$relativePath' at index $i."
                }

                if ($category -ne [System.Globalization.UnicodeCategory]::Format) {
                    $i++
                    continue
                }
            }

            $codePoint = [System.Char]::ConvertToUtf32($text, $i)

            if ([System.Char]::IsHighSurrogate($text[$i])) {
                $i++
            }

            if ($category -ne [System.Globalization.UnicodeCategory]::Format) {
                continue
            }

            if ($codePoint -eq 0xFEFF -and $i -eq 0) {
                continue
            }

            $location = Get-LineColumn -Text $text -Index $i
            $findings.Add([pscustomobject]@{
                Path = $relativePath
                Line = $location.Line
                Column = $location.Column
                CodePoint = ('U+{0:X4}' -f $codePoint)
                Name = [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($text, $i)
            }) | Out-Null
        }
    }

if ($findings.Count -gt 0) {
    Write-Host 'Hidden Unicode control characters found:'
    $findings | Format-Table Path, Line, Column, CodePoint, Name -AutoSize
    exit 1
}

Write-Host 'No hidden Unicode control characters found.'
