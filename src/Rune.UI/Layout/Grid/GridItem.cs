using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>
/// Grid 里的一项（div）：跨列、跨行。<c>&lt;GridItem Span="2" RowSpan="2"&gt;…&lt;/GridItem&gt;</c>、<c>&lt;GridItem FullRow="true"&gt;</c>。
/// 不跨的单元格不需要它，直接放子元素。按宽度自动换列的网格窄到放不下时跨当前的列数（JS 初始化器量网格实际有几列）。
/// </summary>
public class GridItem : UIComponent
{
    Memo<int, Attrs> managed;

    /// <summary>跨几列。</summary>
    [Parameter] public int? Span { get; set; }

    /// <summary>跨几行。</summary>
    [Parameter] public int? RowSpan { get; set; }

    /// <summary>占满一整行（从第一列到最后一列），优先于 <see cref="Span"/>。</summary>
    [Parameter] public bool FullRow { get; set; }

    /// <summary>单元格内容。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>元素名。</summary>
    protected virtual string Tag => "div";

    // 跨多列时把列数写在属性上：网格的列比它少时 JS 改成跨当前的列数
    Attrs? Managed => !FullRow && Span is > 1 and var span
        ? managed.Get(span, static span => new Attrs { ["data-rune-grid-span"] = span.ToString(CultureInfo.InvariantCulture) })
        : null;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        var span = FullRow ? int.MaxValue : Span;
        builder.AddMultipleAttributes(1, Attrs.Merge(LayoutStyles.GridItem(span, RowSpan, Config.Styles)[LayoutPart.Root], Rest, Managed)!);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}

/// <summary>GridList 里的一项（li）：跨列、跨行。</summary>
public sealed class GridListItem : GridItem
{
    /// <inheritdoc/>
    protected override string Tag => "li";
}
