using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 二维布局（div，display: grid）。
/// <code>
/// &lt;Grid Columns="3" Gap="SpaceScale.Md"&gt;…&lt;/Grid&gt;                 固定列数，等宽
/// &lt;Grid MinItemWidth="@("14em")" Gap="SpaceScale.Md"&gt;…&lt;/Grid&gt;   按宽度自动换列：容器多宽就放几列，不需要断点
/// &lt;Grid MinItemWidth="@("14em")" Balanced="true"&gt;…&lt;/Grid&gt;        同上，最后一行最多空一格（4 项排 2 + 2，不排 3 + 1）
/// </code>
/// 跨列、跨行用 GridItem。响应式不引入断点：MinItemWidth 覆盖最常见的自动换列，其余用 Sx 加媒体查询。
/// 卡片列表这类需要列表语义的用 <see cref="GridList"/>。
/// </summary>
public class Grid : UIComponent
{
    static readonly Attrs listRole = new() { Role = "list" };

    Memo<(CssLength Width, bool List, bool Balanced), Attrs> managed;

    /// <summary>等宽列数；与 <see cref="MinItemWidth"/> 同时给时以后者为准。</summary>
    [Parameter] public int? Columns { get; set; }

    /// <summary>每项的最小宽度：按容器宽度自动决定列数，窄于它时单列。项目比放得下的列数少时各项拉宽占满，不在右边留空。</summary>
    [Parameter] public CssLength? MinItemWidth { get; set; }

    /// <summary>
    /// 和 <see cref="MinItemWidth"/> 一起用：按项数取列数，最后一行最多空一格——4 项放得下 3 列时排成 2 + 2，6 项放得下 4 列时排成 3 + 3，
    /// 不落单（一排指标卡、一组入口）。浏览器量出宽度之前（预渲染）照 MinItemWidth 自动换列；最小宽度只认 px、rem、em。
    /// </summary>
    [Parameter] public bool Balanced { get; set; }

    /// <summary>间隙档位。</summary>
    [Parameter] public SpaceScale? Gap { get; set; }

    /// <summary>行间隙，盖过 <see cref="Gap"/>。</summary>
    [Parameter] public SpaceScale? RowGap { get; set; }

    /// <summary>列间隙，盖过 <see cref="Gap"/>。</summary>
    [Parameter] public SpaceScale? ColumnGap { get; set; }

    /// <summary>单元格在块方向的对齐（align-items），默认拉伸。</summary>
    [Parameter] public ItemAlign? Align { get; set; }

    /// <summary>单元格在行内方向的对齐（justify-items），默认拉伸。</summary>
    [Parameter] public ItemAlign? Justify { get; set; }

    /// <summary>自动排布方向（grid-auto-flow）；Dense 回填空位。</summary>
    [Parameter] public GridFlow? Flow { get; set; }

    /// <summary>单元格。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>元素名。</summary>
    protected virtual string Tag => "div";

    /// <summary>是不是列表（ul）：去掉圆点与缩进，补上 role="list"（Safari 去掉圆点后不再报“列表”）。</summary>
    protected virtual bool IsList => false;

    GridAppearance Appearance => new(Columns, MinItemWidth is not null, Gap, RowGap, ColumnGap, Align, Justify, Flow, IsList);

    // 最小宽度经 CSS 变量写在元素上（类不随宽度变化）；宽度不变时复用同一个对象
    Attrs? Managed => MinItemWidth is { } width
        ? managed.Get((width, IsList, Balanced), static k => new Attrs
        {
            Vars = [LayoutStyles.MinItemWidth.Set(k.Width)],
            Role = k.List ? "list" : null,
            ["data-rune-balanced"] = k.Balanced ? true : null,
        })
        : IsList ? listRole : null;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, Attrs.Merge(LayoutStyles.Grid(Appearance, Config.Styles)[LayoutPart.Root], Rest, Managed)!);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}

/// <summary>网格布局的列表（ul）：卡片列表等，去掉圆点与缩进，保留列表语义（读屏念“列表，6 项”）；子元素用 li 或 GridListItem。</summary>
public sealed class GridList : Grid
{
    /// <inheritdoc/>
    protected override string Tag => "ul";

    /// <inheritdoc/>
    protected override bool IsList => true;
}
