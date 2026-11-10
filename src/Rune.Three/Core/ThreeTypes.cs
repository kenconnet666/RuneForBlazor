using Rune.Styles;

namespace Rune.UI;

/// <summary>3D 效果的画质档位（同 vue-tsx-pro 的 ThreeQuality）：像素比上限、粒子等元素的密度、是否抗锯齿。</summary>
public enum ThreeQuality
{
    /// <summary>按设备选择：省流量或很弱的设备 Low，触屏或 4 核以下 Medium，其余 High。</summary>
    Auto,

    /// <summary>像素比 1、密度 0.4、不抗锯齿。</summary>
    Low,

    /// <summary>像素比上限 1.5、密度 0.7。</summary>
    Medium,

    /// <summary>像素比上限 2、全密度。</summary>
    High,

    /// <summary>不创建 WebGL，直接显示降级内容：用户在设置里关掉了特效、省电模式。</summary>
    Off,
}

/// <summary>渲染状态（同 vue-tsx-pro 的 ThreeStatus），写在画布根元素的 data-state 上。</summary>
public enum ThreeStatus
{
    /// <summary>还没创建（没进过视口、在排队、被回收）。</summary>
    Idle,

    /// <summary>动画循环运行中。</summary>
    Running,

    /// <summary>停着：滚出视口、标签页隐藏或 Paused。</summary>
    Paused,

    /// <summary>只画了一帧静止画面（减少动态效果）。</summary>
    Still,

    /// <summary>没有 WebGL，或画质是 Off：显示降级内容。</summary>
    Fallback,
}

/// <summary>ThreeCanvas 的部件。</summary>
public enum ThreeCanvasPart
{
    /// <summary>根元素（div）：尺寸由它决定，默认占满父元素。</summary>
    Root,

    /// <summary>画布（canvas）：由 JS 建、铺满根元素，重建时换新的一块；部件属性复制到每一块上。</summary>
    Canvas,

    /// <summary>降级内容的外层（div）：没有 WebGL 或画质是 Off 时才显示。</summary>
    Fallback,
}

/// <summary>效果外层（EffectSurface 与建在它上面的效果）的部件。</summary>
public enum EffectSurfacePart
{
    /// <summary>根元素（div）：建立层叠上下文，尺寸由使用方决定。</summary>
    Root,

    /// <summary>铺在底层的画布（ThreeCanvas），不接收指针。</summary>
    Canvas,

    /// <summary>叠在效果上面的内容（div，有内容时才有）。</summary>
    Content,
}

/// <summary>各效果的默认色板（同 vue-tsx-pro 的 SHADER_PALETTE 等）：主题 token，跟随主题。</summary>
public static class ThreePalettes
{
    /// <summary>着色器背景、网格渐变：品牌色加图表色板里的紫、粉、青。</summary>
    public static readonly IReadOnlyList<CssColor> Shader = [Tones.Accent.Solid, ChartColors.Series5, ChartColors.Series4, ChartColors.Series6];

    /// <summary>极光：绿、青、品牌色、紫。</summary>
    public static readonly IReadOnlyList<CssColor> Aurora = [ChartColors.Series7, ChartColors.Series2, Tones.Accent.Solid, ChartColors.Series5];

    /// <summary>粒子场：品牌色、紫、青。</summary>
    public static readonly IReadOnlyList<CssColor> Particle = [Tones.Accent.Solid, ChartColors.Series5, ChartColors.Series6];

    /// <summary>线框地形：低处青、中间品牌色、高处紫。</summary>
    public static readonly IReadOnlyList<CssColor> WaveGrid = [ChartColors.Series6, Tones.Accent.Solid, ChartColors.Series5];

    /// <summary>彩纸：图表色板的前六个。</summary>
    public static readonly IReadOnlyList<CssColor> Confetti =
        [ChartColors.Series1, ChartColors.Series2, ChartColors.Series3, ChartColors.Series4, ChartColors.Series5, ChartColors.Series6];

    /// <summary>色板交给 JS 的写法（CSS 颜色的文字）。</summary>
    internal static string[] Text(IReadOnlyList<CssColor> palette) => [.. palette.Select(color => color.ToString())];
}
