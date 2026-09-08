param(
    [ValidateRange(1,16)][int]$Enemies = 4,
    [ValidateRange(15,600)][int]$Duration = 35,
    [ValidateSet('Default','DirectX11','DirectX12')][string]$GraphicsApi = 'Default',
    [ValidateRange(800,7680)][int]$Width = 1600,
    [ValidateRange(450,4320)][int]$Height = 900,
    [ValidateSet('p51','zero','bf109','p38')][string]$Player = 'p51',
    [ValidateSet('p51','zero','bf109','p38')][string]$Enemy = 'zero'
)
$ErrorActionPreference = 'Stop'
$Player = $Player.ToLowerInvariant()
$Enemy = $Enemy.ToLowerInvariant()
$projectPath = Split-Path -Parent $PSScriptRoot
$buildPath = Join-Path $projectPath 'Builds/Windows'
$executablePath = Join-Path $buildPath 'PacificFighterSweep.exe'
if (!(Test-Path -LiteralPath $executablePath)) { throw 'Build the Windows player with Tools/Build.ps1 first.' }
$reportFolder = "SmokeTest$Enemies-$Player-$Enemy"
$arguments = @('--smoke-test', "--enemies=$Enemies", "--smoke-duration=$Duration", "--player=$Player", "--enemy=$Enemy", "--smoke-label=$Player-$Enemy", '-screen-width', "$Width", '-screen-height', "$Height", '-logFile', "$reportFolder-player.log")
if ($GraphicsApi -eq 'DirectX11') { $arguments += '-force-d3d11' }
if ($GraphicsApi -eq 'DirectX12') { $arguments += '-force-d3d12' }
$startedAt = [DateTime]::UtcNow
# This is an interactive game window: Windows skips rendered screenshot capture if hidden.
$gameProcess = Start-Process -FilePath $executablePath -ArgumentList $arguments -WorkingDirectory $buildPath -WindowStyle Normal -PassThru
if (!$gameProcess.WaitForExit(($Duration + 90) * 1000)) {
    if (!$gameProcess.HasExited) { Stop-Process -Id $gameProcess.Id }
    throw "Smoke test timed out. See Builds/Windows/$reportFolder-player.log."
}
$resultsPath = Join-Path $buildPath "$reportFolder/results.txt"
if ($gameProcess.ExitCode -ne 0) { throw "Smoke test exited $($gameProcess.ExitCode). Inspect $resultsPath and the player log." }
if (!(Test-Path -LiteralPath $resultsPath) -or (Get-Item -LiteralPath $resultsPath).LastWriteTimeUtc -lt $startedAt.AddSeconds(-1)) { throw 'The player did not produce a fresh smoke report.' }
$report = Get-Content -LiteralPath $resultsPath -Raw
if (!$report.Contains('SMOKE PASSED')) { throw "Smoke test did not pass: $resultsPath" }
Write-Output $report
