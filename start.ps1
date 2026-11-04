param(
    [string]$NxRoot = $env:UGII_BASE_DIR,
    [ValidateRange(20700, 20799)]
    [int]$Port = 20790
)

$ErrorActionPreference = 'Stop'
if (-not $NxRoot) {
    throw 'Pass -NxRoot with your NX installation directory, or set UGII_BASE_DIR.'
}
$NxRoot = (Resolve-Path -LiteralPath $NxRoot).Path
$launcher = Join-Path $NxRoot 'NXBIN\run_dotnet_nxopen.exe'
$executable = Join-Path $PSScriptRoot 'build\RawExec.exe'
foreach ($required in @($launcher, $executable)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required file not found: $required. Build the executor first."
    }
}
$previousNxRoot = $env:UGII_BASE_DIR
try {
    $env:UGII_BASE_DIR = $NxRoot
    Write-Output "Starting a separate NX session at http://127.0.0.1:$Port/"
    Write-Output 'Keep this terminal open while using the MCP bridge.'
    & $launcher $executable $Port
    if ($LASTEXITCODE -ne 0) { throw "NX launcher exited with code $LASTEXITCODE." }
}
finally {
    $env:UGII_BASE_DIR = $previousNxRoot
}
