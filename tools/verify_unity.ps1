param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [switch]$Configure,
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$unityPath = (Resolve-Path -LiteralPath $UnityEditor).Path
$evidenceRoot = Join-Path $projectRoot 'Logs/S001'
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null

function Invoke-Editor([string]$Name, [string[]]$Extra) {
    $logPath = Join-Path $evidenceRoot "$Name.log"
    $arguments = @('-batchmode', '-projectPath', ('"' + $projectRoot + '"'), '-logFile', ('"' + $logPath + '"')) + $Extra
    $editorProcess = Start-Process -FilePath $unityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $editorProcess.WaitForExit()
    if ($editorProcess.ExitCode -ne 0) { throw "$Name failed ($($editorProcess.ExitCode)); see $logPath" }
    Write-Output "$Name succeeded: $logPath"
}

if ($Configure) { Invoke-Editor 'configure' @('-nographics', '-quit', '-executeMethod', 'DodgerMover.Editor.ImpactProject.Configure') }
foreach ($mode in @('EditMode', 'PlayMode')) {
    $resultPath = Join-Path $evidenceRoot "$mode.xml"
    # -quit must not accompany -runTests: the test runner owns editor shutdown.
    Invoke-Editor $mode @('-runTests', '-testPlatform', $mode, '-testResults', ('"' + $resultPath + '"'))
    if (-not (Test-Path -LiteralPath $resultPath)) { throw "Missing $mode test results" }
    [xml]$result = Get-Content -LiteralPath $resultPath
    if ($result.'test-run'.result -ne 'Passed' -or [int]$result.'test-run'.total -eq 0) { throw "$mode did not pass: $resultPath" }
    Write-Output "$mode passed $($result.'test-run'.passed)/$($result.'test-run'.total) tests"
}
if ($Build) { Invoke-Editor 'build' @('-quit', '-executeMethod', 'DodgerMover.Editor.ImpactProject.BuildWindows') }
