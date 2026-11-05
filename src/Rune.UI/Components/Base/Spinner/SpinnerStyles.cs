using Rune.Styles;

namespace Rune.UI;

/// <summary>加载指示的部件。</summary>
public enum SpinnerPart
{
    /// <summary>外层（span）：有说明时是 role="status"。</summary>
    Root,

    /// <summary>旋转的图标（Icon 组件）。</summary>
    Icon,
}

/// <summary>加载指示的样式：匀速旋转；减少动效时转得慢一些（仍要表达“进行中”）。</summary>
public static class SpinnerStyles
{
    /// <summary>某套定制下的部件默认层（大小由 Icon 的字号档位决定）。</summary>
    public static PartLayers<SpinnerPart> Of(ComponentStyles styles) =>
        Recipe.Of("spinner", styles, static (SpinnerPart part) => part == SpinnerPart.Root ? Root : Icon);

    static Keyframes Spin => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Transform = "rotate(0deg)" },
        ["to"] = new() { Transform = "rotate(360deg)" },
    });

    static Style Root => Css.Define(static () => new Style { Display = Display.InlineFlex, FlexShrink = 0 });

    // 旋转本身就是“还在加载”的信息，不随动效时长变为 0
    static Style Icon => Css.Define(static () => new Style
    {
        Keyframes = Spin,
        AnimationDuration = 800,
        AnimationTimingFunction = AnimationTimingFunction.Linear,
        AnimationIterationCount = "infinite",
        [Media.ReducedMotion] = new() { AnimationDuration = 1600 },
    });
}
