# Starts a scratch s&box editor session hosting this library (plus the sbox-mcp server) so compile
# errors, whitelist violations and in-engine behaviour can be checked autonomously over MCP (see mcp.py).
# The MCP server is bound to its own port (default 8433) so it never collides with another editor.
#
#   powershell -ExecutionPolicy Bypass -File dev\editor-rig\start-editor.ps1 [-Clean] [-Port 8433]
#
# Exit codes: 0 = editor up and MCP answering, 1 = library failed to compile, 2 = failed to start.
[CmdletBinding()]
param(
    [string]$SboxRoot = "",
    [string]$McpRoot = "",
    [string]$LibraryRoot = "",
    [int]$Port = 8433,
    [int]$TimeoutSec = 420,
    [switch]$Clean
)
$ErrorActionPreference = "Stop"

if (-not $SboxRoot) {
    foreach ($c in @($env:IMGUI_SBOX_ROOT, "C:\Program Files (x86)\Steam\steamapps\common\sbox", "D:\SteamLibrary\steamapps\common\sbox")) {
        if ($c -and (Test-Path (Join-Path $c "sbox-dev.exe"))) { $SboxRoot = $c; break }
    }
}
if (-not $SboxRoot) { Write-Host "RESULT: sbox-dev.exe not found"; exit 2 }

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not $LibraryRoot) { $LibraryRoot = $repoRoot }
if (-not $McpRoot) {
    foreach ($c in @($env:IMGUI_MCP_ROOT, (Join-Path (Split-Path $repoRoot -Parent) "sbox-mcp"), "P:\02-projects\Zeljko Vranjes\sbox-mcp")) {
        if ($c -and (Test-Path (Join-Path $c "sbox-mcp.sbproj"))) { $McpRoot = $c; break }
    }
}
if (-not $McpRoot) { Write-Host "RESULT: sbox-mcp library not found (set IMGUI_MCP_ROOT)"; exit 2 }

$scratch  = Join-Path $env:TEMP "imgui-editor-host"   # outside the repo: avoids junction cycles
$sbproj   = Join-Path $scratch "imguihost.sbproj"
$template = Join-Path $SboxRoot "templates\game.minimal"
$sboxExe  = Join-Path $SboxRoot "sbox-dev.exe"

$links = @{
    "Libraries\duccsoft.imgui"  = $LibraryRoot
    "Libraries\local.chomnr_mcp" = $McpRoot
    "Code\ImGuiTests"            = (Join-Path $PSScriptRoot "host-code")
}

if ($Clean -and (Test-Path $scratch)) {
    foreach ($rel in $links.Keys) {
        $p = Join-Path $scratch $rel
        if (Test-Path $p) { [System.IO.Directory]::Delete($p) }
    }
    Remove-Item -Recurse -Force $scratch
}

if (-not (Test-Path $sbproj)) {
    New-Item -ItemType Directory -Force $scratch | Out-Null
    foreach ($d in "Assets", "Code", "Editor") {
        if (Test-Path (Join-Path $template $d)) { Copy-Item (Join-Path $template $d) (Join-Path $scratch $d) -Recurse -Force }
    }
    $proj = Get-Content (Join-Path $template "`$ident.sbproj") -Raw
    $proj = $proj -replace '"Title":\s*"[^"]*"', '"Title": "ImGui Host"'
    $proj = $proj -replace '"Ident":\s*"[^"]*"', '"Ident": "imguihost"'
    $proj = $proj -replace '"StartupScene":\s*"[^"]*"', '"StartupScene": "scenes/imgui_test.scene"'
    [System.IO.File]::WriteAllText($sbproj, $proj)
}

# Test scene: camera + ImGui test harness component
$sceneSrc = Join-Path $PSScriptRoot "imgui_test.scene"
$sceneDst = Join-Path $scratch "Assets\scenes\imgui_test.scene"
Copy-Item $sceneSrc $sceneDst -Force

New-Item -ItemType Directory -Force (Join-Path $scratch "Libraries") | Out-Null
foreach ($rel in $links.Keys) {
    $link = Join-Path $scratch $rel
    if (Test-Path $link) {
        $item = Get-Item $link -Force
        $target = if ($item.Target) { [string]($item.Target | Select-Object -First 1) } else { "" }
        if ($target -and ((Resolve-Path $target -ErrorAction SilentlyContinue).Path -ne (Resolve-Path $links[$rel]).Path)) {
            [System.IO.Directory]::Delete($link)
        }
    }
    if (-not (Test-Path $link)) {
        New-Item -ItemType Junction -Path $link -Value $links[$rel] | Out-Null
    }
}

# Launch. sbox-dev may write to a shared log; judge liveness from the MCP port itself.
$logPath = Join-Path $SboxRoot "logs\sbox-dev.log"
$preLen = 0
if (Test-Path $logPath) { $preLen = (Get-Item $logPath).Length }
$env:SBOX_MCP_PORT = "$Port"
try {
    Write-Host "Launching: `"$sboxExe`" -project `"$sbproj`" (SBOX_MCP_PORT=$Port)"
    $proc = Start-Process -FilePath $sboxExe -ArgumentList @("-project", "`"$sbproj`"") -WorkingDirectory $SboxRoot -PassThru
} finally {
    Remove-Item Env:SBOX_MCP_PORT -ErrorAction SilentlyContinue
}

$url = "http://127.0.0.1:$Port/sbox-mcp"
$urlFile = Join-Path $PSScriptRoot ".mcp-url"
Remove-Item $urlFile -Force -ErrorAction SilentlyContinue
$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 3
    try {
        $body = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"imgui-rig","version":"1"}}}'
        $r = Invoke-WebRequest -Uri $url -Method Post -Body $body -ContentType "application/json" -Headers @{ Accept = "application/json, text/event-stream" } -TimeoutSec 5 -UseBasicParsing
        if ($r.StatusCode -eq 200) {
            Set-Content -Path $urlFile -Value $url -Encoding ascii
            Write-Host "RESULT: editor up (pid $($proc.Id)), MCP at $url"
            exit 0
        }
    } catch { }
    if ($proc.HasExited) { Write-Host "RESULT: editor process exited (code $($proc.ExitCode))"; exit 2 }
}
Write-Host "RESULT: timed out waiting for the MCP server on port $Port"
exit 2
