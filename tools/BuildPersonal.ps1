param(
    [Parameter(Mandatory = $true)]
    [string] $OptimizerFolder
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $OptimizerFolder).Path.TrimEnd('\')
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$output = Join-Path $project 'artifacts\personal'
$zipPath = Join-Path $output 'optimizer-bundle.zip'
$publish = Join-Path $output 'win-x64'
$files = @(Get-ChildItem -LiteralPath $source -Recurse -File | Sort-Object FullName)
if ($files.Count -eq 0) { throw 'A pasta Optimizer está vazia.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath }

Add-Type -AssemblyName System.IO.Compression
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $name = $file.FullName.Substring($source.Length + 1).Replace('\', '/')
        $entry = $zip.CreateEntry($name, [System.IO.Compression.CompressionLevel]::Optimal)
        $inputStream = $file.OpenRead()
        $outputStream = $entry.Open()
        try { $inputStream.CopyTo($outputStream) }
        finally { $outputStream.Dispose(); $inputStream.Dispose() }
    }
}
finally { $zip.Dispose() }

$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if ($archive.Entries.Count -ne $files.Count) { throw 'A contagem de arquivos do pacote está incorreta.' }
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        foreach ($file in $files) {
            $name = $file.FullName.Substring($source.Length + 1).Replace('\', '/')
            $entry = $archive.GetEntry($name)
            if ($null -eq $entry -or $entry.Length -ne $file.Length) {
                throw "Arquivo incompleto no pacote: $name"
            }
            $inputStream = $file.OpenRead()
            $archiveStream = $entry.Open()
            try {
                $originalHash = [Convert]::ToHexString($sha.ComputeHash($inputStream))
                $archiveHash = [Convert]::ToHexString($sha.ComputeHash($archiveStream))
                if ($originalHash -ne $archiveHash) { throw "Conteúdo diferente no pacote: $name" }
            }
            finally { $archiveStream.Dispose(); $inputStream.Dispose() }
        }
    }
    finally { $sha.Dispose() }
}
finally { $archive.Dispose() }

& dotnet publish (Join-Path $project 'src\DkGameOptimizer\DkGameOptimizer.csproj') `
    -c Release -r win-x64 --self-contained true -o $publish `
    '-p:IncludePrivateBundle=true' "-p:OptimizerBundlePath=$zipPath"
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o executável pessoal.' }

$exe = Join-Path $publish 'DKGameOptimizer.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Executável pessoal não foi gerado.' }
$hash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
Write-Host "$($files.Count) arquivos incorporados e verificados."
Write-Host "Executável: $exe"
Write-Host "SHA256: $hash"
