using Rune.Styles;

namespace Rune.UI;

/// <summary>要检查对比度的一对颜色 token：前景、背景、最低对比度与用途。</summary>
/// <param name="Foreground">前景 token（名字同 vue-tsx-pro，如 accentText）。</param>
/// <param name="Background">背景 token。</param>
/// <param name="Minimum">最低对比度。</param>
/// <param name="Label">用途说明。</param>
public sealed record ContrastPair(string Foreground, string Background, double Minimum, string Label);

/// <summary>一处对比度不够的组合。</summary>
/// <param name="Pair">哪一对。</param>
/// <param name="Ratio">实际对比度。</param>
public sealed record ContrastIssue(ContrastPair Pair, double Ratio);

/// <summary>
/// 主题颜色要满足的对比度（WCAG 2.2 AA，同 vue-tsx-pro 的 contrast.ts）：哪些文字放在哪些底色上、各要多少。
/// 主题编辑器按它实时提示改出来的配色；token 的取值从主题对象里读（内置主题的颜色都是字面量），半透明的背景叠在页面背景上算。
/// </summary>
public static class ThemeContrast
{
    const double TextMinimum = ColorContrast.Text;
    const double UiMinimum = ColorContrast.Ui;

    /// <summary>要检查的颜色组合。</summary>
    public static IReadOnlyList<ContrastPair> Pairs { get; } = BuildPairs();

    static List<ContrastPair> BuildPairs()
    {
        var pairs = new List<ContrastPair>();
        void Add(string fg, string bg, double minimum, string label) => pairs.Add(new(fg, bg, minimum, label));
        foreach (var bg in new[] { "background", "surface", "surfaceMuted", "surfaceRaised", "fill", "fillHover", "fillActive" }) Add("text", bg, TextMinimum, "正文");
        foreach (var bg in new[] { "background", "surface", "surfaceMuted", "surfaceRaised", "fill" }) Add("textMuted", bg, TextMinimum, "次要文字");
        Add("textPlaceholder", "surface", UiMinimum, "占位文字");
        Add("accentContrast", "accent", TextMinimum, "主按钮");
        Add("accentContrast", "accentHover", TextMinimum, "主按钮悬停");
        foreach (var bg in new[] { "background", "surface", "surfaceRaised", "accentSoft", "accentSoftHover" }) Add("accentText", bg, TextMinimum, "链接、选中项");
        Add("dangerContrast", "danger", TextMinimum, "危险按钮");
        Add("dangerContrast", "dangerHover", TextMinimum, "危险按钮悬停");
        foreach (var bg in new[] { "background", "surface", "dangerSoft", "dangerSoftHover" }) Add("dangerText", bg, TextMinimum, "错误文字");
        foreach (var (fg, soft) in new[] { ("successText", "successSoft"), ("warningText", "warningSoft"), ("infoText", "infoSoft") })
        {
            Add(fg, soft, TextMinimum, "提示条");
            Add(fg, "background", TextMinimum, "提示文字");
            Add(fg, "surface", TextMinimum, "提示文字");
        }
        foreach (var tone in new[] { "success", "warning", "info" })
        {
            Add($"{tone}Contrast", tone, TextMinimum, "状态实色（按钮、实底徽标）");
            Add($"{tone}Contrast", $"{tone}Hover", TextMinimum, "状态实色悬停");
            Add($"{tone}Text", $"{tone}SoftHover", TextMinimum, "状态浅底悬停");
        }
        Add("inverseText", "inverseSurface", TextMinimum, "反色表面");
        // 占位文字也是要读的文字：表面色的输入框与浅底（soft）的输入框
        Add("textPlaceholder", "surface", TextMinimum, "占位文字");
        Add("textPlaceholder", "fill", TextMinimum, "占位文字（浅底输入框）");
        // 遮罩是半透明的：按叠在页面背景上算（盖在图片上时由遮罩本身保证够深）
        Add("onScrim", "scrim", TextMinimum, "图片上的遮罩");
        // 代码块（surfaceMuted）、行内代码与高亮行（fill）、diff 的增删行上都有高亮的代码
        foreach (var fg in new[] { "codeKeyword", "codeString", "codeNumber", "codeFunction", "codeType", "codeComment", "codeTag", "codeAttribute" })
            foreach (var bg in new[] { "surfaceMuted", "fill", "codeInserted", "codeDeleted" }) Add(fg, bg, TextMinimum, "代码高亮");
        Add("text", "codeInserted", TextMinimum, "diff 新增行");
        Add("text", "codeDeleted", TextMinimum, "diff 删除行");
        Add("text", "selection", TextMinimum, "选中的文字");
        Add("text", "highlight", TextMinimum, "高亮标记");
        Add("focusRing", "background", UiMinimum, "焦点环");
        Add("focusRing", "surface", UiMinimum, "焦点环");
        // 选中的勾选框、开关、滑块与进度条的实底色：在表面与轨道上
        foreach (var fg in new[] { "accent", "inverseSurface", "infoText", "successText", "warningText", "dangerText" })
        {
            Add(fg, "surface", UiMinimum, "选中的勾选框、开关");
            Add(fg, "fill", UiMinimum, "滑块、进度条");
        }
        for (var series = 1; series <= 8; series++)
        {
            Add($"chart{series}", "background", UiMinimum, "图表系列");
            Add($"chart{series}", "surface", UiMinimum, "图表系列");
        }
        return pairs;
    }

