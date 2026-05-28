[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$r = Invoke-RestMethod -Uri "https://api.github.com/repos/Catatatau/ControllerOverlay/actions/runs"
$r.workflow_runs[0] | Select-Object name, status, conclusion, html_url | ConvertTo-Json
