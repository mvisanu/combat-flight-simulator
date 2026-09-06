param(
    [string]$UnityPath = 'C:/Program Files/Unity/Hub/Editor/6000.0.79f1/Editor/Unity.exe',
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity editor not found: $UnityPath" }
New-Item -ItemType Directory -Force -Path (Join-Path $projectPath 'Logs') | Out-Null
$buildMethod = if ($SkipTests) { 'PacificCombat.Editor.ProjectBuilder.Build' } else { 'PacificCombat.Editor.ProjectBuilder.VerifyAndBuild' }
& $UnityPath -batchmode -nographics -quit -projectPath $projectPath -executeMethod $buildMethod -logFile (Join-Path $projectPath 'Logs/build.log')
if ($LASTEXITCODE -ne 0) { throw 'Windows build failed. See Logs/build.log.' }
Write-Output (Join-Path $projectPath 'Builds/Windows/PacificFighterSweep.exe')
