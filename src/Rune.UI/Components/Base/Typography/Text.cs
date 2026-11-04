namespace Rune.UI;

/// <summary>TextSpan、Paragraph、Strong、Em、Small 的基类：用文字的配方。</summary>
public abstract class TextBase : TypographyBase
{
    /// <inheritdoc/>
    protected override Attrs RootLayer => TypographyStyles.Text(Appearance, Config.Styles)[TextPart.Root];
}

/// <summary>
/// 行内文字（span）：<c>&lt;TextSpan Tone="TextTone.Muted" Size="Size.Sm"&gt;更新于 3 分钟前&lt;/TextSpan&gt;</c>、
/// <c>&lt;TextSpan Mono="true"&gt;PO-2026-0187&lt;/TextSpan&gt;</c>。
/// 不叫 Text：Razor 在代码块里把 &lt;text&gt; 当作自己的标记（不区分大小写），@foreach 里写不了。
/// </summary>
public sealed class TextSpan : TextBase
{
    /// <inheritdoc/>
    protected override string Tag => "span";

    /// <inheritdoc/>
    protected override TypographyKind Kind => TypographyKind.Inline;
}

/// <summary>段落（p）：去掉浏览器默认的外边距（间距交给布局），默认避免最后一行只剩一个字。</summary>
public sealed class Paragraph : TextBase
{
    /// <inheritdoc/>
    protected override string Tag => "p";

    /// <inheritdoc/>
    protected override TypographyKind Kind => TypographyKind.Paragraph;
}

/// <summary>重要的内容（strong）：读屏可能加重语气；默认字重 Semibold。</summary>
public sealed class Strong : TextBase
{
    /// <inheritdoc/>
    protected override string Tag => "strong";

    /// <inheritdoc/>
    protected override TypographyKind Kind => TypographyKind.Inline;

    /// <inheritdoc/>
    protected override Weight? DefaultWeight => UI.Weight.Semibold;
}

/// <summary>语气上的强调（em）：浏览器默认斜体。</summary>
public sealed class Em : TextBase
{
    /// <inheritdoc/>
    protected override string Tag => "em";

    /// <inheritdoc/>
    protected override TypographyKind Kind => TypographyKind.Inline;
}

/// <summary>附注、版权、细则（small）：默认字号 Xs。</summary>
public sealed class Small : TextBase
{
    /// <inheritdoc/>
    protected override string Tag => "small";

    /// <inheritdoc/>
    protected override TypographyKind Kind => TypographyKind.Inline;

    /// <inheritdoc/>
    protected override Size? DefaultSize => UI.Size.Xs;
}
