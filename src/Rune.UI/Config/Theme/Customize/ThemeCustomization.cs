using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 主题定制（同 vue-tsx-pro 的 customize.ts、palette.ts）：几个旋钮（品牌色、中性色、圆角、缩放、字体、动效）换算成 token 覆盖，叠在任意一套主题上。
/// <code>
/// var theme = ThemeCustomization.Apply(Themes.Light, new() { Accent = AccentScale.Crimson, Radius = ThemeRadius.Large });
/// ThemeCustomization.Code(Themes.Light, settings)   // 可以粘贴进项目的 C# 代码
/// </code>
/// 每个旋钮只改相关的一组 token，层级关系不变：换品牌色、中性色按 Radix 的级别整组替换（底色、悬停、边框、文字），
/// 圆角按这套主题自己的圆角等比缩放，字号按默认值缩放，动效按倍数缩放时长（减少动态效果时主题的条件块仍把时长写成 0）。
/// </summary>
public static class ThemeCustomization
{
    /// <summary>缩放的可选值。</summary>
    public static IReadOnlyList<double> Scalings { get; } = [0.9, 0.95, 1, 1.05, 1.1];

    /// <summary>一个颜色 token 取色阶的哪一级：亮色、暗色各一级（1–12），C# 里的写法用于导出代码。</summary>
    internal sealed record Step(CssColor Token, string Expression, int Light, int Dark);

    // 品牌色 token 取第几级（Radix 的用途约定：9–10 实色，11 文字，3–4 浅底，7 边框）
    internal static readonly Step[] AccentSteps =
    [
        new(Tones.Accent.Solid, "Tones.Accent.Solid", 9, 9),
        new(Tones.Accent.SolidHover, "Tones.Accent.SolidHover", 10, 10),
        new(Tones.Accent.Text, "Tones.Accent.Text", 11, 11),
        new(Tones.Accent.Soft, "Tones.Accent.Soft", 3, 3),
        new(Tones.Accent.SoftHover, "Tones.Accent.SoftHover", 4, 4),
        new(Tones.Accent.Border, "Tones.Accent.Border", 7, 7),
        new(Colors.FocusRing, "Colors.FocusRing", 9, 9),
        new(Colors.Selection, "Colors.Selection", 5, 5),
        new(CodeColors.Keyword, "CodeColors.Keyword", 11, 11),
        // 图表的第一个系列跟随品牌色
        new(ChartColors.Series1, "ChartColors.Series1", 9, 9),
    ];

    // 中性色 token 取第几级（1–2 背景，3–5 填充，6–8 边框，11–12 文字）
    internal static readonly Step[] NeutralSteps =
    [
        // 亮色的页面着一点色（2 级），卡片、浮层是白色（见 PaletteColors）
        new(Colors.Background, "Colors.Background", 2, 1),
        new(Colors.Surface, "Colors.Surface", 1, 2),
        new(Colors.SurfaceMuted, "Colors.SurfaceMuted", 2, 2),
        // 暗色不取第 3 级：那是 fill，滑块轨道、进度条底色、标签放进浮层会看不见；浮层靠描边与阴影分层（同 Radix Themes 的 panel）
        new(Colors.SurfaceRaised, "Colors.SurfaceRaised", 1, 2),
        new(Colors.Fill, "Colors.Fill", 3, 3),
        new(Colors.FillHover, "Colors.FillHover", 4, 4),
        new(Colors.FillActive, "Colors.FillActive", 5, 5),
        new(Colors.Text, "Colors.Text", 12, 12),
        new(Colors.TextMuted, "Colors.TextMuted", 11, 11),
        new(Colors.TextPlaceholder, "Colors.TextPlaceholder", 11, 11),
        new(Colors.TextDisabled, "Colors.TextDisabled", 8, 8),
        new(Colors.BorderMuted, "Colors.BorderMuted", 6, 6),
        new(Colors.Border, "Colors.Border", 7, 7),
        new(Colors.BorderStrong, "Colors.BorderStrong", 8, 8),
        new(Colors.InverseSurface, "Colors.InverseSurface", 12, 12),
        new(Colors.InverseText, "Colors.InverseText", 1, 1),
        new(CodeColors.Comment, "CodeColors.Comment", 11, 11),
    ];

