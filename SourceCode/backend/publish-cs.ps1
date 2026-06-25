<#
.SYNOPSIS
  构建并打包 CS 租户端（WPF 桌面端，自包含 win-x64），输出到仓库根的「发布包」目录。

.DESCRIPTION
  dotnet publish 自包含 win-x64（免装 .NET 运行时），压缩为
  发布包/CS租户端-发布包-yyyyMMdd-HHmm.zip 。
  解压即用：双击 MassageSaas.Cs.exe 运行。首次使用需把 appsettings.json 的
  apiBaseUrl 改成真实 API 地址（默认是本地 http://localhost:5139/api）。

  如需做成 setup.exe 安装包，用 Inno Setup 指向 publish 目录打包，
  见 docs/客户端升级-发版打包.md。

.EXAMPLE
  powershell -File backend/publish-cs.ps1
  powershell -File backend/publish-cs.ps1 -NoZip      # 只发布到文件夹，不压缩
#>
param(
  # 写入发布产物 appsettings.json 的后端 API 地址（源码保持 localhost 供开发，仅改发布包）。留空则不改。
  [string]$ApiBaseUrl = 'https://api.massage.qiyuanrj.com/api',
  [switch]$NoZip
)

$ErrorActionPreference = 'Stop'

$backendDir = $PSScriptRoot
$proj       = Join-Path $backendDir 'src/MassageSaas.Cs/MassageSaas.Cs.csproj'
$outDir     = Join-Path $backendDir 'src/MassageSaas.Cs/bin/Release/net8.0-windows/publish-cs'
$repoRoot   = Split-Path $backendDir -Parent
$releaseDir = Join-Path $repoRoot '发布包'
$stamp      = Get-Date -Format 'yyyyMMdd-HHmm'

if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }

Write-Host '==> 清理旧的发布目录' -ForegroundColor Cyan
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

Write-Host '==> dotnet publish（Release / win-x64 / 自包含 / WPF）' -ForegroundColor Cyan
dotnet publish $proj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o $outDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败（exit $LASTEXITCODE）" }

# 不带开发用本地配置（含本机连接串/密钥）
$devCfg = Join-Path $outDir 'appsettings.Development.json'
if (Test-Path $devCfg) { Remove-Item $devCfg -Force }

# 把发布产物里的 apiBaseUrl 改成生产地址（源码 appsettings.json 保持 localhost 供开发）
if ($ApiBaseUrl) {
  $asPath = Join-Path $outDir 'appsettings.json'
  if (Test-Path $asPath) {
    $txt = Get-Content -Raw -Encoding UTF8 $asPath
    $txt = [regex]::Replace($txt, '("apiBaseUrl"\s*:\s*")[^"]*(")', ('${1}' + $ApiBaseUrl + '${2}'))
    Set-Content -Path $asPath -Value $txt -Encoding UTF8 -NoNewline
    Write-Host "==> 已设置发布包 apiBaseUrl：$ApiBaseUrl" -ForegroundColor Green
  }
}

Write-Host "==> 发布完成：$outDir" -ForegroundColor Green

if (-not $NoZip) {
  $zip = Join-Path $releaseDir "CS租户端-发布包-$stamp.zip"
  if (Test-Path $zip) { Remove-Item $zip -Force }
  Write-Host "==> 压缩发布包：$zip" -ForegroundColor Cyan
  Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zip -CompressionLevel Optimal
  Write-Host "==> 打包完成：$zip" -ForegroundColor Green
}

Write-Host ''
Write-Host '部署提示：' -ForegroundColor Yellow
Write-Host '  · 解压后双击 MassageSaas.Cs.exe 即可运行（自包含，免装 .NET 运行时）。'
Write-Host '  · 首次使用改 appsettings.json 的 apiBaseUrl 为真实 API 地址。'
Write-Host '  · 需要 setup.exe 安装包时用 Inno Setup 打包，见 docs/客户端升级-发版打包.md。'

