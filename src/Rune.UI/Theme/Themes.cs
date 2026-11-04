using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 内置主题。自定义主题以它们为基础，只写不同的 token：
/// <code>new Theme("brand", basedOn: Themes.Light) { [Colors.Accent] = "#7c3aed" }</code>
/// 每个 token 都要在 <see cref="Light"/> 里有值（测试会检查），暗色主题只覆盖颜色与阴影。
/// </summary>
public static class Themes
{
    /// <summary>亮色主题（默认）。</summary>
    public static Theme Light => Css.Define(static () => new Theme("light")
    {
        [Space.Xxs] = 2,
        [Space.Xs] = 4,
        [Space.Sm] = 8,
        [Space.Md] = 12,
        [Space.Lg] = 16,
        [Space.Xl] = 24,

        [Radii.Sm] = 4,
        [Radii.Md] = 6,
        [Radii.Lg] = 10,

        [FontSizes.Xs] = 12,
        [FontSizes.Sm] = 13,
        [FontSizes.Md] = 14,
        [FontSizes.Lg] = 16,
        [FontSizes.Xl] = 20,
        [FontWeights.Normal] = 400,
        [FontWeights.Semibold] = 600,
        [LineHeights.Tight] = 1.25,
        [LineHeights.Normal] = 1.5,

        [Durations.Fast] = "120ms",
        [Durations.Normal] = "200ms",
        [Easings.Standard] = "cubic-bezier(0.2, 0, 0, 1)",
        [Easings.Enter] = "cubic-bezier(0, 0, 0, 1)",
        [Easings.Exit] = "cubic-bezier(0.3, 0, 1, 1)",

        [ZIndices.Dropdown] = 1000,
        [ZIndices.Modal] = 1100,
        [ZIndices.Toast] = 1200,
        [ZIndices.Tooltip] = 1300,

        [Sizes.MinTarget] = 24,
        [Sizes.Control] = 32,

        [Colors.Text] = "#1f2328",
        [Colors.TextMuted] = "#59636e",
        [Colors.Background] = "#ffffff",
        [Colors.Surface] = "#f6f8fa",
        [Colors.SurfaceHover] = "#eef1f4",
        [Colors.Border] = "#d1d9e0",
        [Colors.Accent] = "#2563eb",
        [Colors.AccentContrast] = "#ffffff",
        [Colors.FocusRing] = "#2563eb80",

        [Shadows.Sm] = "0 1px 2px rgb(31 35 40 / 8%)",
        [Shadows.Md] = "0 4px 12px rgb(31 35 40 / 12%)",
        [Shadows.Lg] = "0 12px 32px rgb(31 35 40 / 18%)",
    });

    /// <summary>暗色主题：在亮色主题的基础上换颜色与阴影。</summary>
    public static Theme Dark => Css.Define(static () => new Theme("dark", basedOn: Light)
    {
        [Colors.Text] = "#e6edf3",
        [Colors.TextMuted] = "#9198a1",
        [Colors.Background] = "#0d1117",
        [Colors.Surface] = "#151b23",
        [Colors.SurfaceHover] = "#1c232c",
        [Colors.Border] = "#3d444d",
        [Colors.Accent] = "#4c8dff",
        [Colors.AccentContrast] = "#0d1117",
        [Colors.FocusRing] = "#4c8dff80",

        [Shadows.Sm] = "0 1px 2px rgb(1 4 9 / 60%)",
        [Shadows.Md] = "0 4px 12px rgb(1 4 9 / 70%)",
        [Shadows.Lg] = "0 12px 32px rgb(1 4 9 / 80%)",
    });
}
