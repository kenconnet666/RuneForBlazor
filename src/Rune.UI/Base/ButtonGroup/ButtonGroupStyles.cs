using Rune.Styles;

namespace Rune.UI;

/// <summary>按钮组的部件。</summary>
public enum ButtonGroupPart
{
    /// <summary>组（div，role="group"）。</summary>
    Root,
}

/// <summary>按钮组交给组里按钮家族的外观（级联值）：按钮自己没写的项用它。</summary>
/// <param name="Variant">样式。</param>
/// <param name="Tone">色调。</param>
/// <param name="Size">字号档位。</param>
/// <param name="Radius">圆角。</param>
public sealed record ButtonGroupAppearance(ButtonVariant? Variant, Tone? Tone, Size? Size, Radius? Radius) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Variant, Tone, Size, Radius];
}

/// <summary>按钮组的样式（同 vue-tsx-pro 的 buttonGroupStyles）。</summary>
public static class ButtonGroupStyles
{
    /// <summary>某个方向、某套定制下的部件默认层。</summary>
    public static PartLayers<ButtonGroupPart> Of(Orientation orientation, ComponentStyles styles) =>
        Recipe.Of("button-group", orientation, styles, static (Orientation orientation, ButtonGroupPart _) =>
            Sx.Of(Root, orientation == Orientation.Vertical ? Vertical : null));

    static Style Root => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Stretch });

    static Style Vertical => Css.Define(static () => new Style { FlexDirection = FlexDirection.Column });
}
