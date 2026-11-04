using Rune.Styles;

namespace Rune.UI;

/// <summary>字重档位。</summary>
public enum Weight
{
    /// <summary>400：正文。</summary>
    Normal,

    /// <summary>500：略加强调。</summary>
    Medium,

    /// <summary>600：强调、标题。</summary>
    Semibold,

    /// <summary>700：粗体。</summary>
    Bold,
}

/// <summary>文字的水平对齐（只在块级元素上有效，如 Paragraph、标题）。</summary>
public enum TextAlignment
{
    /// <summary>起始边（从左到右的文字是左对齐）。</summary>
    Start,

    /// <summary>居中。</summary>
    Center,

    /// <summary>结束边。</summary>
    End,

    /// <summary>两端对齐。</summary>
    Justify,
}

/// <summary>换行方式（text-wrap）。</summary>
public enum TextWrapping
{
    /// <summary>正常换行。</summary>
    Wrap,

    /// <summary>不换行。</summary>
    Nowrap,

    /// <summary>各行长度均衡（标题默认）。</summary>
    Balance,

    /// <summary>避免最后一行只剩一个字（段落默认）。</summary>
    Pretty,
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

/// <summary>TextSpan、Paragraph、Strong、Em、Small 的部件。</summary>
public enum TextPart
{
    /// <summary>文字元素本身。</summary>
    Root,
}

/// <summary>H1–H6 的部件。</summary>
public enum HeadingPart
{
    /// <summary>标题元素本身。</summary>
    Root,
}

/// <summary>按键的部件。</summary>
public enum KbdPart
{
    /// <summary>kbd 元素。</summary>
    Root,
}

/// <summary>视觉隐藏的部件。</summary>
public enum VisuallyHiddenPart
{
    /// <summary>span 元素。</summary>
    Root,
}

/// <summary>文字元素的种类：决定元素自带的样式（段落、标题去掉浏览器默认的外边距）。</summary>
public enum TypographyKind
{
    /// <summary>行内文字（span）。</summary>
    Inline,

    /// <summary>段落（p）。</summary>
    Paragraph,

