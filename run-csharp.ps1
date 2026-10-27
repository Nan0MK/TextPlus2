$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

dotnet run --project (Join-Path $root "csharp") -- @args
exit $LASTEXITCODE