    // 圆角、字号、时长、字体的 token 与 C# 里的写法
    static readonly (CssLength Token, string Expression)[] RadiusTokens =
        [(Radii.Xs, "Radii.Xs"), (Radii.Sm, "Radii.Sm"), (Radii.Md, "Radii.Md"), (Radii.Lg, "Radii.Lg"), (Radii.Xl, "Radii.Xl")];

    static readonly (CssLength Token, string Expression)[] FontSizeTokens =
    [
        (FontSizes.Xs, "FontSizes.Xs"), (FontSizes.Sm, "FontSizes.Sm"), (FontSizes.Md, "FontSizes.Md"), (FontSizes.Lg, "FontSizes.Lg"),
        (FontSizes.Xl, "FontSizes.Xl"), (FontSizes.Xxl, "FontSizes.Xxl"), (FontSizes.Xxxl, "FontSizes.Xxxl"),
    ];

    static readonly (CssValue Token, string Expression)[] DurationTokens =
        [(Durations.Xs, "Durations.Xs"), (Durations.Sm, "Durations.Sm"), (Durations.Md, "Durations.Md"), (Durations.Lg, "Durations.Lg"), (Durations.Xl, "Durations.Xl")];

    static double RadiusFactor(ThemeRadius radius) => radius switch { ThemeRadius.None => 0, ThemeRadius.Small => 0.5, ThemeRadius.Large => 1.75, _ => 1 };

    static double MotionFactor(ThemeMotion motion) => motion switch { ThemeMotion.None => 0, ThemeMotion.Fast => 0.6, ThemeMotion.Slow => 1.5, _ => 1 };

    /// <summary>色阶的代表色（第 9 级实色）：主题编辑器里选色阶的色块。</summary>
    public static string Swatch(AccentScale scale, bool dark) => RadixColors.Of(scale, dark)[8];

    /// <inheritdoc cref="Swatch(AccentScale, bool)"/>
    public static string Swatch(NeutralScale scale, bool dark) => RadixColors.Of(scale, dark)[8];

