using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 内置主题。自定义主题以它们为基础，只写不同的 token：
/// <code>new Theme("brand", basedOn: Themes.Light) { [Tones.Accent.Solid] = "#7c3aed" }</code>
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
        [Radii.Full] = 9999,

        [Opacities.Disabled] = 0.5,

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
        [Sizes.Control] = "2.25em",

        [Colors.Text] = "#1f2328",
        [Colors.TextMuted] = "#59636e",
        [Colors.Background] = "#ffffff",
        [Colors.Surface] = "#f6f8fa",
        [Colors.SurfaceHover] = "#eef1f4",
        [Colors.Border] = "#d1d9e0",
        [Colors.FocusRing] = "#2563eb80",

        [Tones.Neutral.Solid] = "#1f2328",
        [Tones.Neutral.SolidHover] = "#32383f",
        [Tones.Neutral.Contrast] = "#ffffff",
        [Tones.Neutral.Text] = "#1f2328",
        [Tones.Neutral.Soft] = "#eff2f5",
        [Tones.Neutral.SoftHover] = "#e3e8ed",
        [Tones.Neutral.Border] = "#d1d9e0",

        [Tones.Accent.Solid] = "#2563eb",
        [Tones.Accent.SolidHover] = "#1d4ed8",
        [Tones.Accent.Contrast] = "#ffffff",
        [Tones.Accent.Text] = "#1d4ed8",
        [Tones.Accent.Soft] = "#eff4ff",
        [Tones.Accent.SoftHover] = "#dbe6fe",
        [Tones.Accent.Border] = "#bfd3fe",

        [Tones.Info.Solid] = "#0369a1",
        [Tones.Info.SolidHover] = "#075985",
        [Tones.Info.Contrast] = "#ffffff",
        [Tones.Info.Text] = "#0369a1",
        [Tones.Info.Soft] = "#f0f9ff",
        [Tones.Info.SoftHover] = "#e0f2fe",
        [Tones.Info.Border] = "#bae6fd",

        [Tones.Success.Solid] = "#15803d",
        [Tones.Success.SolidHover] = "#166534",
        [Tones.Success.Contrast] = "#ffffff",
        [Tones.Success.Text] = "#15803d",
        [Tones.Success.Soft] = "#f0fdf4",
        [Tones.Success.SoftHover] = "#dcfce7",
        [Tones.Success.Border] = "#bbf7d0",

        [Tones.Warning.Solid] = "#b45309",
        [Tones.Warning.SolidHover] = "#92400e",
        [Tones.Warning.Contrast] = "#ffffff",
        [Tones.Warning.Text] = "#b45309",
        [Tones.Warning.Soft] = "#fffbeb",
        [Tones.Warning.SoftHover] = "#fef3c7",
        [Tones.Warning.Border] = "#fde68a",

        [Tones.Danger.Solid] = "#dc2626",
        [Tones.Danger.SolidHover] = "#b91c1c",
        [Tones.Danger.Contrast] = "#ffffff",
        [Tones.Danger.Text] = "#b91c1c",
        [Tones.Danger.Soft] = "#fef2f2",
        [Tones.Danger.SoftHover] = "#fee2e2",
        [Tones.Danger.Border] = "#fecaca",

        [Shadows.Sm] = "0 1px 2px rgb(31 35 40 / 8%)",
        [Shadows.Md] = "0 4px 12px rgb(31 35 40 / 12%)",
        [Shadows.Lg] = "0 12px 32px rgb(31 35 40 / 18%)",
    });

    /// <summary>暗色主题：在亮色主题的基础上换颜色（含各色调）与阴影。</summary>
    public static Theme Dark => Css.Define(static () => new Theme("dark", basedOn: Light)
    {
        [Colors.Text] = "#e6edf3",
        [Colors.TextMuted] = "#9198a1",
        [Colors.Background] = "#0d1117",
        [Colors.Surface] = "#151b23",
        [Colors.SurfaceHover] = "#1c232c",
        [Colors.Border] = "#3d444d",
        [Colors.FocusRing] = "#4c8dff80",

        [Tones.Neutral.Solid] = "#e6edf3",
        [Tones.Neutral.SolidHover] = "#c9d1d9",
        [Tones.Neutral.Contrast] = "#0d1117",
        [Tones.Neutral.Text] = "#e6edf3",
        [Tones.Neutral.Soft] = "#21262d",
        [Tones.Neutral.SoftHover] = "#2a313c",
        [Tones.Neutral.Border] = "#3d444d",

        [Tones.Accent.Solid] = "#4c8dff",
        [Tones.Accent.SolidHover] = "#6ea2ff",
        [Tones.Accent.Contrast] = "#0d1117",
        [Tones.Accent.Text] = "#79a8ff",
        [Tones.Accent.Soft] = "#13233f",
        [Tones.Accent.SoftHover] = "#1a2f55",
        [Tones.Accent.Border] = "#24467d",

        [Tones.Info.Solid] = "#38bdf8",
        [Tones.Info.SolidHover] = "#7dd3fc",
        [Tones.Info.Contrast] = "#0d1117",
        [Tones.Info.Text] = "#7dd3fc",
        [Tones.Info.Soft] = "#0c2533",
        [Tones.Info.SoftHover] = "#11324a",
        [Tones.Info.Border] = "#155e75",

        [Tones.Success.Solid] = "#4ade80",
        [Tones.Success.SolidHover] = "#86efac",
        [Tones.Success.Contrast] = "#0d1117",
        [Tones.Success.Text] = "#86efac",
        [Tones.Success.Soft] = "#0f2a1a",
        [Tones.Success.SoftHover] = "#143823",
        [Tones.Success.Border] = "#166534",

        [Tones.Warning.Solid] = "#fbbf24",
        [Tones.Warning.SolidHover] = "#fcd34d",
        [Tones.Warning.Contrast] = "#0d1117",
        [Tones.Warning.Text] = "#fcd34d",
        [Tones.Warning.Soft] = "#2d2208",
        [Tones.Warning.SoftHover] = "#3b2d0b",
        [Tones.Warning.Border] = "#92400e",

        [Tones.Danger.Solid] = "#f87171",
        [Tones.Danger.SolidHover] = "#fca5a5",
        [Tones.Danger.Contrast] = "#0d1117",
        [Tones.Danger.Text] = "#fca5a5",
        [Tones.Danger.Soft] = "#3a1414",
        [Tones.Danger.SoftHover] = "#4a1a1a",
        [Tones.Danger.Border] = "#991b1b",

        [Shadows.Sm] = "0 1px 2px rgb(1 4 9 / 60%)",
        [Shadows.Md] = "0 4px 12px rgb(1 4 9 / 70%)",
        [Shadows.Lg] = "0 12px 32px rgb(1 4 9 / 80%)",
    });
}
