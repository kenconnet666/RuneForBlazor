using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>
/// 文字组件（TextSpan、Paragraph、Strong、Em、Small、H1–H6）的基类：一个元素一个组件（语义不同的元素不靠参数切换标签），
/// 参数相同、样式由同一个配方生成。没写的参数都继承上下文。
/// </summary>
public abstract class TypographyBase : UIComponent
{
    /// <summary>字号档位；不写时继承（标题按级别有默认值）。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>字重；不写时继承（标题默认 Semibold）。</summary>
    [Parameter] public Weight? Weight { get; set; }

    /// <summary>色调（文字色）：Muted 是次要说明，其余是正文色、品牌色与提示色的文字色；不写时继承。</summary>
    [Parameter] public TextTone? Tone { get; set; }

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
        new(Kind, Size ?? DefaultSize, Weight ?? DefaultWeight, Tone, Align, Wrap, Truncate, LineClamp, Numeric, Mono);

    /// <summary>根元素的默认层（文字与标题用各自的配方，按部件定制互不影响）。</summary>
    protected abstract Attrs RootLayer { get; }

    /// <summary>文字之后的内容（标题的锚点链接）；序号从 10 开始。</summary>
    protected virtual void BuildTrailing(RenderTreeBuilder builder)
    {
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, Attrs.Merge(RootLayer, Rest)!);
        builder.AddContent(2, ChildContent);
        BuildTrailing(builder);
        builder.CloseElement();
    }
}
