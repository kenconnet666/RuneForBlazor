using Rune.Styles;

namespace Rune.UI;

/// <summary>组合输入的部件。</summary>
public enum ControlGroupPart
{
    /// <summary>组（div，role="group"）。</summary>
    Root,
}

/// <summary>组合输入的样式（同 vue-tsx-pro 的 controlGroupStyles）。</summary>
public static class ControlGroupStyles
{
    /// <summary>某个方向、是否行内、某套定制下的部件默认层。</summary>
    public static PartLayers<ControlGroupPart> Of(Orientation orientation, bool inline, ComponentStyles styles) =>
        Recipe.Of("control-group", (orientation, inline), styles, static ((Orientation Orientation, bool Inline) k, ControlGroupPart _) =>
            Sx.Of(Root, k.Inline ? InlineRoot : null, k.Orientation == Orientation.Vertical ? Vertical : null));

    // 输入框外壳分掉剩余宽度；按钮、选择框按内容宽（见 InputStyles 的相连档位）
    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Stretch, Width = CssLength.Percent(100) });

    static Style InlineRoot => Css.Define(static () => new Style { Display = Display.InlineFlex, Width = CssLength.Auto });

    static Style Vertical => Css.Define(static () => new Style { FlexDirection = FlexDirection.Column });
}
