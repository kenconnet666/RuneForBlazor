using Rune.Styles;

namespace Rune.UI;

/// <summary>字数计数的部件。</summary>
public enum CharacterCountPart
{
    /// <summary>外层（span）。</summary>
    Root,

    /// <summary>看得见的字数（对读屏隐藏）。</summary>
    Value,

    /// <summary>给读屏的完整句子（视觉隐藏）。</summary>
    Label,
}

/// <summary>字数计数的样式（同 vue-tsx-pro 的 characterCountStyles）。</summary>
public static class CharacterCountStyles
{
    /// <summary>是否超出上限下各部件的默认层。</summary>
    public static PartLayers<CharacterCountPart> Of(bool over, ComponentStyles styles) =>
        Recipe.Of("character-count", over, styles, static (bool over, CharacterCountPart part) => part switch
        {
            CharacterCountPart.Root => Sx.Of(Root, over ? Over : null),
            _ => Sx.Of(),
        });

    // 超出时 JS 初始化器也会写 data-over（输入时还没往返）
    static Style Root => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        FontSize = FontSizes.Sm,
        Color = Colors.TextMuted,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        WhiteSpace = WhiteSpace.Nowrap,
        [When.Attribute("[data-over]")] = new() { Color = Tones.Danger.Text },
    });

    static Style Over => Css.Define(static () => new Style { Color = Tones.Danger.Text });
}
