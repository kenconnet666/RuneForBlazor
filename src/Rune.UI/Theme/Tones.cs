using Rune.Styles;

namespace Rune.UI;

/// <summary>色调：按钮、标记、提示共用同一套（同名就同义）。</summary>
public enum Tone
{
    /// <summary>中性：不带倾向的操作与信息。</summary>
    Neutral,

    /// <summary>品牌强调（按钮默认）。</summary>
    Accent,

    /// <summary>提示信息。</summary>
    Info,

    /// <summary>成功、已完成。</summary>
    Success,

    /// <summary>警告、需要注意。</summary>
    Warning,

    /// <summary>危险、错误、不可撤销的操作。</summary>
    Danger,
}

/// <summary>标记、提示条这类状态表面的样式（同 vue-tsx-pro 的 StatusVariant），颜色见 <see cref="SharedStyles.StatusColors"/>。</summary>
public enum StatusVariant
{
    /// <summary>实底：最醒目，计数、状态点。</summary>
    Solid,

    /// <summary>浅色底（默认）。</summary>
    Soft,

    /// <summary>浅色底加描边。</summary>
    Surface,

    /// <summary>只有描边。</summary>
    Outline,
}

/// <summary>
/// 一个色调的一组颜色。按色调变化的样式写成以它为参数的函数，每种外观只写一遍，样式里直接看得到用的是哪个 token：
/// <code>static Style Solid(ToneColors tone) => new() { Color = tone.Contrast, BackgroundColor = tone.Solid };</code>
/// 品牌色与四种状态色各有一组 token（<c>--rune-tones-{色调}-{用途}</c>，主题给值）；中性色直接用 <see cref="Colors"/> 里的 token（同 vue-tsx-pro）。
/// </summary>
public sealed record ToneColors
{
    ToneColors(CssColor solid, CssColor solidHover, CssColor contrast, CssColor text, CssColor soft, CssColor softHover, CssColor border,
        CssColor? surface = null, CssColor? quietHover = null)
    {
        Solid = solid;
        SolidHover = solidHover;
        Contrast = contrast;
        Text = text;
        Soft = soft;
        SoftHover = softHover;
        Border = border;
        Surface = surface ?? soft;
        QuietHover = quietHover ?? soft;
    }

    /// <summary>一组自己的 token（主题里给值）。</summary>
    internal static ToneColors Tokens(string tone) => new(
        Token(tone, "solid"), Token(tone, "solid-hover"), Token(tone, "contrast"), Token(tone, "text"),
        Token(tone, "soft"), Token(tone, "soft-hover"), Token(tone, "border"));

    /// <summary>中性：反色表面做实底，填充做浅色底（不另设 token）。</summary>
    internal static ToneColors FromColors() => new(
        Colors.InverseSurface,
        $"color-mix(in oklab, {Colors.InverseSurface} 85%, {Colors.Background})",
        Colors.InverseText,
        Colors.Text,
        Colors.Fill,
        Colors.FillHover,
        Colors.Border,
        surface: Colors.Surface,
        quietHover: Colors.FillHover);

    /// <summary>实底的背景（主按钮、实底标记）。</summary>
    public CssColor Solid { get; }

    /// <summary>实底悬停时的背景。</summary>
    public CssColor SolidHover { get; }

    /// <summary>实底上的文字（与 <see cref="Solid"/> 对比度 ≥ 4.5:1）。</summary>
    public CssColor Contrast { get; }

    /// <summary>浅色底、透明底上的文字。</summary>
    public CssColor Text { get; }

    /// <summary>浅色底。</summary>
    public CssColor Soft { get; }

    /// <summary>浅色底悬停。</summary>
    public CssColor SoftHover { get; }

    /// <summary>描边。</summary>
    public CssColor Border { get; }

    /// <summary>浅色底加描边（Surface 样式）的背景：彩色色调是浅色底，中性是表面色。</summary>
    public CssColor Surface { get; }

    /// <summary>透明底（描边、幽灵样式）悬停时的背景：彩色色调是浅色底，中性是填充的悬停色。</summary>
    public CssColor QuietHover { get; }

    /// <summary>这组颜色的全部取值（主题检查、测试用）。</summary>
    public IEnumerable<CssColor> All => [Solid, SolidHover, Contrast, Text, Soft, SoftHover, Border, Surface, QuietHover];

    static CssColor Token(string tone, string use) => $"var(--rune-tones-{tone}-{use})";
}

/// <summary>各色调的颜色 token。</summary>
public static class Tones
{
    /// <summary>色调的默认状态图标（同 vue-tsx-pro 的 statusIcons）：Alert、Popconfirm 不写图标时用它。</summary>
    public static IconData StatusIcon(Tone tone) => tone switch
    {
        Tone.Success => BuiltInIcons.CircleCheck,
        Tone.Warning => BuiltInIcons.TriangleExclamationPoint,
        Tone.Danger => BuiltInIcons.CircleExclamationPoint,
        _ => BuiltInIcons.Info,
    };

    /// <summary>中性（用 <see cref="Colors"/> 的反色表面、填充、文字、边框）。</summary>
    public static readonly ToneColors Neutral = ToneColors.FromColors();

    /// <summary>品牌强调。</summary>
    public static readonly ToneColors Accent = ToneColors.Tokens("accent");

    /// <summary>提示信息。</summary>
    public static readonly ToneColors Info = ToneColors.Tokens("info");

    /// <summary>成功。</summary>
    public static readonly ToneColors Success = ToneColors.Tokens("success");

    /// <summary>警告。</summary>
    public static readonly ToneColors Warning = ToneColors.Tokens("warning");

    /// <summary>危险。</summary>
    public static readonly ToneColors Danger = ToneColors.Tokens("danger");

    /// <summary>色调对应的颜色 token。</summary>
    public static ToneColors Of(Tone tone) => tone switch
    {
        Tone.Neutral => Neutral,
        Tone.Info => Info,
        Tone.Success => Success,
        Tone.Warning => Warning,
        Tone.Danger => Danger,
        _ => Accent,
    };
}

/// <summary>文字的色调（同 vue-tsx-pro 文字的 tone）：比组件的 <see cref="Tone"/> 多了次要文字与继承。</summary>
public enum TextTone
{
    /// <summary>正文色。</summary>
    Neutral,

    /// <summary>次要文字色：说明、时间、辅助信息。</summary>
    Muted,

    /// <summary>品牌色文字：链接、强调。</summary>
    Accent,

    /// <summary>信息。</summary>
    Info,

    /// <summary>成功。</summary>
    Success,

    /// <summary>警告。</summary>
    Warning,

    /// <summary>危险：错误提示、破坏性操作的说明。</summary>
    Danger,

    /// <summary>跟随所在的文字颜色：放在有底色的通栏、标记里时用。</summary>
    Inherit,
}

/// <summary>色调与文字色调的换算。</summary>
public static class TextToneExtensions
{
    /// <summary>组件的色调对应的文字色调（提示条、气泡确认的图标用所在组件的色调）。</summary>
    public static TextTone AsText(this Tone tone) => tone switch
    {
        Tone.Neutral => TextTone.Neutral,
        Tone.Info => TextTone.Info,
        Tone.Success => TextTone.Success,
        Tone.Warning => TextTone.Warning,
        Tone.Danger => TextTone.Danger,
        _ => TextTone.Accent,
    };
}
