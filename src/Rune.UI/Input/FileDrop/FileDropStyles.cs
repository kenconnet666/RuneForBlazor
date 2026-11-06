using Microsoft.AspNetCore.Components.Forms;
using Rune.Styles;

namespace Rune.UI;

/// <summary>选文件区域的样式（同 vue-tsx-pro 的 fileDropStyles）。</summary>
public static class FileDropStyles
{
    /// <summary>区域（label）：虚线框，拖着文件经过（data-dragging）时变成强调色。</summary>
    internal static Attrs Root(bool invalid, bool locked) => Css.Define((invalid, locked), static k => new Attrs
    {
        Sx = Sx.Of(RootStyle, SharedStyles.FocusRingWithin, k.invalid ? Invalid : null, k.locked ? Disabled : null),
    });

    /// <summary>原生 file input：透明地盖满整个区域，点击、放下文件都落在它上面（键盘焦点也在它上面，焦点环画在区域上）。</summary>
    internal static readonly Attrs Input = new() { Sx = InputStyle };

    internal static Style Icon => Css.Define(static () => new Style { Width = CssLength.Em(1.5), Height = CssLength.Em(1.5), Color = Colors.TextMuted });

    internal static Style Hint => Css.Define(static () => new Style { FontWeight = FontWeights.Medium });

    internal static Style Description => Css.Define(static () => new Style { FontSize = FontSizes.Sm, Color = Colors.TextMuted });

    static Style RootStyle => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Position = Position.Relative,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Xs,
        Width = CssLength.Percent(100),
        MinHeight = CssLength.Em(8),
        Padding = Space.Lg,
        BorderWidth = BorderWidths.Sm,
        BorderStyle = BorderStyle.Dashed,
        BorderColor = Colors.BorderStrong,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Surface,
        Color = Colors.Text,
        TextAlign = TextAlign.Center,
        Cursor = Cursor.Pointer,
        Transition = $"border-color {Durations.Sm} {Easings.Standard}, background-color {Durations.Sm} {Easings.Standard}",
        [When.HoverEnabled] = new() { BorderColor = Tones.Accent.Solid },
        [When.Attribute("[data-dragging]")] = new() { BorderColor = Tones.Accent.Solid, BackgroundColor = Tones.Accent.Soft },
    });

    static Style Invalid => Css.Define(static () => new Style { BorderColor = Tones.Danger.Solid, [When.HoverEnabled] = new() { BorderColor = Tones.Danger.Solid } });

    static Style Disabled => Css.Define(static () => new Style { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed });

    static Style InputStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        Margin = 0,
        Opacity = 0,
        Cursor = CssWide.Inherit,
    });
}

/// <summary>文件是否符合 accept（同原生 accept：逗号分隔的 <c>image/*</c>、<c>.pdf</c>、<c>application/json</c>）。拖放不受 accept 限制，FileDrop 按它补上。</summary>
public static class FileAccept
{
    /// <summary>符合；accept 为空时都符合。</summary>
    public static bool Matches(IBrowserFile file, string? accept) => Matches(file.Name, file.ContentType, accept);

    /// <summary>按文件名与类型判断；accept 为空时都符合。</summary>
    public static bool Matches(string name, string? contentType, string? accept)
    {
        if (string.IsNullOrWhiteSpace(accept)) return true;
        foreach (var raw in accept.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (raw.StartsWith('.') ? name.EndsWith(raw, StringComparison.OrdinalIgnoreCase)
                : raw.EndsWith("/*", StringComparison.Ordinal) ? contentType?.StartsWith(raw[..^1], StringComparison.OrdinalIgnoreCase) == true
                : string.Equals(contentType, raw, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
