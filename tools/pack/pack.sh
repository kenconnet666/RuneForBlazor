#!/usr/bin/env bash
# 打全部对外发布的包并检查内容：CI（每次推送，留作产物）与发版（release.yml，推送到 nuget.org）共用。
#
#   tools/pack/pack.sh <输出目录> [版本]     不写版本时用 Directory.Build.props 的 VersionPrefix-VersionSuffix
#
# 先要有 Release 构建（这里 --no-build）。包 ID 是 RuneForBlazor.*（见 Directory.Build.targets）。
set -euo pipefail

out=$1
version=${2:-}
projects=(Rune Rune.UI Rune.Icons Rune.UI.Dates Rune.Charts Rune.Charts.Maps Rune.UI.Area)

for project in "${projects[@]}"; do
  dotnet pack "src/$project/$project.csproj" --configuration Release --no-build --output "$out" ${version:+-p:Version="$version"}
done

package() { ls "$out"/"$1".[0-9]*.nupkg; }
test "$(ls "$out"/*.nupkg | wc -l)" -eq "${#projects[@]}"
# 生成器在核心包里（装 RuneForBlazor.UI 也能用 [State]：库之间的依赖不排除分析器）
unzip -l "$(package RuneForBlazor)" | grep -q "analyzers/dotnet/cs/Rune.Generators.dll"
unzip -p "$(package RuneForBlazor.UI)" RuneForBlazor.UI.nuspec | grep -q '<dependency id="RuneForBlazor" [^>]*include="All"'
# 浮层等 JS 初始化器在组件库包里；Lucide 的许可随两个带图标数据的包
unzip -l "$(package RuneForBlazor.UI)" | grep -q "Rune.UI.*lib.module.js"
unzip -l "$(package RuneForBlazor.UI)" | grep -q "LICENSE-lucide.txt"
unzip -l "$(package RuneForBlazor.Icons)" | grep -q "LICENSE-lucide.txt"
ls -l "$out"