    /// <summary>`0.4em` × 1.75 → `0.7em`：数值部分乘倍数，单位不变；保留三位小数。</summary>
    static string Scale(string value, double factor)
    {
        var digits = value.TrimStart('-').TakeWhile(c => char.IsAsciiDigit(c) || c == '.').Count() + (value.StartsWith('-') ? 1 : 0);
        if (digits == 0 || !double.TryParse(value[..digits], NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return value;
        return Math.Round(number * factor, 3).ToString(CultureInfo.InvariantCulture) + value[digits..];
    }

    /// <summary>设置换算成 token 覆盖（token → CSS 文本，附带 C# 里的写法）；亮暗、圆角按 <paramref name="theme"/>。</summary>
    internal static IReadOnlyList<(string Token, string Expression, string Value)> Overrides(Theme theme, ThemeSettings settings)
    {
        var result = new List<(string, string, string)>();
        var dark = theme.IsDark;
        if (settings.Accent is { } accent)
        {
            var scale = RadixColors.Of(accent, dark);
            foreach (var step in AccentSteps) result.Add((step.Token.ToString(), step.Expression, scale[(dark ? step.Dark : step.Light) - 1]));
            // 实色按钮上的文字：亮黄、青柠这类浅色实色配深色字，其余配白字
            result.Add((Tones.Accent.Contrast.ToString(), "Tones.Accent.Contrast", ColorContrast.ReadableText(scale[8])));
        }
        if (settings.Neutral is { } neutral)
        {
            var scale = RadixColors.Of(neutral, dark);
            var colors = NeutralSteps.ToDictionary(step => step.Expression, step => scale[(dark ? step.Dark : step.Light) - 1]);
            if (!dark)
            {
                // 着色的页面 + 白色的卡片与浮层；弱化的区域取 2、3 级的中间，放在页面上也看得出
                colors["Colors.Surface"] = colors["Colors.SurfaceRaised"] = "#ffffff";
                colors["Colors.SurfaceMuted"] = ColorContrast.Mix(colors["Colors.Background"], colors["Colors.Fill"]);
            }
            foreach (var step in NeutralSteps) result.Add((step.Token.ToString(), step.Expression, colors[step.Expression]));
            result.Add((Colors.SurfaceTranslucent.ToString(), "Colors.SurfaceTranslucent", ColorContrast.WithAlpha(colors["Colors.Background"], 0.8)));
        }
        if (settings.Radius is { } radius and not ThemeRadius.Medium)
        {
            // 按这套主题自己的圆角缩放：自带圆角的主题上小、默认、大才是由小到大
            foreach (var (token, expression) in RadiusTokens)
                if (theme.ValueOf(token.ToString()) is { } value) result.Add((token.ToString(), expression, Scale(value, RadiusFactor(radius))));
        }
        if (settings.Scaling is { } scaling && scaling != 1)
        {
            foreach (var (token, expression) in FontSizeTokens)
                if (Themes.Base.ValueOf(token.ToString()) is { } value) result.Add((token.ToString(), expression, Scale(value, scaling)));
        }
        if (settings.Font == ThemeFont.SerifHeadings && Themes.Base.ValueOf(Fonts.Serif.ToString()) is { } serif)
            result.Add((Fonts.Heading.ToString(), "Fonts.Heading", serif));
        if (settings.Motion is { } motion and not ThemeMotion.Normal)
        {
            foreach (var (token, expression) in DurationTokens)
                if (Themes.Base.ValueOf(token.ToString()) is { } value) result.Add((token.ToString(), expression, Scale(value, MotionFactor(motion))));
        }
        return result;
    }

    static readonly ConditionalWeakTable<Theme, Dictionary<ThemeSettings, Theme>> applied = [];

    /// <summary>
    /// 在一套主题上套用设置（名字后加 -custom）；没动旋钮时原样返回。同一套主题、同样的设置得到同一个对象（主题类只编译一次）。
    /// </summary>
    public static Theme Apply(Theme theme, ThemeSettings settings)
    {
        if (settings.IsEmpty) return theme;
        var cache = applied.GetOrCreateValue(theme);
        lock (cache)
        {
            if (cache.TryGetValue(settings, out var cached)) return cached;
            var overrides = Overrides(theme, settings);
            var result = overrides.Count == 0 ? theme : new Theme($"{theme.Name}-custom", theme, overrides.Select(o => KeyValuePair.Create(o.Token, o.Value)));
            cache[settings] = result;
            return result;
        }
    }

    /// <summary>
    /// 套用这些设置的 C# 代码，可以直接粘贴进项目：<c>new Theme("my-theme", basedOn: Themes.Light) { … }</c>。
    /// 基础主题是内置主题时写它的名字，否则按亮暗写 Themes.Light / Themes.Dark。
    /// </summary>
    public static string Code(Theme theme, ThemeSettings settings)
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"public static Theme MyTheme => Css.Define(static () => new Theme(\"my-theme\", basedOn: {BaseExpression(theme)})\n{{\n");
        foreach (var (_, expression, value) in Overrides(theme, settings))
            builder.Append(CultureInfo.InvariantCulture, $"    [{expression}] = \"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\",\n");
        return builder.Append("});\n").ToString();
    }

    // 内置主题的属性名（high-contrast → Themes.HighContrast）；定制过的按它的基础主题
    static string BaseExpression(Theme theme)
    {
        for (var current = theme; current is not null; current = current.BasedOn)
        {
            if (!Themes.All.Contains(current)) continue;
            return "Themes." + string.Concat(current.Name.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        }
        return theme.IsDark ? "Themes.Dark" : "Themes.Light";
    }
}
