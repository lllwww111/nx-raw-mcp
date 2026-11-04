param(
    [Parameter(Mandatory = $true)]
    [string]$NxRoot,
    [ValidatePattern('^[A-Za-z0-9_-]+\.dll$')]
    [string]$OutputName = 'NxRawMcp.dll'
)

$ErrorActionPreference = 'Stop'
$NxRoot = (Resolve-Path -LiteralPath $NxRoot).Path
$managed = Join-Path $NxRoot 'NXBIN\managed'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$references = @(
    (Join-Path $managed 'NXOpen.dll'),
    (Join-Path $managed 'NXOpen.Utilities.dll')
)
foreach ($required in @($compiler) + $references) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required file not found: $required"
    }
}
$outputDirectory = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$output = Join-Path $outputDirectory $OutputName
& $compiler /nologo /target:library /platform:x64 "/out:$output" /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll "/reference:$($references[0])" "/reference:$($references[1])" (Join-Path $PSScriptRoot 'RawExec.cs')
if ($LASTEXITCODE -ne 0) { throw 'Raw executor build failed.' }
Write-Output "Built $output"
