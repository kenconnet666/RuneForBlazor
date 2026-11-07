using System.Text;

namespace Rune.UI;

/// <summary>
/// 图表里文字的宽度估算（同 vue-tsx-pro 的 text-size.ts）：留坐标轴的边距、决定类目刻度隔几个写一个、地点名字的避让都要在画之前知道文字多宽。
/// 不去量 DOM（服务端渲染、还没挂上时也要算）：中日韩文字、全角符号按一个字号宽，其余按 0.6 个字号宽，
/// 宁宽勿窄（宽了只是多留一点空，窄了会叠在一起）。
/// <code>ChartText.Width("华东区 2026", 12)          // 3 × 12 + 5 × 7.2 = 72
/// ChartText.Truncate("很长很长的类目名", 60, 12)   // "很长很长…"</code>
/// </summary>
public static class ChartText
{
    // 按一个字号宽算的字：中日韩文字、全角标点、假名、谚文
    static bool Wide(System.Text.Rune rune) => rune.Value is >= 0x1100 and <= 0x115F or >= 0x2E80 and <= 0xA4CF or >= 0xAC00 and <= 0xD7A3
        or >= 0xF900 and <= 0xFAFF or >= 0xFE30 and <= 0xFE4F or >= 0xFF00 and <= 0xFF60 or >= 0xFFE0 and <= 0xFFE6;

    static double WidthOf(System.Text.Rune rune, double fontSize) => Wide(rune) ? fontSize : fontSize * 0.6;

    /// <summary>估算文字的宽度（px）；<paramref name="fontSize"/> 是字号（px），默认 12（主题的 xs）。</summary>
    public static double Width(string text, double fontSize = 12)
    {
        var width = 0.0;
        foreach (var rune in text.EnumerateRunes()) width += WidthOf(rune, fontSize);
        return width;
    }

    /// <summary>文字超过 <paramref name="maxWidth"/> 时截短并加省略号；放得下时原样返回。</summary>
    public static string Truncate(string text, double maxWidth, double fontSize = 12)
    {
        if (Width(text, fontSize) <= maxWidth) return text;
        var room = maxWidth - fontSize;
        var result = new StringBuilder();
        var width = 0.0;
        foreach (var rune in text.EnumerateRunes())
        {
            width += WidthOf(rune, fontSize);
            if (width > room) break;
            result.Append(rune.ToString());
        }
        return result.Append('…').ToString();
    }
}
