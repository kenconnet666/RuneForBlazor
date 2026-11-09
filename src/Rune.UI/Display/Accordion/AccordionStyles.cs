using Rune.Styles;

namespace Rune.UI;

/// <summary>手风琴的部件。</summary>
public enum AccordionPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>每一项的外层（div）。</summary>
    Item,

    /// <summary>标题（h2–h6）。</summary>
    Heading,

    /// <summary>标题按钮。</summary>
    Trigger,

    /// <summary>展开箭头。</summary>
    Chevron,

    /// <summary>内容区里放内边距的那一层（Collapse 的内容层）。</summary>
    Content,
}

/// <summary>手风琴的样式（同 vue-tsx-pro 的 accordionStyles）：项之间一条细线，标题按钮占满一行、悬停变底色，展开时箭头转过来。</summary>
public static class AccordionStyles
{
    /// <summary>某种状态（展开、不可用）、字号档位（外层用，各项写 null）、某套定制下各部件的默认层。</summary>
    public static PartLayers<AccordionPart> Of(bool open, bool disabled, Size? size, ComponentStyles styles) =>
        Recipe.Of("accordion", (open, disabled, size), styles, static ((bool Open, bool Disabled, Size? Size) s, AccordionPart part) => part switch
        {
            // 字号在配方里：按部件的定制排在它后面，盖得住
            AccordionPart.Root => Sx.Of(Root, s.Size is { } size ? ScaleStyles.FontSize(size) : null),
            AccordionPart.Item => Item,
            AccordionPart.Heading => Heading,
            // 同 vue 的 ButtonBase：去默认外观、焦点环、波纹、交互状态的颜色过渡
            // 标题贴着卡片、边框的边：焦点环画在里面
            AccordionPart.Trigger => Sx.Of(SharedStyles.ButtonReset, SharedStyles.FocusRingInset, SharedStyles.RippleHost, SharedStyles.InteractionColors, Trigger, s.Disabled ? TriggerDisabled : null),
            AccordionPart.Chevron => Sx.Of(Chevron, DisclosureStyles.Icon, s.Open ? DisclosureStyles.Flip : null),
            _ => Content,
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column });

    static Style Item => Css.Define(static () => new Style
    {
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderStyle.Solid,
        BorderBlockEndColor = Colors.BorderMuted,
    });

    static Style Heading => Css.Define(static () => new Style { Margin = 0, FontSize = CssWide.Inherit, FontWeight = CssWide.Inherit });

    static Style Trigger => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Md,
        Width = CssLength.Percent(100),
        MinHeight = Sizes.MinTarget,
        PaddingBlock = Space.Md,
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Sm,
        BackgroundColor = CssColors.Transparent,
        Color = Colors.Text,
        FontWeight = FontWeights.Medium,
        TextAlign = TextAlign.Start,
        [When.Hover] = new() { BackgroundColor = Colors.Fill },
    });

    static Style TriggerDisabled => Css.Define(static () => new Style
    {
        Opacity = Opacities.Disabled,
        Cursor = Cursor.NotAllowed,
        [When.Hover] = new() { BackgroundColor = CssColors.Transparent },
    });

    static Style Chevron => Css.Define(static () => new Style { FlexShrink = 0, Color = Colors.TextMuted });

    static Style Content => Css.Define(static () => new Style { PaddingInline = Space.Sm, PaddingBlockEnd = Space.Md, Color = Colors.Text });
}

/// <summary>Accordion 交给 AccordionItem 的状态（级联值）。</summary>
/// <param name="Expanded">展开着的项。</param>
/// <param name="HeadingLevel">标题的层级（2–6）。</param>
/// <param name="Disabled">整组不可用。</param>
/// <param name="Toggle">展开或收起一项（单开 / 多开的规则由组决定）。</param>
public sealed record AccordionState(IReadOnlyList<string> Expanded, int HeadingLevel, bool Disabled, Func<string, Task> Toggle);
