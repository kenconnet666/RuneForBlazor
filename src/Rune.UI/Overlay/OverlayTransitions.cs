using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 顶层元素（原生 popover、模态 dialog）的进出场过渡，纯 CSS（同 vue-tsx-pro 的 transitions）：
/// <list type="bullet">
/// <item><description>进场：打开状态（<c>:popover-open</c> / <c>:modal</c>）里用 <c>@starting-style</c> 给出起始值，enter 缓动；</description></item>
/// <item><description>离场：display 与 overlay 加 allow-discrete，关闭后先播完过渡再离开顶层，exit 缓动、比进场短一档。</description></item>
/// </list>
/// 原生弹层被 Esc、点外面关闭时 JS 来不及介入，所以不用 JS 动画。离场依赖 CSS 的 overlay（目前只有 Chromium 支持），
/// 其他浏览器关闭后立即隐藏：渐进增强，状态不受影响。用户要求减少动态效果时时长 token 为 0，不用另写。
/// 用法：放进浮层根部件的 Sx：<c>Sx.Of(Surface, OverlayTransitions.Scale)</c>；模态的再加 <see cref="Backdrop"/>。
/// </summary>
public static class OverlayTransitions
{
    /// <summary>淡入并从略小放大到原尺寸，进场 Md、离场 Sm：对话框。</summary>
    public static Style Scale => Css.Define(static () => Zoom(Durations.Md, Durations.Sm));

    /// <summary>
    /// 同 <see cref="Scale"/> 但更快，进场 Sm、离场 Xs（同 vue-tsx-pro 的 transitions.pop）：锚定的小浮层（菜单、弹出框、选项列表）。
    /// 跟着点击出现的小面板慢了会显得拖沓；对话框面积大、打断当前操作，用 Scale。
    /// </summary>
    public static Style Pop => Css.Define(static () => Zoom(Durations.Sm, Durations.Xs));

    /// <summary>淡入淡出（同 vue-tsx-pro 的 transitions.fade）：提示。</summary>
    public static Style Fade => Css.Define(static () => new Style
    {
        Opacity = 0,
        Transition = Exit("opacity"),
        [opened] = new()
        {
            Opacity = 1,
            Transition = Enter("opacity"),
            [When.Starting] = new() { Opacity = 0 },
        },
    });

    /// <summary>模态的遮罩：打开时变暗，关闭时淡出。</summary>
    public static Style Backdrop => Css.Define(static () => new Style
    {
        [Pseudo.Backdrop] = new()
        {
            BackgroundColor = Transparent,
            Transition = Exit("background-color"),
        },
        [opened] = new()
        {
            [Pseudo.Backdrop] = new()
            {
                BackgroundColor = Colors.Overlay,
                Transition = Enter("background-color"),
                [When.Starting] = new() { BackgroundColor = Transparent },
            },
        },
    });

    // 打开状态：弹出的 popover 或模态打开的 dialog
    static readonly Condition opened = new(":is(:popover-open, :modal)");

    const string Transparent = "rgb(0 0 0 / 0%)";

    static Style Zoom(CssValue enter, CssValue exit) => new()
    {
        Opacity = 0,
        Scale = 0.96,
        Transition = Transition(["opacity", "scale"], exit, Easings.Exit),
        [opened] = new()
        {
            Opacity = 1,
            Scale = 1,
            Transition = Transition(["opacity", "scale"], enter, Easings.Enter),
            [When.Starting] = new() { Opacity = 0, Scale = 0.96 },
        },
    };

    static string Enter(params string[] properties) => Transition(properties, Durations.Md, Easings.Enter);

    static string Exit(params string[] properties) => Transition(properties, Durations.Sm, Easings.Exit);

    // 每个属性一段，再加上 display / overlay 的离散过渡（离场时等动画播完再隐藏）
    static string Transition(string[] properties, CssValue duration, CssValue easing) =>
        string.Join(", ", properties.Select(p => $"{p} {duration} {easing}")
            .Append($"display {duration} allow-discrete")
            .Append($"overlay {duration} allow-discrete"));
}
