
@("bin","obj","node_modules") | %{
    Write-Host $_
    gci -Path $PSScriptRoot -Filter $_   -Directory -Recurse -Verbose | ri -Force -Recurse
}
