using System.Globalization;

namespace Rune.UI;

/// <summary>颜色的写法（ColorPicker 的输入框在它们之间切换）。</summary>
public enum ColorFormat
{
    /// <summary>#rrggbb / #rrggbbaa。</summary>
    Hex,

    /// <summary>rgb() / rgba()。</summary>
    Rgb,

    /// <summary>hsl() / hsla()。</summary>
    Hsl,
}

/// <summary>
/// HSV 颜色（同 vue-tsx-pro 的 Hsva）：取色器内部按它记，拖到灰色、黑色时色相不丢。
/// </summary>
/// <param name="H">色相（0–360）。</param>
/// <param name="S">饱和度（0–1）。</param>
/// <param name="V">明度（0–1）。</param>
/// <param name="A">不透明度（0–1）。</param>
public readonly record struct ColorHsva(double H, double S, double V, double A = 1)
{
    /// <summary>解析 CSS 颜色写法（同 <see cref="ColorValue.TryParse"/>）成 HSV；解析不了时返回 null。灰色的色相是 0。</summary>
    public static ColorHsva? Parse(string? text) => ColorValue.TryParse(text, out var color) ? From(color) : null;

    /// <summary>sRGB → HSV。</summary>
    public static ColorHsva From(ColorValue color)
    {
        var (r, g, b) = (color.R, color.G, color.B);
        var max = Math.Max(r, Math.Max(g, b));
        var delta = max - Math.Min(r, Math.Min(g, b));
        var hue = delta == 0 ? 0
            : max == r ? 60 * ((g - b) / delta % 6)
            : max == g ? 60 * ((b - r) / delta + 2)
            : 60 * ((r - g) / delta + 4);
        return new(hue < 0 ? hue + 360 : hue, max == 0 ? 0 : delta / max, max, color.A);
    }

    /// <summary>HSV → sRGB。</summary>
    public ColorValue ToColor()
    {
        var chroma = V * S;
        var h = (H % 360 + 360) % 360 / 60;
        var x = chroma * (1 - Math.Abs(h % 2 - 1));
        var (r, g, b) = h switch
        {
            < 1 => (chroma, x, 0d),
            < 2 => (x, chroma, 0d),
            < 3 => (0d, chroma, x),
            < 4 => (0d, x, chroma),
            < 5 => (x, 0d, chroma),
            _ => (chroma, 0d, x),
        };
        var m = V - chroma;
        return new(r + m, g + m, b + m, A);
    }

    /// <summary>#rrggbb（不透明度小于 1 时 #rrggbbaa）。</summary>
    public string ToHex() => ToColor().ToHex();

    /// <summary>按写法格式化颜色；解析不了时原样返回。</summary>
    public static string Format(string text, ColorFormat format)
    {
        if (!ColorValue.TryParse(text, out var color)) return text;
        var alpha = color.A < 1 ? Number(color.A, 3) : null;
        switch (format)
        {
            case ColorFormat.Rgb:
                var (r, g, b) = (Channel(color.R), Channel(color.G), Channel(color.B));
                return alpha is null ? $"rgb({r}, {g}, {b})" : $"rgba({r}, {g}, {b}, {alpha})";
            case ColorFormat.Hsl:
                var (h, s, l) = Hsl(color);
                return alpha is null ? $"hsl({Number(h, 1)}, {Number(s * 100, 1)}%, {Number(l * 100, 1)}%)" : $"hsla({Number(h, 1)}, {Number(s * 100, 1)}%, {Number(l * 100, 1)}%, {alpha})";
            default:
                return color.ToHex();
        }
    }

    static int Channel(double value) => (int)Math.Round(Math.Clamp(value, 0, 1) * 255, MidpointRounding.AwayFromZero);

    static string Number(double value, int digits) => Math.Round(value, digits).ToString(CultureInfo.InvariantCulture);

    static (double H, double S, double L) Hsl(ColorValue color)
    {
        var hsv = From(color);
        var l = hsv.V * (1 - hsv.S / 2);
        var s = l is 0 or 1 ? 0 : (hsv.V - l) / Math.Min(l, 1 - l);
        return (hsv.H, s, l);
    }
}
