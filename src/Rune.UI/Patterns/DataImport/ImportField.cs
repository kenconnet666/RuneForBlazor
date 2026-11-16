using Rune.Styles;

namespace Rune.UI;

/// <summary>要导入的字段的值类型。</summary>
public enum ImportFieldType
{
    /// <summary>文字（默认）。</summary>
    Text,

    /// <summary>数字：把文字转成 decimal（去掉千分位的逗号），转不了算错。</summary>
    Number,
}

/// <summary><see cref="DataImport{TRow}"/> 要导入的一个字段。</summary>
/// <param name="Key">字段名：导入的行里的键。</param>
/// <param name="Label">名称：对应列时显示，也用来自动对上文件里的同名列。</param>
public sealed record ImportField(string Key, string Label)
{
    /// <summary>必填：没对应上不能往下走；空值算错。</summary>
    public bool Required { get; init; }

    /// <summary>文件里这一列可能的其他叫法（“料号”“Part No.”）：自动对应时也认。</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>值的类型，默认文字。</summary>
    public ImportFieldType Type { get; init; }

    /// <summary>预览表里这一列的宽度（px）。</summary>
    public double? Width { get; init; }
}

/// <summary>数据导入预览里的一行。</summary>
public sealed class ImportRow
{
    internal ImportRow(int line, Dictionary<string, object?> raw)
    {
        Line = line;
        Raw = raw;
    }

    /// <summary>第几行数据（从 1 起）。</summary>
    public int Line { get; }

    /// <summary>对应后的原值（字段名 → 值），就地改的是它。</summary>
    public Dictionary<string, object?> Raw { get; }

    /// <summary>字段名 → 原因；没有错时是空的。</summary>
    public IReadOnlyDictionary<string, string> Errors { get; internal set; } = new Dictionary<string, string>();

    /// <summary>校验后的值（字段名 → 转好的值：数字是 decimal，空是 null）。</summary>
    public IReadOnlyDictionary<string, object?> Values { get; internal set; } = new Dictionary<string, object?>();

    /// <summary>一格的文字（预览表、就地编辑用）。</summary>
    public string? Text(string key) => Raw.GetValueOrDefault(key) is { } value ? Conditions.ToText(value) : null;
}

/// <summary>数据导入的部件。</summary>
public enum DataImportPart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>步骤条。</summary>
    Steps,

    /// <summary>这一步的内容（换步后焦点在这里）。</summary>
    Panel,

    /// <summary>预览表（DataTable）。</summary>
    Preview,

    /// <summary>按钮行。</summary>
    Actions,
}

/// <summary>数据导入的样式（同 vue-tsx-pro 的 dataImportStyles）。</summary>
public static class DataImportStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<DataImportPart> Of(ComponentStyles styles) =>
        Recipe.Of("data-import", styles, static (DataImportPart part) => part switch
        {
            DataImportPart.Root => Root,
            DataImportPart.Panel => Panel,
            DataImportPart.Preview => Preview,
            _ => Sx.Of((Style?)null),
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Lg, MinWidth = 0 });

    // 换步后焦点在这里：不画焦点环（内容本身不是控件），读屏念出这一步的名称
    static Style Panel => Css.Define(static () => new Style { MinWidth = 0, OutlineStyle = OutlineStyle.None });

    static Style Preview => Css.Define(static () => new Style { MaxHeight = CssLength.Rem(24) });
}
