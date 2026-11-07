namespace Rune.Docs.Components.Charts.MapChart.Demo;

/// <summary>示例共用的省份：Rune.Charts.Maps 自带的中国省份 TopoJSON 的要素 id 与中文名、所属大区（湖北是 CN-HU、河北是 CN-HB、河南是 CN-HE、海南是 CN-HA、陕西是 CN-SA、内蒙古是 CN-NM）。</summary>
static class ChinaDemo
{
    public static readonly (string Id, string Name, string Region)[] Provinces =
    [
        ("CN-BJ", "北京", "华北"), ("CN-TJ", "天津", "华北"), ("CN-HB", "河北", "华北"), ("CN-SX", "山西", "华北"), ("CN-NM", "内蒙古", "华北"),
        ("CN-LN", "辽宁", "东北"), ("CN-JL", "吉林", "东北"), ("CN-HL", "黑龙江", "东北"),
        ("CN-SH", "上海", "华东"), ("CN-JS", "江苏", "华东"), ("CN-ZJ", "浙江", "华东"), ("CN-AH", "安徽", "华东"), ("CN-FJ", "福建", "华东"), ("CN-JX", "江西", "华东"), ("CN-SD", "山东", "华东"),
        ("CN-HE", "河南", "华中"), ("CN-HU", "湖北", "华中"), ("CN-HN", "湖南", "华中"),
        ("CN-GD", "广东", "华南"), ("CN-GX", "广西", "华南"), ("CN-HA", "海南", "华南"),
        ("CN-CQ", "重庆", "西南"), ("CN-SC", "四川", "西南"), ("CN-GZ", "贵州", "西南"), ("CN-YN", "云南", "西南"), ("CN-XZ", "西藏", "西南"),
        ("CN-SA", "陕西", "西北"), ("CN-GS", "甘肃", "西北"), ("CN-QH", "青海", "西北"), ("CN-NX", "宁夏", "西北"), ("CN-XJ", "新疆", "西北"),
    ];

    /// <summary>按位置算出一个看起来自然的数：东部高、西部低（同 vue 示例的 sampleValue）。</summary>
    public static decimal SampleValue(int index, double scale)
    {
        var wave = Math.Sin(index * 1.7) * 0.35 + Math.Cos(index * 0.6) * 0.25;
        return (decimal)(Math.Floor(scale * Math.Max(0.05, 0.9 - index / 40.0 + wave) * 10 + 0.5) / 10);
    }
}
