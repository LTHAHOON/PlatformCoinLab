#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
$ruleName = 'CoinGame.ContentServer.Http5250'
$serverPath = Join-Path $PSScriptRoot 'bin/Debug/net10.0/ContentServer.exe'
if (-not (Test-Path -LiteralPath $serverPath))
{
    throw "Build ContentServer before enabling its firewall rule: $serverPath"
}
$rule = Get-NetFirewallRule -Name $ruleName -ErrorAction SilentlyContinue
if ($null -eq $rule)
{
    New-NetFirewallRule -Name $ruleName -DisplayName 'CoinGame ContentServer HTTP 5250' -Direction Inbound -Action Allow -Enabled True -Profile Any -Protocol TCP -LocalPort 5250 -Program $serverPath | Out-Null
}
else
{
    $rule | Set-NetFirewallRule -Enabled True -Action Allow -Profile Any -Program $serverPath
    $rule | Get-NetFirewallPortFilter | Set-NetFirewallPortFilter -Protocol TCP -LocalPort 5250
}
Get-NetFirewallRule -Name $ruleName | Format-List Name,Enabled,Direction,Action
