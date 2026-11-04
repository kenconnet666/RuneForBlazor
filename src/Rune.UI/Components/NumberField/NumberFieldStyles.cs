using Rune.Styles;

namespace Rune.UI;

/// <summary>数字输入框另有的部件（外框、输入元素、前后缀与 TextField 相同，按 <see cref="TextFieldPart"/> 定制）。</summary>
public enum NumberFieldPart
{
    /// <summary>减少 / 增加按钮的容器。</summary>
    Stepper,

    /// <summary>减少按钮（IconButton）。</summary>
    Decrement,

    /// <summary>增加按钮（IconButton）。</summary>
    Increment,
}

/// <summary>数字输入框的样式（配方）。</summary>
public static class NumberFieldStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<NumberFieldPart> Of(ComponentStyles styles) =>
        Recipe.Of("number-field", styles, static (NumberFieldPart part) => part switch
        {
            NumberFieldPart.Stepper => Stepper,
            _ => Button,
        });

    // 按钮贴着外框的结束边，不撑高外框
    static Style Stepper => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        FlexShrink = 0,
        MarginInlineEnd = "-0.4em",
    });

    static Style Button => Css.Define(static () => new Style { Color = Colors.TextMuted });
}
