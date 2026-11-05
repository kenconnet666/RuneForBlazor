using Rune.Styles;

namespace Rune.UI;

/// <summary>工具栏的部件。</summary>
public enum ToolbarPart
{
    /// <summary>外层（div，role="toolbar"）。</summary>
    Root,
}

/// <summary>工具栏交给里面的组件的信息：自带漫游焦点的组件（ToggleGroup）据此把焦点交给工具栏统一管理。</summary>
public sealed class ToolbarContext
{
    /// <summary>唯一的实例（级联值不变，里面的组件不随工具栏重新渲染）。</summary>
    public static readonly ToolbarContext Instance = new();

    ToolbarContext() { }
}

/// <summary>工具栏的样式（同 vue-tsx-pro 的 toolbarStyles）。</summary>
public static class ToolbarStyles
{
    /// <summary>某个方向、间距、某套定制下的默认层。</summary>
    public static PartLayers<ToolbarPart> Of(Orientation orientation, SpaceScale gap, ComponentStyles styles) =>
        Recipe.Of("toolbar", (orientation, gap), styles, static ((Orientation Orientation, SpaceScale Gap) a, ToolbarPart _) =>
            Sx.Of(Root, a.Orientation == Orientation.Vertical ? Vertical : null, ScaleStyles.Gap(a.Gap)));

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, FlexWrap = FlexWrap.Wrap });

    static Style Vertical => Css.Define(static () => new Style { FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Stretch });
}
