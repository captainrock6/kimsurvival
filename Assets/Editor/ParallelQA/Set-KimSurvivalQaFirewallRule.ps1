[CmdletBinding()]
param(
    [string[]]$ProjectRoot,

    [string]$EvidencePath
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
    $ruleName = "KimSurvivalQaBlock-$token"
    $displayName = "Kim Survival QA Player - Block Inbound $token"
    $legacyDisplayName = "Kim Survival QA Player - Block Inbound [$token]"
    $existing = @(Get-NetFirewallRule -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -eq $ruleName -or $_.DisplayName -eq $displayName -or $_.DisplayName -eq $legacyDisplayName
    })
    if ($existing.Count -gt 0) {
        $existing | Remove-NetFirewallRule
    }
    New-NetFirewallRule -Name $ruleName -DisplayName $displayName -Description 'Blocks unsolicited inbound traffic for the local Kim Survival Unity QA player so Windows does not repeatedly prompt for each automated smoke run.' -Direction Inbound -Program $program -Action Block -Profile Any -Enabled True | Out-Null
    $verifiedRule = Get-NetFirewallRule -Name $ruleName -ErrorAction Stop
    $verifiedFilter = $verifiedRule | Get-NetFirewallApplicationFilter
    $verified = [string]$verifiedRule.Enabled -eq 'True' -and
        [string]$verifiedRule.Direction -eq 'Inbound' -and
        [string]$verifiedRule.Action -eq 'Block' -and
        $verifiedFilter.Program.Equals($program, [StringComparison]::OrdinalIgnoreCase)
    [pscustomobject][ordered]@{
        displayName = $displayName
        ruleName = $ruleName
        program = $program
        direction = 'Inbound'
        action = 'Block'
        profiles = 'Any'
        observedEnabled = [string]$verifiedRule.Enabled
        observedDirection = [string]$verifiedRule.Direction
        observedAction = [string]$verifiedRule.Action
        observedProgram = [string]$verifiedFilter.Program
        status = if ($verified) { 'PASS' } else { 'FAIL' }
    }
}

if (-not [string]::IsNullOrWhiteSpace($EvidencePath)) {
    $resolvedEvidence = [IO.Path]::GetFullPath($EvidencePath)
    $evidenceParent = Split-Path -Parent $resolvedEvidence
    New-Item -ItemType Directory -Path $evidenceParent -Force | Out-Null
    $payload = [ordered]@{
        schemaVersion = 1
        observedUtc = [DateTime]::UtcNow.ToString('O')
        overall = if (@($results | Where-Object status -ne 'PASS').Count -eq 0) { 'PASS' } else { 'FAIL' }
        rules = @($results)
    }
    [IO.File]::WriteAllText($resolvedEvidence, ($payload | ConvertTo-Json -Depth 6) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
}

$results | Format-Table -AutoSize
if (@($results | Where-Object status -ne 'PASS').Count -gt 0) {
    throw 'One or more Windows Firewall rules failed verification.'
}
