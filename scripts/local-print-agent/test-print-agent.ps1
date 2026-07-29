[CmdletBinding()]
param([string]$BaseUrl = "http://localhost:9101")
$ErrorActionPreference = "Stop"
$live = Invoke-RestMethod "$BaseUrl/health/live" -TimeoutSec 5
$ready = Invoke-RestMethod "$BaseUrl/health/ready" -TimeoutSec 5
[pscustomobject]@{ Live = $live.status; Ready = $ready.ready; AgentVersion = $ready.agentVersion }
