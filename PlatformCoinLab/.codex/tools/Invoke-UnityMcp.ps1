param([string]$Method = 'resources/read', [string]$Parameters = '{"uri":"mcpforunity://instances"}')
$ErrorActionPreference = 'Stop'
$headers = @{ Accept = 'application/json, text/event-stream' }
$endpoint = 'http://127.0.0.1:8081/mcp'
$init = @{ jsonrpc = '2.0'; id = 1; method = 'initialize'; params = @{ protocolVersion = '2024-11-05'; capabilities = @{}; clientInfo = @{ name = 'CodexShield'; version = '1.0' } } }
$response = Invoke-WebRequest -Uri $endpoint -Method Post -Headers $headers -ContentType application/json -Body ($init | ConvertTo-Json -Depth 12) -UseBasicParsing
$headers['Mcp-Session-Id'] = $response.Headers['mcp-session-id']
$notification = @{ jsonrpc = '2.0'; method = 'notifications/initialized' }
$null = Invoke-WebRequest -Uri $endpoint -Method Post -Headers $headers -ContentType application/json -Body ($notification | ConvertTo-Json) -UseBasicParsing
$request = @{ jsonrpc = '2.0'; id = 2; method = $Method; params = ($Parameters | ConvertFrom-Json) }
$result = Invoke-WebRequest -Uri $endpoint -Method Post -Headers $headers -ContentType application/json -Body ($request | ConvertTo-Json -Depth 32) -UseBasicParsing -TimeoutSec 90
$result.Content -split "`n" | Where-Object { $_.StartsWith('data: ') } | ForEach-Object { $_.Substring(6) }
