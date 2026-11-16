using Rune.Styles;

namespace Rune.UI;

/// <summary>按模型生成的表单的部件（SchemaFields、SchemaForm 共用 data-scope="schema-form"）。</summary>
public enum SchemaFormPart
{
    /// <summary>SchemaForm 的表单（Form）。</summary>
    Form,

    /// <summary>按钮那一行。</summary>
    Actions,

    /// <summary>按钮那一行起始边的内容（提交结果、说明）。</summary>
    ActionsStart,

    /// <summary>列表、明细表那一块。</summary>
    Rows,

    /// <summary>“添加一行”按钮。</summary>
    AddRow,

    /// <summary>列表的各行。</summary>
    List,

    /// <summary>列表的一行（字段与删除按钮）。</summary>
    Row,

    /// <summary>一行里的字段。</summary>
    RowFields,

    /// <summary>一行的删除按钮。</summary>
    RemoveRow,

    /// <summary>页签里的几组。</summary>
    TabPanel,

    /// <summary>明细表（Table）。</summary>
    Table,

    /// <summary>明细表的一格。</summary>
    TableCell,

    /// <summary>明细表行末的操作列。</summary>
    TableActions,

    /// <summary>明细表的合计。</summary>
    TableTotal,
}

/// <summary>SchemaFields、SchemaForm 的样式（同 vue-tsx-pro 的 schemaFormStyles）。</summary>
public static class SchemaFormStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<SchemaFormPart> Of(ComponentStyles styles) =>
        Recipe.Of("schema-form", styles, static (SchemaFormPart part) => part switch
        {
            SchemaFormPart.Form => Form,
            SchemaFormPart.Actions => Actions,
            SchemaFormPart.ActionsStart => ActionsStart,
            SchemaFormPart.List => List,
            SchemaFormPart.Row => Row,
            SchemaFormPart.RowFields => RowFields,
            SchemaFormPart.RemoveRow => RowRemove,
            SchemaFormPart.TabPanel => TabPanel,
            SchemaFormPart.TableCell => TableCell,
            SchemaFormPart.TableActions => TableActions,
            SchemaFormPart.TableTotal => TableTotal,
            _ => Sx.Of((Style?)null),
        });

    static Style Form => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Lg });

    static Style Actions => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, FlexWrap = FlexWrap.Wrap, Gap = Space.Sm });

    static Style ActionsStart => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0, Color = Colors.TextMuted, FontSize = CssLength.Em(0.875) });

    static Style List => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Md });

    static Style Row => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.FlexStart, Gap = Space.Sm });

    static Style RowFields => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0 });

    // 删除按钮与第一行输入框对齐：让开标签的高度
    static Style RowRemove => Css.Define(static () => new Style { MarginBlockStart = CssLength.Em(1.75) });

    // 页签里的几组：同表单本身的间距
    static Style TabPanel => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Lg, PaddingBlockStart = Space.Md });

    // 明细表：每格放一个控件，列至少能放下输入框；合计右对齐、等宽数字
    static Style TableCell => Css.Define(static () => new Style { MinWidth = CssLength.Em(8), VerticalAlign = VerticalAlign.Top });

    static Style TableActions => Css.Define(static () => new Style { Width = CssLength.Percent(1), VerticalAlign = VerticalAlign.Top });

    static Style TableTotal => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold, FontVariantNumeric = FontVariantNumeric.TabularNums });
}
