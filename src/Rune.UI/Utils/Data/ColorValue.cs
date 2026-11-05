using System.Globalization;

namespace Rune.UI;

/// <summary>
/// sRGB 颜色（各通道 0–1）：从 CSS 的颜色写法解析、格式化成 #hex（同 vue-tsx-pro 用 culori 的 parseColor / toHex）。
/// 支持 #rgb / #rgba / #rrggbb / #rrggbbaa、rgb() / rgba()、hsl() / hsla()、hwb()、oklab()、oklch()、颜色名与 transparent；
/// 函数写法用逗号（旧写法）或空格加“/ 透明度”都行，<c>none</c> 当 0。超出 sRGB 色域的（oklch 的高彩度）格式化时按通道夹住。
/// <code>
/// ColorValue.ToHex("rgb(37 99 235 / 50%)")   // "#2563eb80"
/// ColorValue.ToHex("rebeccapurple")          // "#663399"
/// </code>
/// </summary>
/// <param name="R">红（0–1）。</param>
/// <param name="G">绿（0–1）。</param>
/// <param name="B">蓝（0–1）。</param>
/// <param name="A">不透明度（0–1）。</param>
public readonly partial record struct ColorValue(double R, double G, double B, double A = 1)
{
    /// <summary>解析 CSS 颜色写法；解析不了时返回 false。</summary>
    public static bool TryParse(string? text, out ColorValue color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var value = text.Trim().ToLowerInvariant();
        if (value == "transparent")
        {
            color = new(0, 0, 0, 0);
            return true;
        }
        if (value.StartsWith('#')) return TryParseHex(value[1..], out color);
        if (Names.TryGetValue(value, out var rgb))
        {
            color = FromRgb24(rgb);
            return true;
        }
        var open = value.IndexOf('(');
        if (open <= 0 || !value.EndsWith(')') || !TrySplit(value[(open + 1)..^1], out var channels, out var alphaText)) return false;
        if (!TryAlpha(alphaText, out var alpha)) return false;
        return value[..open].Trim() switch
        {
            "rgb" or "rgba" => TryRgb(channels, alpha, out color),
            "hsl" or "hsla" => TryHsl(channels, alpha, out color),
            "hwb" => TryHwb(channels, alpha, out color),
            "oklab" => TryOklab(channels, alpha, out color),
            "oklch" => TryOklch(channels, alpha, out color),
            _ => false,
        };
    }

    /// <summary>格式化成 #rrggbb；不完全不透明时是 #rrggbbaa。</summary>
    public string ToHex()
    {
        var hex = $"#{Byte(R):x2}{Byte(G):x2}{Byte(B):x2}";
        return A < 1 ? hex + Byte(A).ToString("x2", CultureInfo.InvariantCulture) : hex;
    }

    /// <summary>CSS 颜色写法规范成 #hex；解析不了时返回 null。</summary>
    public static string? ToHex(string? text) => TryParse(text, out var color) ? color.ToHex() : null;

    static int Byte(double channel) => (int)Math.Round(Math.Clamp(channel, 0, 1) * 255, MidpointRounding.AwayFromZero);

    static ColorValue FromRgb24(int rgb) => new(((rgb >> 16) & 0xff) / 255d, ((rgb >> 8) & 0xff) / 255d, (rgb & 0xff) / 255d);

    static bool TryParseHex(string digits, out ColorValue color)
    {
        color = default;
        if (digits.Length is not (3 or 4 or 6 or 8) || !digits.All(Uri.IsHexDigit)) return false;
        // #rgb、#rgba：每位重复一次
        if (digits.Length <= 4) digits = string.Concat(digits.Select(c => $"{c}{c}"));
        int At(int index) => int.Parse(digits.AsSpan(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        color = new(At(0) / 255d, At(1) / 255d, At(2) / 255d, digits.Length == 8 ? At(3) / 255d : 1);
        return true;
    }

    // 函数的参数：逗号分隔（旧写法，第 4 个是透明度），或空格分隔、“/” 后是透明度
    static bool TrySplit(string body, out string[] channels, out string? alpha)
    {
        alpha = null;
        if (body.Contains(','))
        {
            var parts = body.Split(',', StringSplitOptions.TrimEntries);
            channels = parts.Length == 4 ? parts[..3] : parts;
            if (parts.Length == 4) alpha = parts[3];
        }
        else
        {
            var slash = body.IndexOf('/');
            if (slash >= 0) alpha = body[(slash + 1)..].Trim();
            channels = (slash >= 0 ? body[..slash] : body).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        return channels.Length == 3;
    }

    // 数字或百分数：百分数按 percentScale 换算（100% = percentScale）；none 当 0
    static bool TryNumber(string token, double percentScale, out double value)
    {
        value = 0;
        if (token == "none") return true;
        var percent = token.EndsWith('%');
        if (!double.TryParse(percent ? token[..^1] : token, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
        if (percent) value = value / 100 * percentScale;
        return true;
    }

    static bool TryAlpha(string? token, out double alpha)
    {
        alpha = 1;
        if (token is null) return true;
        if (!TryNumber(token, 1, out alpha)) return false;
        alpha = Math.Clamp(alpha, 0, 1);
        return true;
    }

    // 色相：不带单位是度；deg、rad、grad、turn
    static bool TryHue(string token, out double degrees)
    {
        degrees = 0;
        if (token == "none") return true;
        (string Unit, double Scale)[] units = [("deg", 1), ("grad", 0.9), ("rad", 180 / Math.PI), ("turn", 360)];
        foreach (var (unit, scale) in units)
        {
            if (!token.EndsWith(unit)) continue;
            if (!double.TryParse(token[..^unit.Length], NumberStyles.Float, CultureInfo.InvariantCulture, out degrees)) return false;
            degrees *= scale;
            return true;
        }
        return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out degrees);
    }

    static bool TryRgb(string[] c, double alpha, out ColorValue color)
    {
        color = default;
        // 数字是 0–255，百分数是 0–100%
        static bool Channel(string token, out double value)
        {
            if (!TryNumber(token, 255, out value)) return false;
            value /= 255;
            return true;
        }
        if (!Channel(c[0], out var r) || !Channel(c[1], out var g) || !Channel(c[2], out var b)) return false;
        color = new(r, g, b, alpha);
        return true;
    }

    static bool TryHsl(string[] c, double alpha, out ColorValue color)
    {
        color = default;
        // 新写法里饱和度、亮度也可以是不带 % 的数字，含义相同
        if (!TryHue(c[0], out var h) || !TryNumber(c[1], 100, out var s) || !TryNumber(c[2], 100, out var l)) return false;
        var (r, g, b) = HslToRgb(h, Math.Clamp(s / 100, 0, 1), Math.Clamp(l / 100, 0, 1));
        color = new(r, g, b, alpha);
        return true;
    }

    static bool TryHwb(string[] c, double alpha, out ColorValue color)
    {
        color = default;
        if (!TryHue(c[0], out var h) || !TryNumber(c[1], 100, out var w) || !TryNumber(c[2], 100, out var k)) return false;
        w = Math.Clamp(w / 100, 0, 1);
        k = Math.Clamp(k / 100, 0, 1);
        // 白与黑加起来超过 100% 时是灰
        if (w + k >= 1)
        {
            var gray = w / (w + k);
            color = new(gray, gray, gray, alpha);
            return true;
        }
        var (r, g, b) = HslToRgb(h, 1, 0.5);
        double Mix(double channel) => channel * (1 - w - k) + w;
        color = new(Mix(r), Mix(g), Mix(b), alpha);
        return true;
    }

    // 亮度 0–1（100% = 1），a、b 的 100% = 0.4
    static bool TryOklab(string[] c, double alpha, out ColorValue color)
    {
        color = default;
        if (!TryNumber(c[0], 1, out var l) || !TryNumber(c[1], 0.4, out var a) || !TryNumber(c[2], 0.4, out var b)) return false;
        color = FromOklab(l, a, b, alpha);
        return true;
    }

    // 彩度的 100% = 0.4
    static bool TryOklch(string[] c, double alpha, out ColorValue color)
    {
        color = default;
        if (!TryNumber(c[0], 1, out var l) || !TryNumber(c[1], 0.4, out var chroma) || !TryHue(c[2], out var h)) return false;
        var radians = h * Math.PI / 180;
        color = FromOklab(l, chroma * Math.Cos(radians), chroma * Math.Sin(radians), alpha);
        return true;
    }

    static (double R, double G, double B) HslToRgb(double hue, double s, double l)
    {
        var h = ((hue % 360) + 360) % 360;
        double Channel(double n)
        {
            var k = (n + h / 30) % 12;
            return l - s * Math.Min(l, 1 - l) * Math.Max(-1, Math.Min(Math.Min(k - 3, 9 - k), 1));
        }
        return (Channel(0), Channel(8), Channel(4));
    }

    // OKLab → 线性 sRGB → sRGB（Björn Ottosson 给出的矩阵，同 CSS Color 4）
    static ColorValue FromOklab(double l, double a, double b, double alpha)
    {
        var l_ = Math.Pow(l + 0.3963377774 * a + 0.2158037573 * b, 3);
        var m_ = Math.Pow(l - 0.1055613458 * a - 0.0638541728 * b, 3);
        var s_ = Math.Pow(l - 0.0894841775 * a - 1.2914855480 * b, 3);
        return new(
            Gamma(4.0767416621 * l_ - 3.3077115913 * m_ + 0.2309699292 * s_),
            Gamma(-1.2684380046 * l_ + 2.6097574011 * m_ - 0.3413193965 * s_),
            Gamma(-0.0041960863 * l_ - 0.7034186147 * m_ + 1.7076147010 * s_),
            alpha);
    }

    static double Gamma(double linear)
    {
        var magnitude = Math.Abs(linear);
        var encoded = magnitude <= 0.0031308 ? 12.92 * magnitude : 1.055 * Math.Pow(magnitude, 1 / 2.4) - 0.055;
        return Math.Sign(linear) * encoded;
    }
}
