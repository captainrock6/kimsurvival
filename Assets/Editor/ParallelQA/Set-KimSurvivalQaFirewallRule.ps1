[CmdletBinding()]
param(
    [string[]]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

if ($null -eq $ProjectRoot -or $ProjectRoot.Count -eq 0) {
    $ProjectRoot = @((Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path)
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Administrator privileges are required to create the Windows Firewall rule.'
}

function Get-PathToken([string]$Value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Value.ToLowerInvariant())
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').Substring(0, 12)
    } finally {
        $sha.Dispose()
    }
}

$results = foreach ($root in $ProjectRoot) {
    $resolvedRoot = [IO.Path]::GetFullPath($root)
    $program = [IO.Path]::GetFullPath((Join-Path $resolvedRoot 'work\ParallelQA\StableWindowsBuild\KimSurvivalIsland.exe'))
    $token = Get-PathToken $program
    $displayName = "Kim Survival QA Player - Block Inbound [$token]"
    $existing = @(Get-NetFirewallRule -DisplayName $displayName -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 0) {
        $existing | Remove-NetFirewallRule
    }
    New-NetFirewallRule -DisplayName $displayName -Description 'Blocks unsolicited inbound traffic for the local Kim Survival Unity QA player so Windows does not repeatedly prompt for each automated smoke run.' -Direction Inbound -Program $program -Action Block -Profile Any -Enabled True | Out-Null
    [pscustomobject][ordered]@{
        displayName = $displayName
        program = $program
        direction = 'Inbound'
        action = 'Block'
        profiles = 'Any'
        status = 'CREATED'
    }
}

$results | Format-Table -AutoSize
