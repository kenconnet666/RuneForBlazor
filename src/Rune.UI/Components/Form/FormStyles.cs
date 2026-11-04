using Rune.Styles;

namespace Rune.UI;

/// <summary>表单的部件。</summary>
public enum FormPart
{
    /// <summary>form 元素。</summary>
    Root,

    /// <summary>错误汇总（Alert）。</summary>
    Summary,

    /// <summary>错误汇总里的列表。</summary>
    SummaryList,
}

/// <summary>表单的样式（配方）：字段之间竖排留间距，错误汇总在最上面。</summary>
public static class FormStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<FormPart> Of(ComponentStyles styles) =>
        Recipe.Of("form", styles, static (FormPart part) => part switch
        {
            FormPart.Root => Root,
            FormPart.Summary => Sx.Of(),
            _ => SummaryList,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Gap = Space.Md,
        MinWidth = 0,
    });

    static Style SummaryList => Css.Define(static () => new Style
    {
        Margin = 0,
        PaddingInlineStart = "1.2em",
    });
}
