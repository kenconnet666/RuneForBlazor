using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 按钮家族的基类：外观参数（样式、色调、尺寸、圆角）、加载中、图标与各部件属性。
/// 外观按“组件上写的参数 &gt; 配置的默认值（<see cref="ButtonDefaults"/>）&gt; 各组件自己的默认值”决定。
/// </summary>
public abstract partial class ButtonBase : UIComponent
{
    /// <summary>样式；不写时取配置的默认值，再没有就是组件自己的默认值。</summary>
    [Parameter] public ButtonVariant? Variant { get; set; }

    /// <summary>色调，与 Badge、Alert 同一套。</summary>
    [Parameter] public Tone? Tone { get; set; }

    /// <summary>字号档位，内部尺寸随之缩放；不写时继承上下文字号。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>圆角，默认中圆角。</summary>
    [Parameter] public Radius? Radius { get; set; }

    /// <summary>加载中：显示旋转指示，宽度不变，点击无效，焦点保留；读屏会得知 aria-busy。</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>文字前的图标：<c>Lucide.Plus</c>。</summary>
    [Parameter] public IconData? Icon { get; set; }

    /// <summary>文字后的图标。</summary>
    [Parameter] public IconData? IconEnd { get; set; }

    /// <summary>内容层（包着图标与文字的 span）的部件属性。</summary>
    [Parameter] public Attrs? PropsContent { get; set; }

    /// <summary>前置图标的部件属性（交给 Icon 组件）。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>后置图标的部件属性。</summary>
    [Parameter] public Attrs? PropsIconEnd { get; set; }

    /// <summary>加载指示的部件属性（交给 Spinner 组件）。</summary>
    [Parameter] public Attrs? PropsSpinner { get; set; }

    /// <summary>这个组件自己的默认样式（Button 是实底，IconButton 是无底）。</summary>
    protected abstract ButtonVariant DefaultVariant { get; }

    /// <summary>这个组件自己的默认色调。</summary>
    protected abstract Tone DefaultTone { get; }

    /// <summary>当前生效的外观。</summary>
    protected ButtonAppearance Appearance(ButtonShape shape)
    {
        var defaults = Config.Defaults.Get<ButtonDefaults>();
        return new ButtonAppearance(
            Variant ?? defaults?.Variant ?? DefaultVariant,
            Tone ?? defaults?.Tone ?? DefaultTone,
            Size ?? defaults?.Size,
            Radius ?? defaults?.Radius ?? UI.Radius.Md,
            shape);
    }

    /// <summary>各部件的默认层。</summary>
    protected PartLayers<ButtonPart> Parts(ButtonShape shape) => ButtonStyles.Of(Appearance(shape), Config.Styles);

    /// <summary>
    /// 使用方的属性：不可交互（加载中、不可用）时去掉点击处理，
    /// 加载中的按钮仍可聚焦（用 aria-disabled 而不是 disabled），点击不能触发使用方的处理。
    /// </summary>
    protected Attrs UserAttrs(bool interactive) => interactive ? Rest : Rest.Without("onclick");
}
