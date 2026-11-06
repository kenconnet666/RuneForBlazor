using Rune.Styles;

namespace Rune.UI;

/// <summary>命令面板的部件（对话框本身的部件见 <see cref="DialogPart"/>）。</summary>
public enum CommandPalettePart
{
    /// <summary>默认触发按钮（画成输入框外壳）。</summary>
    Trigger,

    /// <summary>触发按钮里的文字（名称）。</summary>
    TriggerLabel,

    /// <summary>触发按钮里的快捷键（Kbd 组件）。</summary>
    TriggerHotkey,

    /// <summary>搜索行（放大镜与输入框）。</summary>
    Search,

    /// <summary>输入框上叠加的样式。</summary>
    Input,

    /// <summary>结果列表（ListboxView 组件）。</summary>
    Listbox,
}

/// <summary>命令面板的样式（同 vue-tsx-pro 的 commandPaletteStyles）。</summary>
public static class CommandPaletteStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<CommandPalettePart> Of(ComponentStyles styles) =>
        Recipe.Of("command-palette", styles, static (CommandPalettePart part) => part switch
        {
            CommandPalettePart.Trigger => Trigger,
            CommandPalettePart.TriggerLabel => TriggerLabel,
            CommandPalettePart.TriggerHotkey => TriggerHotkey,
            CommandPalettePart.Search => Search,
            CommandPalettePart.Input => Input,
            _ => Listbox,
        });

    /// <summary>对话框：停在视口上部而不是正中，结果变多变少时输入框不跳。</summary>
    internal static Style Dialog => Css.Define(static () => new Style { MarginBlockStart = CssLength.Dvh(12) });

    /// <summary>对话框的正文：结果列表贴边。</summary>
    internal static Style Body => Css.Define(static () => new Style { Padding = 0 });

    // 窄屏（40rem 以下）只留放大镜图标，不挤占顶栏
    static Style Trigger => Css.Define(static () => new Style
    {
        Gap = Space.Sm,
        Color = Colors.TextMuted,
        Cursor = Cursor.Pointer,
        [Breakpoints.Sm] = new() { MinWidth = CssLength.Rem(12) },
    });

    // 按钮的名称来自这段文字（与看得见的一致，WCAG 2.5.3）：窄屏只留图标时视觉上隐藏，读屏仍念得到
#pragma warning disable RUNE007
    static Style TriggerLabel => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        TextAlign = TextAlign.Start,
        WhiteSpace = WhiteSpace.Nowrap,
        Position = Position.Absolute,
        Width = 1,
        Height = 1,
        Overflow = Overflow.Hidden,
        ClipPath = "inset(50%)",
        [Breakpoints.Sm] = new() { Position = Position.Static, Width = CssLength.Auto, Height = CssLength.Auto, Overflow = Overflow.Visible, ClipPath = ClipPath.None },
    });
#pragma warning restore RUNE007

    static Style TriggerHotkey => Css.Define(static () => new Style
    {
        Display = Display.None,
        [Breakpoints.Sm] = new() { Display = CssWide.Initial },
    });

    static Style Search => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        PaddingBlock = Space.Sm,
        PaddingInline = Space.Md,
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderStyle.Solid,
        BorderBlockEndColor = Colors.Border,
        FontSize = FontSizes.Lg,
    });

    static Style Input => Css.Define(static () => new Style { PaddingBlock = Space.Sm });

    static Style Listbox => Css.Define(static () => new Style { MaxHeight = "min(24rem, 50dvh)" });
}
