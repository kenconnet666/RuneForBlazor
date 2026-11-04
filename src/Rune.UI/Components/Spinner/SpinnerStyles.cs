using Rune.Styles;

namespace Rune.UI;

/// <summary>加载指示的部件。</summary>
public enum SpinnerPart
{
    /// <summary>svg 元素。</summary>
    Root,
}

/// <summary>加载指示的样式：跟着字号的圆环，匀速旋转；减少动效时转得慢一些（仍要表达“进行中”）。</summary>
public static class SpinnerStyles
{
    /// <summary>某个字号档位、某套定制下的部件默认层。</summary>
    public static PartLayers<SpinnerPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("spinner", size, styles, static (Size? size, SpinnerPart part) =>
            Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null));

    static Keyframes Spin => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Transform = "rotate(0deg)" },
        ["to"] = new() { Transform = "rotate(360deg)" },
    });

    static Style Root => Css.Define(static () => new Style
    {
        Width = "1em",
        Height = "1em",
        FlexShrink = 0,
        Keyframes = Spin,
        AnimationDuration = 800,
        AnimationTimingFunction = AnimationTimingFunction.Linear,
        AnimationIterationCount = "infinite",
        [Media.ReducedMotion] = new() { AnimationDuration = 2400 },
    });
}
