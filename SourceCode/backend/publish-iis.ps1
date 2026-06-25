<#
.SYNOPSIS
  把 MassageSaas.Api 打包成 IIS 部署包，统一输出到仓库根目录的「发布包」目录。

.DESCRIPTION
  默认只打「增量更新包」：仅含本项目自己改动会涉及的程序集与配置
  （MassageSaas.*.dll/pdb + deps/runtimeconfig + web.config），
  不含 .NET 运行时与第三方依赖——这些日常代码更新不变，服务器已有。
  部署时解压覆盖 IIS 站点根目录即可，回收应用池生效。

  仅在「首次部署 / 升级 .NET 运行时 / 增删第三方 NuGet 依赖」时用 -Full
  打完整自包含包（win-x64）。

  产物与建表 SQL 统一放在：<仓库根>/发布包/
    - MassageSaas.Api-增量更新包-yyyyMMdd-HHmm.zip   （默认）
    - MassageSaas.Api-完整部署包-yyyyMMdd-HHmm.zip   （-Full）
    - migration-yyyyMMdd-HHmm.sql                    （-WithSql，幂等）

.EXAMPLE
  powershell -File backend/publish-iis.ps1                 # 增量更新包
  powershell -File backend/publish-iis.ps1 -WithSql        # 增量包 + 迁移SQL
  powershell -File backend/publish-iis.ps1 -Full           # 完整自包含包
  powershell -File backend/publish-iis.ps1 -NoZip          # 只发布到文件夹，不压缩
#>
param(
  [switch]$Full,      # 打完整自包含包（首次部署 / 升级运行时 / 第三方依赖变动时用）
  [switch]$WithSql,   # 同时生成幂等迁移 SQL 到 发布包
  [string]$SqlFrom = '', # 生产已部署的最后一个迁移名；只生成它之后的增量(只含本次需更新的表)。留空=全量幂等
  [switch]$NoZip
)

$ErrorActionPreference = 'Stop'

$backendDir  = $PSScriptRoot
$proj        = Join-Path $backendDir 'src/MassageSaas.Api/MassageSaas.Api.csproj'
$outDir      = Join-Path $backendDir 'src/MassageSaas.Api/bin/Release/net8.0/publish-iis'
$repoRoot    = Split-Path $backendDir -Parent
$releaseDir  = Join-Path $repoRoot '发布包'
$stamp       = Get-Date -Format 'yyyyMMdd-HHmm'

if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }

Write-Host '==> 清理旧的发布目录' -ForegroundColor Cyan
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

Write-Host '==> dotnet publish（Release / win-x64 / 自包含 / Production）' -ForegroundColor Cyan
dotnet publish $proj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:EnvironmentName=Production `
  -o $outDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败（exit $LASTEXITCODE）" }

# 发布目录里不要带开发用的本地配置（含本机连接串/密钥）
$devCfg = Join-Path $outDir 'appsettings.Development.json'
if (Test-Path $devCfg) { Remove-Item $devCfg -Force }
# 生产连接串/密钥由服务器上的 appsettings.Production.json 维护，更新包不要覆盖它
$prodCfg = Join-Path $outDir 'appsettings.Production.json'
if (Test-Path $prodCfg) { Remove-Item $prodCfg -Force }

Write-Host "==> 发布完成：$outDir" -ForegroundColor Green

# 可选：生成幂等迁移 SQL 到 发布包（只补跑缺失迁移，重复执行安全）
if ($WithSql) {
  $apiDir = Join-Path $backendDir 'src/MassageSaas.Api'
  $sqlOut = Join-Path $releaseDir "migration-$stamp.sql"
  Write-Host '==> 生成幂等迁移 SQL' -ForegroundColor Cyan
  Push-Location $apiDir
  try {
    dotnet tool restore | Out-Null
    # 指定 -SqlFrom 时只生成该迁移之后的增量（只含本次需更新的表）；否则全量幂等。
    # 两种都带 --idempotent（IF NOT EXISTS 守卫），可重复执行。
    if ($SqlFrom) {
      Write-Host "    （增量：仅 $SqlFrom 之后的迁移）" -ForegroundColor DarkGray
      dotnet ef migrations script $SqlFrom --idempotent `
        --project ../MassageSaas.Infrastructure --startup-project . -o $sqlOut
    }
    else {
      dotnet ef migrations script --idempotent `
        --project ../MassageSaas.Infrastructure --startup-project . -o $sqlOut
    }
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef 生成 SQL 失败（exit $LASTEXITCODE）" }
    Write-Host "==> 迁移 SQL：$sqlOut" -ForegroundColor Green
  } finally { Pop-Location }
}

if ($NoZip) { return }

if ($Full) {
  $zip = Join-Path $releaseDir "MassageSaas.Api-完整部署包-$stamp.zip"
  if (Test-Path $zip) { Remove-Item $zip -Force }
  Write-Host "==> 压缩完整部署包：$zip" -ForegroundColor Cyan
  Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zip -CompressionLevel Optimal
  Write-Host "==> 打包完成：$zip" -ForegroundColor Green
}
else {
  # 增量更新包：只含本项目改动会涉及的文件，覆盖到 IIS 站点根目录即可
  $incNames = @(
    'MassageSaas.Api.dll', 'MassageSaas.Api.pdb',
    'MassageSaas.Api.deps.json', 'MassageSaas.Api.runtimeconfig.json',
    'MassageSaas.Api.staticwebassets.endpoints.json',
    'MassageSaas.Application.dll', 'MassageSaas.Application.pdb',
    'MassageSaas.Domain.dll', 'MassageSaas.Domain.pdb',
    'MassageSaas.Infrastructure.dll', 'MassageSaas.Infrastructure.pdb',
    'MassageSaas.Shared.dll', 'MassageSaas.Shared.pdb',
    'web.config'
  )
  $incPaths = $incNames |
    ForEach-Object { Join-Path $outDir $_ } |
    Where-Object { Test-Path $_ }

  $zip = Join-Path $releaseDir "MassageSaas.Api-增量更新包-$stamp.zip"
  if (Test-Path $zip) { Remove-Item $zip -Force }
  Write-Host "==> 压缩增量更新包（$($incPaths.Count) 个文件）：$zip" -ForegroundColor Cyan
  Compress-Archive -Path $incPaths -DestinationPath $zip -CompressionLevel Optimal
  Write-Host "==> 打包完成：$zip" -ForegroundColor Green
}

Write-Host ''
Write-Host '部署提示：' -ForegroundColor Yellow
if ($Full) {
  Write-Host '  · 完整包：首次部署用。先装「.NET 8 Hosting Bundle」，新建 IIS 站点指向解压目录，'
  Write-Host '    配 http(80)/https(443) 绑定，再到服务器改 appsettings.Production.json（连接串/Jwt:SecretKey）。'
}
else {
  Write-Host '  · 增量包：解压覆盖 IIS 站点根目录的同名文件，回收应用池（或 iisreset）即可。'
  Write-Host '    注意：不会动服务器上的 appsettings.Production.json；若本次新增了 NuGet 依赖请改用 -Full。'
}
Write-Host '  · 有数据库结构变更时：在生产库执行 发布包/ 下的 migration-*.sql（幂等，可重复执行）。'
Write-Host '  · 详见 docs/IIS部署说明.md'

