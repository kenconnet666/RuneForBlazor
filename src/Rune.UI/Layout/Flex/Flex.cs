using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>
/// 一维布局（div，display: flex）。不另做 Stack：竖排写 <c>Direction="LayoutDirection.Column"</c>。
/// <c>&lt;Flex Gap="SpaceScale.Sm" Align="ItemAlign.Center"&gt;…&lt;/Flex&gt;</c>
/// 需要列表语义（标签、互动数据）时用 <see cref="FlexList"/>；其他语义容器（nav、section）在外面包。
/// </summary>
public class Flex : UIComponent
{
    static readonly Attrs listRole = new() { Role = "list" };

    /// <summary>主轴方向，默认横排。</summary>
    [Parameter] public LayoutDirection? Direction { get; set; }

    /// <summary>交叉轴对齐（align-items），默认拉伸。</summary>
    [Parameter] public ItemAlign? Align { get; set; }

    /// <summary>主轴分布（justify-content），默认靠起点。</summary>
    [Parameter] public ContentJustify? Justify { get; set; }

    /// <summary>放不下时换行。</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>间隙档位。</summary>
    [Parameter] public SpaceScale? Gap { get; set; }

    /// <summary>换行时行与行之间的间隙，盖过 <see cref="Gap"/>。</summary>
    [Parameter] public SpaceScale? RowGap { get; set; }

    /// <summary>同一行里项与项之间的间隙，盖过 <see cref="Gap"/>。</summary>
    [Parameter] public SpaceScale? ColumnGap { get; set; }

    /// <summary>行内（inline-flex）：和文字排在一行。</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>在父级的 flex 布局里占满剩余空间，并允许收缩到比内容窄（里面的文字才能截断）。</summary>
    [Parameter] public bool Grow { get; set; }

    /// <summary>子元素。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>元素名。</summary>
    protected virtual string Tag => "div";

    /// <summary>是不是列表（ul）：去掉圆点与缩进，补上 role="list"。</summary>
    protected virtual bool IsList => false;

    FlexAppearance Appearance => new(Direction, Align, Justify, Wrap, Gap, RowGap, ColumnGap, Inline, Grow, IsList);

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, Attrs.Merge(LayoutStyles.Flex(Appearance, Config.Styles)[LayoutPart.Root], Rest, IsList ? listRole : null)!);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}

/// <summary>flex 布局的列表（ul）：一行标签、互动数据等，去掉圆点与缩进，保留列表语义；子元素用 li。</summary>
public sealed class FlexList : Flex
{
    /// <inheritdoc/>
    protected override string Tag => "ul";

    /// <inheritdoc/>
    protected override bool IsList => true;
}

/// <summary>flex 布局的有序列表（ol，同 vue-tsx-pro 的 FlexOrderedList）：顺序有意义的一行（面包屑、步骤），去掉序号与缩进，保留列表语义；子元素用 li。</summary>
public sealed class FlexOrderedList : Flex
{
    /// <inheritdoc/>
    protected override string Tag => "ol";

    /// <inheritdoc/>
    protected override bool IsList => true;
}