    /// <summary>
    /// 按 <see cref="Pairs"/> 检查一套主题的配色，返回对比度不够的组合；有读不懂的颜色时抛 <see cref="FormatException"/>。
    /// </summary>
    public static IReadOnlyList<ContrastIssue> Check(Theme theme)
    {
        string Read(string name) => theme.ValueOf(Token(name).ToString()) ?? throw new FormatException($"主题 {theme.Name} 没有 {name} 的取值");
        var page = Read("background");
        var issues = new List<ContrastIssue>();
        foreach (var pair in Pairs)
        {
            var ratio = ColorContrast.Ratio(Read(pair.Foreground), Read(pair.Background), page);
            if (ratio < pair.Minimum) issues.Add(new(pair, ratio));
        }
        return issues;
    }

    /// <summary>vue-tsx-pro 的颜色 token 名 → Rune 的 token（同 theme-gen 的对应规则：状态色在 Tones，代码在 CodeColors，图表在 ChartColors）。</summary>
    static CssColor Token(string name)
    {
        foreach (var (prefix, tone) in new[] { ("accent", Tones.Accent), ("danger", Tones.Danger), ("success", Tones.Success), ("warning", Tones.Warning), ("info", Tones.Info) })
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            switch (name[prefix.Length..])
            {
                case "": return tone.Solid;
                case "Hover": return tone.SolidHover;
                case "Contrast": return tone.Contrast;
                case "Text": return tone.Text;
                case "Soft": return tone.Soft;
                case "SoftHover": return tone.SoftHover;
                case "Border": return tone.Border;
            }
        }
        if (name.StartsWith("chart", StringComparison.Ordinal) && int.TryParse(name[5..], out var series))
            return series switch
            {
                1 => ChartColors.Series1, 2 => ChartColors.Series2, 3 => ChartColors.Series3, 4 => ChartColors.Series4,
                5 => ChartColors.Series5, 6 => ChartColors.Series6, 7 => ChartColors.Series7, _ => ChartColors.Series8,
            };
        return name switch
        {
            "background" => Colors.Background,
            "surface" => Colors.Surface,
            "surfaceMuted" => Colors.SurfaceMuted,
            "surfaceRaised" => Colors.SurfaceRaised,
            "fill" => Colors.Fill,
            "fillHover" => Colors.FillHover,
            "fillActive" => Colors.FillActive,
            "text" => Colors.Text,
            "textMuted" => Colors.TextMuted,
            "textPlaceholder" => Colors.TextPlaceholder,
            "inverseSurface" => Colors.InverseSurface,
            "inverseText" => Colors.InverseText,
            "focusRing" => Colors.FocusRing,
            "scrim" => Colors.Scrim,
            "onScrim" => Colors.OnScrim,
            "selection" => Colors.Selection,
            "highlight" => Colors.Highlight,
            "codeKeyword" => CodeColors.Keyword,
            "codeString" => CodeColors.String,
            "codeNumber" => CodeColors.Number,
            "codeFunction" => CodeColors.Function,
            "codeType" => CodeColors.Type,
            "codeComment" => CodeColors.Comment,
            "codeTag" => CodeColors.Tag,
            "codeAttribute" => CodeColors.Attribute,
            "codeInserted" => CodeColors.Inserted,
            "codeDeleted" => CodeColors.Deleted,
            _ => throw new ArgumentException($"不认识的颜色 token：{name}", nameof(name)),
        };
    }
}
