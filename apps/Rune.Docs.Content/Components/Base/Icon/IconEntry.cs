using Rune.UI;

namespace Rune.Docs.Components.Base.Icon;

/// <summary>图标库里的一个图标：Lucide 的属性名（Rocket）与数据（文件名 rocket 在 <see cref="IconData.Name"/>）。</summary>
public sealed record IconEntry(string Name, IconData Icon)
{
    /// <summary>
    /// 完整的 SVG 文件：Lucide 的标准属性（描边跟随文字色），可以直接粘到设计稿或别的项目里。
    /// </summary>
    public string Svg(double strokeWidth)
    {
        var size = Icon.ViewBox.Split(' ') is [_, _, var width, var height] ? (width, height) : ("24", "24");
        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{size.Item1}" height="{size.Item2}" viewBox="{Icon.ViewBox}"
              fill="none" stroke="currentColor" stroke-width="{strokeWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)}" stroke-linecap="round" stroke-linejoin="round">
              {Icon.Body}
            </svg>
            """;
    }
}
