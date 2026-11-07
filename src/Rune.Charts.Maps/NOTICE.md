# 地理数据的来源

`Data/` 里的两份 TopoJSON 与 vue-tsx-pro 的 `packages/ui/src/maps` 相同（逐字节复制），MapChart 的示例与它对照。

| 文件 | 内容 | 来源 | 许可 |
| ---- | ---- | ---- | ---- |
| `china-provinces.json` | 中国省份（要素组 `provinces`，id 如 `CN-JS`，属性 `name` 是英文名） | [Unovis](https://github.com/f5/unovis) 自带的地图（F5, Inc.），只保留了 provinces 与名称属性 | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) |
| `world-countries.json` | 世界各国（要素组 `countries`，id 是 ISO 3166-1 两位代码，不含南极洲） | 国界来自 [Natural Earth](https://www.naturalearthdata.com)（公有领域），经 Unovis 整理成 TopoJSON，只保留了 countries 与名称属性 | Natural Earth：公有领域；整理：Apache License 2.0 |

这份数据里湖北是 `CN-HU`、河北是 `CN-HB`、河南是 `CN-HE`、海南是 `CN-HA`、陕西是 `CN-SA`、内蒙古是 `CN-NM`。
