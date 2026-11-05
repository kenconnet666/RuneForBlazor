using Rune.Styles;

namespace Rune.UI;

/// <summary>宽度调整手柄的部件。</summary>
public enum ResizeHandlePart
{
    /// <summary>手柄（span，role="separator"）。</summary>
    Root,
}

/// <summary>手柄在父元素的哪一边。</summary>
public enum ResizeEdge
{
    /// <summary>结束边（默认）；横向时是下边。</summary>
    End,

    /// <summary>起始边：往起始方向拖是变宽（从结束边滑入的抽屉）；横向时是上边。</summary>
    Start,
}

/// <summary>宽度调整手柄的样式（同 vue-tsx-pro 的 resizeHandleStyles）：贴着父元素一边的细线，悬停、聚焦时显示。</summary>
public static class ResizeHandleStyles
{
    /// <summary>某个方向、某一边、某套定制下的默认层。</summary>
    public static PartLayers<ResizeHandlePart> Of(Orientation orientation, ResizeEdge edge, ComponentStyles styles) =>
        Recipe.Of("resize-handle", (orientation, edge), styles, static ((Orientation Orientation, ResizeEdge Edge) a, ResizeHandlePart _) =>
            a.Orientation == Orientation.Horizontal
                ? Sx.Of(Handle, Horizontal, a.Edge == ResizeEdge.Start ? HorizontalStart : null)
                : Sx.Of(Handle, a.Edge == ResizeEdge.Start ? Start : null));

    static Style Handle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = 0,
        Bottom = 0,
        InsetInlineEnd = 0,
        Width = Space.Sm,
        Cursor = Cursor.ColResize,
        TouchAction = TouchAction.None,
        BackgroundColor = CssColors.Transparent,
        OutlineStyle = OutlineStyle.None,
        [When.Hover] = new() { BackgroundColor = Colors.BorderStrong },
        [When.FocusVisible] = new() { BackgroundColor = Tones.Accent.Solid },
    });

    static Style Start => Css.Define(static () => new Style { InsetInlineEnd = CssLength.Auto, InsetInlineStart = 0 });

    // 横线：贴着父元素的下边（Start 时是上边），调整高度
    static Style Horizontal => Css.Define(static () => new Style
    {
        Top = CssLength.Auto,
        Bottom = 0,
        InsetInlineStart = 0,
        InsetInlineEnd = 0,
        Width = CssLength.Auto,
        Height = Space.Sm,
        Cursor = Cursor.RowResize,
    });

    static Style HorizontalStart => Css.Define(static () => new Style { Top = 0, Bottom = CssLength.Auto });
}
