using System.Globalization;
using Rune.Styles;

namespace Rune.UI;

/// <summary>多行文本框里 textarea 的样式（同 vue-tsx-pro 的 textareaStyles）：外壳与 TextField 相同，InputStyles 的 Input 部件在多行时叠上这些。</summary>
public static class TextareaStyles
{
    // 随外壳拉伸：外壳给了高度时文字区占满，不停在基线上；原生控件换不了浮层滚动条，改细
    internal static Style Textarea => Css.Define(static () => new Style
    {
        AlignSelf = AlignSelf.Stretch,
        Resize = "none",
        OverflowY = Overflow.Auto,
        ScrollbarWidth = "thin",
        ScrollbarColor = $"color-mix(in srgb, {Colors.TextMuted} 50%, transparent) transparent",
    });

    /// <summary>字数计数贴在右下角。</summary>
    internal static Style Count => Css.Define(static () => new Style { AlignSelf = AlignSelf.FlexEnd, PaddingBlockEnd = Space.Xs });

    /// <summary>清空按钮贴在第一行。</summary>
    internal static Style Clear => Css.Define(static () => new Style { AlignSelf = AlignSelf.FlexStart, MarginBlockStart = Space.Xs });

    internal static Style Resizable => Css.Define(static () => new Style { Resize = "vertical" });

    internal static Style MinRows(int rows) => Css.Define(rows, static rows => new Style { MinHeight = $"{rows.ToString(CultureInfo.InvariantCulture)}lh" });

    internal static Style MaxRows(int rows) => Css.Define(rows, static rows => new Style { MaxHeight = $"{rows.ToString(CultureInfo.InvariantCulture)}lh" });
}