    /// <summary>标题（h1–h6）。</summary>
    Heading,
}

/// <summary>文字的外观：交给 <see cref="TypographyStyles"/> 的档位组合，没写的项继承上下文。</summary>
/// <param name="Kind">元素的种类。</param>
/// <param name="Size">字号档位。</param>
/// <param name="Weight">字重。</param>
/// <param name="Tone">色调（文字色）。</param>
/// <param name="Align">水平对齐。</param>
/// <param name="Wrap">换行方式。</param>
/// <param name="Truncate">单行省略。</param>
/// <param name="LineClamp">最多显示几行。</param>
/// <param name="Numeric">数字等宽。</param>
/// <param name="Mono">等宽字体。</param>
public readonly record struct TypographyAppearance(
    TypographyKind Kind,
    Size? Size = null,
    Weight? Weight = null,
    TextTone? Tone = null,
    TextAlignment? Align = null,
    TextWrapping? Wrap = null,
    bool Truncate = false,
    int? LineClamp = null,
    bool Numeric = false,
    bool Mono = false);

/// <summary>文字类组件（TextSpan、Paragraph、Strong、Em、Small、H1–H6、Kbd、VisuallyHidden）的样式（配方）：每个选项一段样式，按外观组合缓存。</summary>
public static class TypographyStyles
{
    /// <summary>TextSpan、Paragraph、Strong、Em、Small 的部件默认层。</summary>
    public static PartLayers<TextPart> Text(TypographyAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("text", appearance, styles, static (TypographyAppearance a, TextPart _) => Compose(a));

    /// <summary>H1–H6 的部件默认层。</summary>
    public static PartLayers<HeadingPart> Heading(TypographyAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("heading", appearance, styles, static (TypographyAppearance a, HeadingPart _) => Compose(a));

    /// <summary>按键（Kbd）的部件默认层。</summary>
    public static PartLayers<KbdPart> Kbd(ComponentStyles styles) =>
        Recipe.Of("kbd", styles, static (KbdPart _) => KbdRoot);

    /// <summary>视觉隐藏（VisuallyHidden）的部件默认层。</summary>
    public static PartLayers<VisuallyHiddenPart> VisuallyHidden(ComponentStyles styles) =>
        Recipe.Of("visually-hidden", styles, static (VisuallyHiddenPart _) => SharedStyles.VisuallyHidden);

    // 后面的段盖过前面的：元素自带的换行方式排在使用方指定的 Wrap 前面
    static Sx Compose(TypographyAppearance a) => Sx.Of(
        a.Kind switch
        {
            TypographyKind.Paragraph => Paragraph,
            TypographyKind.Heading => HeadingRoot,
            _ => null,
        },
        a.Size is { } size ? ScaleStyles.FontSize(size) : null,
        a.Kind == TypographyKind.Heading ? Tracking(a.Size) : null,
        a.Weight is { } weight ? WeightOf(weight) : null,
        a.Tone is { } tone ? ToneOf(tone) : null,
        a.Align is { } align ? AlignOf(align) : null,
        a.Wrap is { } wrap ? WrapOf(wrap) : null,
        a.Truncate ? Truncate : null,
        a.LineClamp is { } lines ? Clamp(lines) : null,
        a.Numeric ? Numeric : null,
        a.Mono ? Mono : null);

    static Style Paragraph => Css.Define(static () => new Style { Margin = 0, TextWrap = TextWrap.Pretty });

    static Style HeadingRoot => Css.Define(static () => new Style
    {
        Margin = 0,
        FontFamily = Fonts.Heading,
        LineHeight = LineHeights.Tight,
        TextWrap = TextWrap.Balance,
    });

    // 大字号的字间距略收紧，否则显得松散（同 vue-tsx-pro：xxl 收紧、xxxl 再收紧）
    static Style? Tracking(Size? size) => size switch
    {
        UI.Size.Xxl => TightTracking,
        UI.Size.Xxxl => TighterTracking,
        _ => null,
    };

    static Style TightTracking => Css.Define(static () => new Style { LetterSpacing = LetterSpacings.Tight });

    static Style TighterTracking => Css.Define(static () => new Style { LetterSpacing = LetterSpacings.Tighter });

    static Style WeightOf(Weight weight) => Css.Define(weight, static weight => new Style
    {
        FontWeight = weight switch
        {
            UI.Weight.Medium => FontWeights.Medium,
            UI.Weight.Semibold => FontWeights.Semibold,
            UI.Weight.Bold => FontWeights.Bold,
            _ => FontWeights.Normal,
        },
    });

    static Style ToneOf(TextTone tone) => tone switch
    {
        TextTone.Neutral => SharedStyles.ToneText(Tone.Neutral),
        TextTone.Muted => Muted,
        TextTone.Accent => SharedStyles.ToneText(Tone.Accent),
        TextTone.Info => SharedStyles.ToneText(Tone.Info),
        TextTone.Success => SharedStyles.ToneText(Tone.Success),
        TextTone.Warning => SharedStyles.ToneText(Tone.Warning),
        TextTone.Danger => SharedStyles.ToneText(Tone.Danger),
        _ => InheritColor,
    };

    static Style Muted => Css.Define(static () => new Style { Color = Colors.TextMuted });

    static Style InheritColor => Css.Define(static () => new Style { Color = CssWide.Inherit });


    static Style AlignOf(TextAlignment align) => Css.Define(align, static align => new Style
    {
        TextAlign = align switch
        {
            TextAlignment.Center => TextAlign.Center,
            TextAlignment.End => TextAlign.End,
            TextAlignment.Justify => TextAlign.Justify,
            _ => TextAlign.Start,
        },
    });

    static Style WrapOf(TextWrapping wrap) => Css.Define(wrap, static wrap => new Style
    {
        TextWrap = wrap switch
        {
            TextWrapping.Nowrap => TextWrap.Nowrap,
            TextWrapping.Balance => TextWrap.Balance,
            TextWrapping.Pretty => TextWrap.Pretty,
            _ => TextWrap.Wrap,
        },
    });

    // 单行省略要求元素有宽度约束：块级元素或 flex 子项（flex 子项默认不收缩到比内容窄，补 min-width: 0）
    static Style Truncate => Css.Define(static () => new Style
    {
        MinWidth = 0,
        Overflow = "hidden",
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    // 标准的 line-clamp 还没普及，用 -webkit-box 的写法（各浏览器都支持）
    static Style Clamp(int lines) => Css.Define(lines, static lines => new Style
    {
        Display = "-webkit-box",
        WebkitBoxOrient = "vertical",
        WebkitLineClamp = lines,
        Overflow = "hidden",
    });

    static Style Numeric => Css.Define(static () => new Style { FontVariantNumeric = "tabular-nums" });

    static Style Mono => Css.Define(static () => new Style { FontFamily = Fonts.Mono });

    // 键帽：底边加厚，像一个按下去的键
    static Style KbdRoot => Css.Define(static () => new Style
    {
        Display = Display.InlineBlock,
        MinWidth = "1.6em",
        PaddingInline = Space.Xs,
        BorderWidth = BorderWidths.Xs,
        BorderBottomWidth = BorderWidths.Sm,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Sm,
        BackgroundColor = Colors.Surface,
        Color = Colors.Text,
        FontFamily = Fonts.Mono,
        FontSize = "0.85em",
        LineHeight = 1.4,
        TextAlign = TextAlign.Center,
        WhiteSpace = WhiteSpace.Nowrap,
    });
}
