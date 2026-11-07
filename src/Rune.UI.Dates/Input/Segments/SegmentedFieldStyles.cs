using Rune.Styles;

namespace Rune.UI;

/// <summary>分段输入框的部件（外壳、图标同 TextField，见 <see cref="InputPart"/>）。</summary>
public enum SegmentedFieldPart
{
    /// <summary>一组段（div role="group"，主元素）。</summary>
    Group,

    /// <summary>一段（span role="spinbutton"）。</summary>
    Segment,

    /// <summary>分隔符（/、:、年），对读屏隐藏。</summary>
    Literal,

    /// <summary>清空按钮。</summary>
    Clear,
}

/// <summary>分段输入框的样式（同 vue-tsx-pro 的 dateSegmentStyles、segmentedFieldStyles）。</summary>
public static class SegmentedFieldStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<SegmentedFieldPart> Of(ComponentStyles styles) =>
        Recipe.Of("segmented-field", styles, static (SegmentedFieldPart part) => part switch
        {
            SegmentedFieldPart.Group => Group,
            SegmentedFieldPart.Segment => Segment,
            SegmentedFieldPart.Literal => Literal,
            _ => Clear,
        });

    // 段占满剩下的宽度：清空按钮、尾部内容贴着外壳的结束边
    static Style Group => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        FlexGrow = 1,
        MinWidth = 0,
        PaddingBlock = Space.Xs,
        WhiteSpace = WhiteSpace.Nowrap,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
    });

    static Style Segment => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.ContentBox,
        PaddingInline = CssLength.Em(0.1),
        BorderRadius = Radii.Xs,
        // 选中由整段高亮表示，不显示浏览器的插入符
        CaretColor = CssColors.Transparent,
        OutlineStyle = OutlineStyle.None,
        TextAlign = TextAlign.End,
        Cursor = Cursor.Text,
        // 占位文字是真正的文字（不是原生 placeholder），要满足 4.5:1：用 TextMuted
        [When.Attribute("[data-placeholder]")] = new() { Color = Colors.TextMuted },
        [When.Focus] = new() { BackgroundColor = Tones.Accent.Solid, Color = Tones.Accent.Contrast },
        [When.Attribute("[aria-disabled=\"true\"]")] = new() { Cursor = Cursor.NotAllowed },
    });

    static Style Literal => Css.Define(static () => new Style { Color = Colors.TextMuted, WhiteSpace = WhiteSpace.Pre });

    // 没有值时 JS 写上 hidden：按钮自己的 display 会盖过浏览器的默认
    static Style Clear => Css.Define(static () => new Style { [When.Attribute("[hidden]")] = new() { Display = Display.None } });
}
