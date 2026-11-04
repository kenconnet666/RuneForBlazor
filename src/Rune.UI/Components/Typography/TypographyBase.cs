using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>
/// 文字组件（TextSpan、Paragraph、H1–H6）的基类：一个元素一个组件（语义不同的元素不靠参数切换标签），
/// 参数相同、样式由同一个配方生成。没写的参数都继承上下文。
/// </summary>
public abstract class TypographyBase : UIComponent
{
    /// <summary>字号档位；不写时继承（标题按级别有默认值）。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>字重；不写时继承（标题默认 Semibold）。</summary>
    [Parameter] public Weight? Weight { get; set; }

    /// <summary>色调（文字色）；不写时继承。</summary>
    [Parameter] public Tone? Tone { get; set; }

    /// <summary>次要文字（说明、时间），优先于 <see cref="Tone"/>。</summary>
    [Parameter] public bool Muted { get; set; }

    /// <summary>水平对齐（只在块级元素上有效）。</summary>
    [Parameter] public TextAlignment? Align { get; set; }

    /// <summary>换行方式；段落默认 Pretty，标题默认 Balance。</summary>
    [Parameter] public TextWrapping? Wrap { get; set; }

    /// <summary>单行省略（在块级元素或 flex 子项上生效）。</summary>
    [Parameter] public bool Truncate { get; set; }

    /// <summary>最多显示几行，超出省略。</summary>
    [Parameter] public int? LineClamp { get; set; }

    /// <summary>数字等宽（tabular-nums）：金额、计数、日期上下对齐，变化时不跳动。</summary>
    [Parameter] public bool Numeric { get; set; }

    /// <summary>等宽字体：编号、件号、代码片段。</summary>
    [Parameter] public bool Mono { get; set; }

    /// <summary>文字内容。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>元素名。</summary>
    protected abstract string Tag { get; }

    /// <summary>元素的种类。</summary>
    protected abstract TypographyKind Kind { get; }

    /// <summary>没写 <see cref="Size"/> 时的字号。</summary>
    protected virtual Size? DefaultSize => null;

    /// <summary>没写 <see cref="Weight"/> 时的字重。</summary>
    protected virtual Weight? DefaultWeight => null;

    /// <summary>当前的外观。</summary>
    protected TypographyAppearance Appearance =>
        new(Kind, Size ?? DefaultSize, Weight ?? DefaultWeight, Tone, Muted, Align, Wrap, Truncate, LineClamp, Numeric, Mono);

    /// <summary>根元素的默认层（文字与标题用各自的配方，按部件定制互不影响）。</summary>
    protected abstract Attrs RootLayer { get; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, Attrs.Merge(RootLayer, Rest)!);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}

/// <summary>TextSpan、Paragraph 的基类：用文字的配方。</summary>
public abstract class TextBase : TypographyBase
{
    /// <inheritdoc/>
    protected override Attrs RootLayer => TypographyStyles.Text(Appearance, Config.Styles)[TextPart.Root];
}

/// <summary>
/// 行内文字（span）：<c>&lt;TextSpan Muted="true" Size="Size.Sm"&gt;更新于 3 分钟前&lt;/TextSpan&gt;</c>、
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
