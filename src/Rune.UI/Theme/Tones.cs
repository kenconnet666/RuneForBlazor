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

/// <summary>
/// 一个色调的一组颜色 token（<c>--rune-tones-{色调}-{用途}</c>）。按色调变化的样式写成以它为参数的函数，
/// 每种外观只写一遍，样式里直接看得到用的是哪个 token：
/// <code>static Style Solid(ToneColors tone) => new() { Color = tone.Contrast, BackgroundColor = tone.Solid };</code>
/// </summary>
public sealed record ToneColors
{
    internal ToneColors(string tone)
    {
        Solid = Token(tone, "solid");
        SolidHover = Token(tone, "solid-hover");
        Contrast = Token(tone, "contrast");
        Text = Token(tone, "text");
        Soft = Token(tone, "soft");
        SoftHover = Token(tone, "soft-hover");
        Border = Token(tone, "border");
    }

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

    /// <summary>这组颜色的全部 token（主题检查、测试用）。</summary>
    public IEnumerable<CssColor> All => [Solid, SolidHover, Contrast, Text, Soft, SoftHover, Border];

    static CssColor Token(string tone, string use) => $"var(--rune-tones-{tone}-{use})";
}

/// <summary>各色调的颜色 token。</summary>
public static class Tones
{
    /// <summary>中性。</summary>
    public static readonly ToneColors Neutral = new("neutral");

    /// <summary>品牌强调。</summary>
    public static readonly ToneColors Accent = new("accent");

    /// <summary>提示信息。</summary>
    public static readonly ToneColors Info = new("info");

    /// <summary>成功。</summary>
    public static readonly ToneColors Success = new("success");

    /// <summary>警告。</summary>
    public static readonly ToneColors Warning = new("warning");

    /// <summary>危险。</summary>
    public static readonly ToneColors Danger = new("danger");

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
