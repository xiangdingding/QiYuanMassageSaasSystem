<#
.SYNOPSIS
  构建并打包前端：租户端 shop-admin 与平台端 platform-admin（生产发布包）。

.DESCRIPTION
  分别 npm run build 两个 app，把 deploy/web.config（SPA 回退 + /api 反代）拷进各自 dist，
  再压缩成 SourceCode 根目录下的 zip：
    shop-admin-发布包-yyyyMMdd-HHmm.zip
    platform-admin-发布包-yyyyMMdd-HHmm.zip

.EXAMPLE
  pwsh ./frontend/publish-web.ps1
  pwsh ./frontend/publish-web.ps1 -Apps shop-admin     # 只发布其中一个
  pwsh ./frontend/publish-web.ps1 -NoZip               # 只构建+放 web.config，不压缩
#>
param(
  [ValidateSet('shop-admin','platform-admin','website')]
  [string[]]$Apps = @('shop-admin','platform-admin','website'),
  [switch]$NoZip
)

$ErrorActionPreference = 'Stop'

$frontendDir = $PSScriptRoot
$repoRoot    = Split-Path $frontendDir -Parent
$webConfig   = Join-Path $frontendDir 'deploy/web.config'
$stamp       = Get-Date -Format 'yyyyMMdd-HHmm'
# 所有发布包统一输出到 SourceCode/发布包/
$outZipDir   = Join-Path $repoRoot '发布包'
if (-not (Test-Path $outZipDir)) { New-Item -ItemType Directory -Path $outZipDir | Out-Null }

Write-Host '==> 确保依赖已安装（npm install）' -ForegroundColor Cyan
Push-Location $frontendDir
try {
  if (-not (Test-Path (Join-Path $frontendDir 'node_modules'))) {
    npm install
    if ($LASTEXITCODE -ne 0) { throw "npm install 失败（exit $LASTEXITCODE）" }
  }

  foreach ($app in $Apps) {
    $task = switch ($app) {
      'shop-admin'     { 'build:shop' }
      'platform-admin' { 'build:platform' }
      'website'        { 'build:website' }
    }
    Write-Host "==> 构建 $app（npm run $task）" -ForegroundColor Cyan
    npm run $task
    if ($LASTEXITCODE -ne 0) { throw "$app 构建失败（exit $LASTEXITCODE）" }

    $dist = Join-Path $frontendDir "apps/$app/dist"
    if (-not (Test-Path $dist)) { throw "$app 构建产物缺失：$dist" }

    Copy-Item $webConfig (Join-Path $dist 'web.config') -Force
    Write-Host "==> 已放入 web.config：$dist" -ForegroundColor Green

    if (-not $NoZip) {
      # 官网包名用更直观的「massage-官网」，避免与母公司静态站 website/ 混淆
      $zipName = if ($app -eq 'website') { 'massage-官网' } else { $app }
      $zip = Join-Path $outZipDir "$zipName-发布包-$stamp.zip"
      if (Test-Path $zip) { Remove-Item $zip -Force }
      Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip -CompressionLevel Optimal
      Write-Host "==> 打包完成：$zip" -ForegroundColor Green
    }
  }
}
finally {
  Pop-Location
}

Write-Host ''
Write-Host '部署提示：' -ForegroundColor Yellow
Write-Host '  1) 服务器装 URL Rewrite + Application Request Routing(ARR)，并在 IIS 服务器层开启 Enable proxy'
Write-Host '  2) 每个 app 建一个 IIS 站点，物理路径指向解压后的目录（含 web.config、index.html、assets/）'
Write-Host '  3) 改各站点 web.config 里的 http://后端API地址:端口 为真实 API 地址'
Write-Host '  4) shop-admin 站点用 http 绑定、platform-admin 按需；详见 docs/前端部署说明.md'
