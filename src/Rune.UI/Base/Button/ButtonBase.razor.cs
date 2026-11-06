using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 按钮家族的基类：外观参数（样式、色调、尺寸、圆角）、加载中、图标与各部件属性。
/// 外观按“组件上写的参数 &gt; 所在 ButtonGroup 写的 &gt; 配置的默认值（<see cref="ButtonDefaults"/>）&gt; 各组件自己的默认值”决定。
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

    /// <summary>按下时的波纹；不写时沿用配置（<see cref="UIConfig.Ripple"/>，默认开启）。</summary>
    [Parameter] public bool? Ripple { get; set; }

    /// <summary>文字前的图标：<c>Lucide.Plus</c>。</summary>
    [Parameter] public IconData? Icon { get; set; }

    /// <summary>文字后的图标。</summary>
    [Parameter] public IconData? IconEnd { get; set; }

    /// <summary>内容层（包着图标与文字的 span）的部件属性。</summary>
    [Parameter] public Attrs? PropsContent { get; set; }

    /// <summary>前置图标的部件属性（交给 Icon 组件）。</summary>
    [Parameter] public Attrs? PropsIcon { get; set; }

    /// <summary>按钮元素（button 或 a），交互渲染之后才有值；用 <see cref="FocusAsync"/> 聚焦。</summary>
    public ElementReference? Element { get; protected set; }

    /// <summary>把焦点移到按钮上；静态渲染（没有交互）时什么也不做。</summary>
    /// <param name="preventScroll">不把它滚动到可见范围。</param>
    public ValueTask FocusAsync(bool preventScroll = false) =>
        RendererInfo.IsInteractive && Element is { } element ? element.FocusAsync(preventScroll) : ValueTask.CompletedTask;

    /// <summary>后置图标的部件属性。</summary>
    [Parameter] public Attrs? PropsIconEnd { get; set; }

    /// <summary>加载指示的部件属性（交给 Spinner 组件）。</summary>
    [Parameter] public Attrs? PropsSpinner { get; set; }

    /// <summary>这个组件自己的默认样式（Button 是实底，IconButton 是无底）。</summary>
    protected abstract ButtonVariant DefaultVariant { get; }

    /// <summary>这个组件自己的默认色调。</summary>
    protected abstract Tone DefaultTone { get; }

    /// <summary>文字后实际显示的图标（外部链接没写 IconEnd 时是外链箭头）。</summary>
    protected virtual IconData? TrailingIcon => IconEnd;

    /// <summary>当前生效的外观。</summary>
    protected ButtonAppearance Appearance(ButtonShape shape)
    {
        var defaults = Config.Defaults.Get<ButtonDefaults>();
        return new ButtonAppearance(
            EffectiveVariant,
            // 组里的按钮外观要一致：不论有没有文字都按 Button 的默认色调（拆分按钮的两半同色）
            Tone ?? GroupAppearance?.Tone ?? defaults?.Tone ?? (GroupAppearance is null ? DefaultTone : UI.Tone.Accent),
            Size ?? GroupAppearance?.Size ?? defaults?.Size,
            Radius ?? GroupAppearance?.Radius ?? defaults?.Radius ?? UI.Radius.Md,
            shape,
            Attached?.Orientation);
    }

    ButtonVariant EffectiveVariant => Variant ?? GroupAppearance?.Variant ?? Config.Defaults.Get<ButtonDefaults>()?.Variant ?? DefaultVariant;

    /// <summary>所在的相连组（ControlGroup、ButtonGroup）：去掉相邻一侧的圆角、边框叠在一起。</summary>
    [CascadingParameter] AttachedGroup? Attached { get; set; }

    /// <summary>所在的 ButtonGroup 写的外观：自己没写的项用它。</summary>
    [CascadingParameter] ButtonGroupAppearance? GroupAppearance { get; set; }

    /// <summary>所在的 Form（提交中时它的提交按钮显示加载中）。</summary>
    [CascadingParameter] FormState? Form { get; set; }

    /// <summary>这个按钮是不是表单的提交按钮（Button 的 Type 为 submit）。</summary>
    protected virtual bool SubmitsForm => false;

    /// <summary>当前是否加载中：写了 <see cref="Loading"/>，或是所在 Form 正在提交时的提交按钮。</summary>
    protected bool IsLoading => Loading || IsBusy || (SubmitsForm && Form?.Submitting == true);

    /// <summary>组件自己的忙碌状态（复制按钮异步取值时），和 <see cref="Loading"/> 一样显示加载指示。</summary>
    protected virtual bool IsBusy => false;

    /// <summary>是否出波纹：组件上写的 &gt; 配置；像链接的按钮（Link 样式）没有波纹。</summary>
    protected bool RippleEnabled => (Ripple ?? Config.Ripple) && EffectiveVariant != ButtonVariant.Link;

    /// <summary>各部件的默认层。</summary>
    protected PartLayers<ButtonPart> Parts(ButtonShape shape) => ButtonStyles.Of(Appearance(shape), Config.Styles);

    /// <summary>
    /// 使用方的属性：不可交互（加载中、不可用）时去掉点击处理，
    /// 加载中的按钮仍可聚焦（用 aria-disabled 而不是 disabled），点击不能触发使用方的处理。
    /// </summary>
    protected Attrs UserAttrs(bool interactive) => interactive ? Rest : Rest.Without("onclick");

    /// <summary>
    /// 按钮家族的状态属性（同 vue-tsx-pro 的 useButtonBehavior）：不可用分原生 disabled 与可聚焦的 aria-disabled；
    /// 加载中只写 aria-busy，外观不变灰。加载中、可聚焦的不可用带 data-rune-inert：JS 初始化器拦下点击的默认动作（提交表单），
    /// 使用方的点击处理由 <see cref="UserAttrs"/> 去掉。
    /// </summary>
    /// <param name="disabled">不可用。</param>
    /// <param name="focusable">不可用时仍可聚焦（链接没有原生 disabled，总是 true）。</param>
    /// <param name="loading">加载中。</param>
    /// <param name="ripple">按下时出波纹。</param>
    protected static Attrs StateAttrs(bool disabled, bool focusable, bool loading, bool ripple) => new()
    {
        ["disabled"] = disabled && !focusable ? true : null,
        AriaDisabled = disabled && focusable ? true : null,
        ["aria-busy"] = loading ? "true" : null,
        ["data-disabled"] = disabled ? true : null,
        ["data-loading"] = loading ? true : null,
        ["data-rune-inert"] = loading || (disabled && focusable) ? true : null,
        ["data-rune-ripple"] = ripple ? true : null,
    };
}
