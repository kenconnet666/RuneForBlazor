using Rune.Styles;

namespace Rune.UI;

/// <summary>切换按钮组的部件（项是 ToggleItem，外观来自按钮家族）。</summary>
public enum ToggleGroupPart
{
    /// <summary>组（div，role="group"）：浅色底，项排在里面。</summary>
    Root,

    /// <summary>单选时选中项下面的滑块：切换时滑过去。</summary>
    Indicator,
}

/// <summary>切换按钮组交给组内各项的状态（经级联值）：每次渲染一个新实例，项按它重新渲染。</summary>
/// <param name="Selected">按下（选中）的项的值。</param>
/// <param name="Size">字号档位；不写时继承。</param>
/// <param name="Disabled">整组不可用。</param>
/// <param name="ReadOnly">整组只读：按钮照常显示与聚焦，点了不切换。</param>
/// <param name="Ripple">波纹；不写时沿用配置。</param>
/// <param name="Sliding">单选：选中底色由组里的滑块画，项本身不画底色。</param>
/// <param name="Toggle">切换一项（单选、多选的规则由组决定）。</param>
public sealed record ToggleGroupState(
    IReadOnlyList<string> Selected,
    Size? Size,
    bool Disabled,
    bool ReadOnly,
    bool? Ripple,
    bool Sliding,
    Func<string, Task> Toggle);

/// <summary>切换按钮组的样式（同 vue-tsx-pro 的 toggleGroupStyles）。</summary>
public static class ToggleGroupStyles
{
    /// <summary>某个方向、某套定制下各部件的默认层。</summary>
    public static PartLayers<ToggleGroupPart> Of(Orientation orientation, ComponentStyles styles) =>
        Recipe.Of("toggle-group", orientation, styles, static (Orientation orientation, ToggleGroupPart part) => part switch
        {
            ToggleGroupPart.Root => Sx.Of(Root, orientation == Orientation.Vertical ? Vertical : null),
            _ => Indicator,
        });

    /// <summary>只读、没选中的项：不像能点（悬停也不出底色）。</summary>
    internal static Style ReadOnlyItem => Css.Define(static () => new Style
    {
        Cursor = Cursor.Default,
        BackgroundColor = CssColors.Transparent,
        [When.Hover] = new() { BackgroundColor = CssColors.Transparent },
    });

    // 滑块按项的 offsetLeft / offsetTop 定位；isolation 让 z-index: -1 的滑块压在项下、组的底色之上
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Isolation = Isolation.Isolate,
        Display = Display.InlineFlex,
        // 放在竖排的弹性容器里（Field 的控件列）时不被拉满整行
        Width = Width.FitContent,
        Gap = Space.Xs,
        Padding = Space.Xs,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Fill,
    });

    static Style Vertical => Css.Define(static () => new Style { FlexDirection = FlexDirection.Column });

    // 同按钮的 surface 外观；位置与宽高由 JS 写（data-rune-indicator），切换时按主题时长滑过去
    static Style Indicator => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = 0,
        Left = 0,
        ZIndex = -1,
        BoxSizing = BoxSizing.BorderBox,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Tones.Accent.Border,
        BorderRadius = Radii.Sm,
        BackgroundColor = Tones.Accent.Soft,
        PointerEvents = PointerEvents.None,
        TransitionProperty = "transform, width, height",
        TransitionDuration = Durations.Md,
        TransitionTimingFunction = Easings.Standard,
    });
}
