using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 骨架的扫光（同 vue-tsx-pro 的 shimmer.ts、Ant Design Skeleton 的 active、MUI Skeleton 的 wave）：一道光带从起始边扫到结束边，表示内容正在来。
/// 光带画在元素的 ::after 上，用 transform 平移：合成器就能跑，满屏骨架也不用每帧重绘。元素因此 contain: paint。
/// 扫光只是装饰：减少动态效果时不扫（静止的占位已经说明在加载）；从右到左的语言里反向扫。
/// 多行依次扫过时，每行的 animation-delay 晚 <see cref="Stagger"/> 毫秒（写在元素的行内样式上，光带继承）。
/// </summary>
public static class ShimmerStyles
{
    /// <summary>多行骨架相邻两行的扫光间隔（毫秒）：一屏七八行时整道波约一秒扫完。</summary>
    public const int Stagger = 120;

    /// <summary>光带颜色的变量：默认是字色的淡色；Skeleton 改成正文色的淡色。</summary>
    public const string HighlightVar = "--rune-shimmer-highlight";

    /// <summary>扫光：元素上叠一道循环扫过的光带。</summary>
    public static Style Wave => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        // 用绘制包含裁掉移出去的光带（按圆角裁）：不用 overflow，行内的骨架仍按文字基线对齐
        // （overflow 不是 visible 时 inline-block 的基线落到底边）
        Contain = Contain.Paint,
        // 从右到左的语言里整个元素镜像，光带就反向扫；骨架包着的内容本来就看不见，镜像没有影响
        [When.Rtl] = new() { Transform = "scaleX(-1)" },
        [Pseudo.After] = new()
        {
            Content = "\"\"",
            Position = Position.Absolute,
            Inset = 0,
            BackgroundImage = $"linear-gradient(90deg, transparent 15%, var({HighlightVar}, color-mix(in oklab, currentColor 12%, transparent)) 50%, transparent 85%)",
            Transform = "translateX(-100%)",
            Keyframes = Sweep,
            // 循环的装饰动画不取 Durations（那是一次性过渡的时长，减少动态效果时为 0）：
            // 2 秒一轮，其中 1.4 秒扫过、0.6 秒停顿，接近 Ant Design、MUI 的节奏
            AnimationDuration = "2s",
            // 先慢后快再慢：光带像被推过去，而不是匀速平移
            AnimationTimingFunction = AnimationTimingFunction.EaseInOut,
            AnimationIterationCount = "infinite",
            // 延迟写在元素上，这里继承
            AnimationDelay = CssWide.Inherit,
        },
        [Media.ReducedMotion] = new() { [Pseudo.After] = new() { AnimationName = "none" } },
    });

    /// <summary>骨架换成内容（或内容换成骨架）时淡入，不硬切；减少动态效果时时长为 0。</summary>
    public static Style Reveal => Css.Define(static () => new Style
    {
        Keyframes = FadeIn,
        AnimationDuration = Durations.Lg,
        AnimationTimingFunction = Easings.Enter,
    });

    /// <summary>扫光延迟的行内样式（毫秒）：几块骨架依次扫过，连成一道波。</summary>
    public static string DelayOf(int delayMilliseconds) => $"animation-delay:{delayMilliseconds}ms";

    // 光带与元素一样宽：从元素外的起始侧（-100%）扫到结束侧（100%）；后 30% 的时间停在外面，两次之间有个间隔
    static Keyframes Sweep => Css.Define(static () => new Keyframes
    {
        ["0%"] = new() { Transform = "translateX(-100%)" },
        ["70%"] = new() { Transform = "translateX(100%)" },
        ["100%"] = new() { Transform = "translateX(100%)" },
    });

    internal static Keyframes FadeIn => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Opacity = 0 },
    });
}
