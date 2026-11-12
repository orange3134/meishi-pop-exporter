$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $repoRoot 'distribution.json') -Raw -Encoding utf8 | ConvertFrom-Json
$metadata = Get-Content (Join-Path $repoRoot 'dist/vpm-release.json') -Raw -Encoding utf8 | ConvertFrom-Json
$destination = Join-Path ([IO.Path]::GetTempPath()) ('meishi-booth-' + [guid]::NewGuid())
try {
    Expand-Archive -LiteralPath (Join-Path $repoRoot ('dist/' + $metadata.boothZip)) -DestinationPath $destination
    $shortcutPath = Join-Path $destination '説明書.url'
    if (-not (Test-Path -LiteralPath $shortcutPath)) { throw 'Japanese shortcut filename was not preserved' }
    # Use Explorer's Unicode-capable property handler for the Japanese filename.
    $shell = New-Object -ComObject Shell.Application
    $folder = $shell.NameSpace($destination)
    $item = $folder.ParseName('説明書.url')
    $targetUrl = $item.ExtendedProperty('System.Link.TargetUrl')
    if ($targetUrl -cne $config.booth.manualUrl) {
        throw "Windows Explorer parsed an unexpected shortcut URL: '$targetUrl'"
    }
    $actual = Get-FileHash -LiteralPath (Join-Path $destination $metadata.unitypackage) -Algorithm SHA256
    $expected = Get-FileHash -LiteralPath (Join-Path $repoRoot ('dist/' + $metadata.unitypackage)) -Algorithm SHA256
    if ($actual.Hash -ne $expected.Hash) { throw 'Unitypackage changed in the Booth ZIP' }
    Write-Output 'Windows ZIP extraction, Japanese filename, Internet Shortcut URL, and Unitypackage hash verified.'
} finally {
    if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
}
