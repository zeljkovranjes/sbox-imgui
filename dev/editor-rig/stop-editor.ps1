# Stops only the scratch editor hosting imguihost.sbproj (other sbox-dev instances are left alone).
$procs = Get-CimInstance Win32_Process -Filter "Name='sbox-dev.exe'" | Where-Object { $_.CommandLine -like "*imguihost.sbproj*" }
if ($procs) {
    foreach ($p in $procs) { taskkill /PID $p.ProcessId /T /F 2>$null | Out-Null; Write-Host "stopped imgui host editor pid $($p.ProcessId)" }
} else { Write-Host "imgui host editor not running" }
Remove-Item (Join-Path $PSScriptRoot ".mcp-url") -Force -ErrorAction SilentlyContinue
