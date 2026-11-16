using System.Globalization;

namespace Rune.UI;

/// <summary>
/// 颜色的 WCAG 对比度、按对比度挑文字色、混色与换透明度（同 vue-tsx-pro 的 utils/data/color.ts）：
/// 主题编辑器的对比度检查、按品牌色算按钮文字色用它。
/// <code>
/// ColorContrast.Ratio("#3e63dd", "#ffffff")      // 4.6…
/// ColorContrast.ReadableText("#ffc53d")          // "#1c2024"：亮黄底上白字达不到 4.5:1
/// </code>
/// 颜色写法同 <see cref="ColorValue.TryParse"/>；解析不了时抛 <see cref="FormatException"/>。
/// </summary>
public static class ColorContrast
{
    /// <summary>正文文字的最低对比度（WCAG 2.2 AA）。</summary>
    public const double Text = 4.5;

    /// <summary>大字、图形与界面部件（焦点环、勾选框边框）的最低对比度。</summary>
    public const double Ui = 3;

    static ColorValue Parse(string value) =>
        ColorValue.TryParse(value, out var color) ? color : throw new FormatException($"无法解析的颜色：{value}");

    // 半透明的前景叠到不透明的背景上，得到人眼看到的颜色
    static ColorValue Over(ColorValue foreground, ColorValue background) => new(
        foreground.R * foreground.A + background.R * (1 - foreground.A),
        foreground.G * foreground.A + background.G * (1 - foreground.A),
        foreground.B * foreground.A + background.B * (1 - foreground.A));

    // WCAG 的相对亮度：通道先转成线性
    static double Luminance(ColorValue color)
    {
        static double Linear(double channel) => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }

    /// <summary>
    /// 前景与背景的对比度（1–21）。半透明的颜色按实际看到的算：背景先叠到页面底色（<paramref name="page"/>）上，前景再叠到背景上。
    /// </summary>
    public static double Ratio(string foreground, string background, string page = "#ffffff")
    {
        var solidBackground = Over(Parse(background), Parse(page) with { A = 1 });
        var solidForeground = Over(Parse(foreground), solidBackground);
        var (a, b) = (Luminance(solidForeground), Luminance(solidBackground));
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>
    /// 实色背景上的文字色（实色按钮上用白字还是深色字）：按候选的顺序，第一个达到正文对比度（4.5:1）的就用它；都达不到时取对比度最高的。
    /// 不给候选时是白、深灰、黑。
    /// </summary>
    public static string ReadableText(string background, IReadOnlyList<string>? candidates = null)
    {
        candidates ??= ["#ffffff", "#1c2024", "#000000"];
        var (best, bestRatio) = (candidates[0], 0d);
        foreach (var candidate in candidates)
        {
            var ratio = Ratio(candidate, background);
            if (ratio >= Text) return candidate;
            if (ratio > bestRatio) (best, bestRatio) = (candidate, ratio);
        }
        return best;
    }

    /// <summary>两个颜色在 sRGB 里按比例混合（<paramref name="amount"/> 是 b 的占比，0–1），输出 #rrggbb。</summary>
    public static string Mix(string a, string b, double amount = 0.5)
    {
        var (from, to) = (Parse(a), Parse(b));
        return new ColorValue(from.R + (to.R - from.R) * amount, from.G + (to.G - from.G) * amount, from.B + (to.B - from.B) * amount).ToHex();
    }

    /// <summary>同一个颜色换成给定的不透明度（0–1），输出 rgba()。</summary>
    public static string WithAlpha(string value, double alpha)
    {
        var color = Parse(value);
        static int Byte(double channel) => (int)Math.Round(Math.Clamp(channel, 0, 1) * 255, MidpointRounding.AwayFromZero);
        return string.Create(CultureInfo.InvariantCulture, $"rgba({Byte(color.R)}, {Byte(color.G)}, {Byte(color.B)}, {Math.Round(alpha, 3)})");
    }
}
