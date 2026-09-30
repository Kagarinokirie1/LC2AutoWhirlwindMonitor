<#
.SYNOPSIS
    编译自动旋风斩插件并部署到游戏的 BepInEx\plugins 目录。

.DESCRIPTION
    默认假设本仓库位于游戏根目录下（与 BepInEx 同级）。
    若仓库在别处，使用 -GameDir 指定游戏根目录。

    构建依赖游戏目录下由 BepInEx 生成的 BepInEx\interop 程序集，
    因此需要先至少启动过一次带 BepInEx 的游戏。

.EXAMPLE
    .\scripts\build.ps1

.EXAMPLE
    .\scripts\build.ps1 -GameDir "D:\Steam\steamapps\common\Lost Castle 2"
#>
[CmdletBinding()]
param(
    [string]$GameDir,
    [switch]$NoDeploy
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\AutoWhirlwindMonitor\LC2AutoWhirlwindMonitor.csproj'

if (-not (Test-Path -LiteralPath $project)) {
    throw "未找到工程文件：$project"
}

if ($GameDir) {
    $buildArgs = @($project, '-c', 'Release', "-p:GameDir=$GameDir")
    $gameRoot = $GameDir
} else {
    $buildArgs = @($project, '-c', 'Release')
    $gameRoot = $repoRoot
}

Write-Host "开始编译：$project" -ForegroundColor Cyan
& dotnet build @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build 失败，退出码 $LASTEXITCODE"
}

$built = Join-Path $repoRoot 'src\AutoWhirlwindMonitor\bin\Release\LC2AutoWhirlwindMonitor.dll'
if (-not (Test-Path -LiteralPath $built)) {
    throw "未找到编译产物：$built"
}

$hash = (Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash
Write-Host "编译产物 SHA256：$hash" -ForegroundColor Cyan

if ($NoDeploy) {
    Write-Host "已跳过部署。" -ForegroundColor Yellow
    return
}

$pluginsDir = Join-Path $gameRoot 'BepInEx\plugins'
if (-not (Test-Path -LiteralPath $pluginsDir)) {
    New-Item -ItemType Directory -Force -Path $pluginsDir | Out-Null
}

$target = Join-Path $pluginsDir 'LC2AutoWhirlwindMonitor.dll'
Copy-Item -LiteralPath $built -Destination $target -Force
Write-Host "已部署到：$target" -ForegroundColor Green
