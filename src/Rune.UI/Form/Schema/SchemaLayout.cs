using System.Collections;
using System.Globalization;

namespace Rune.UI;

/// <summary>字段此刻的样子（同 vue-tsx-pro 的 SchemaFieldState）。</summary>
/// <param name="Visible">显示（不显示的不渲染、不校验）。</param>
/// <param name="ReadOnly">只读。</param>
/// <param name="Required">条件或权限要求必填（模型上的 [Required] 另算）。</param>
public readonly record struct SchemaFieldState(bool Visible, bool ReadOnly, bool Required);

/// <summary>SchemaFields、SchemaView、SchemaFilters 共用的取舍与求值（同 vue-tsx-pro 的 schema-fields.ts 里与类型无关的部分）。</summary>
public static class SchemaLayout
{
    /// <summary>只要这些顶层字段（按 pick 的顺序），或者去掉 omit 里的。</summary>
    public static IReadOnlyList<SchemaField<TModel>> Pick<TModel>(IReadOnlyList<SchemaField<TModel>> fields, IReadOnlyList<string>? pick, IReadOnlyList<string>? omit) =>
        pick is not null
            ? [.. pick.SelectMany(key => fields.Where(field => field.Info.Key == key))]
            : omit is null ? fields : [.. fields.Where(field => !omit.Contains(field.Info.Key))];

    /// <summary>字段此刻的样子：权限 → 显示条件 → 只读（权限或条件）→ 必填（权限或条件）。</summary>
    public static SchemaFieldState StateOf<TModel>(SchemaField<TModel> field, TModel model, SchemaFieldAccess? access)
    {
        if (access == SchemaFieldAccess.Hidden || field.VisibleWhen?.Invoke(model) == false) return new(false, true, false);
        var readOnly = access == SchemaFieldAccess.ReadOnly || field.ReadOnlyWhen?.Invoke(model) == true;
        return new(true, readOnly, !readOnly && (access == SchemaFieldAccess.Required || field.RequiredWhen?.Invoke(model) == true));
    }

    /// <summary>值是不是空的（null、空白文字、空列表）：必填按它判断。</summary>
    public static bool IsBlank(object? value) => value switch
    {
        null => true,
        string text => string.IsNullOrWhiteSpace(text),
        ICollection collection => collection.Count == 0,
        _ => false,
    };

    /// <summary>
    /// 逐个走模型里的值字段（嵌套对象、列表逐行）：所在的对象与属性名（校验消息的 FieldIdentifier）、此刻的样子、空不空、模型上是否 [Required]。
    /// 列表的列没写权限时跟着列表；不显示的字段下面不再走。
    /// </summary>
    public static IEnumerable<(object Owner, string Key, SchemaFieldState State, bool Blank, bool Required)> Walk<TModel>(
        IEnumerable<SchemaField<TModel>> fields, TModel model, string prefix, IReadOnlyDictionary<string, SchemaFieldAccess>? access,
        SchemaFieldAccess? inherited = null) where TModel : class =>
        fields.SelectMany(field =>
        {
            var path = prefix + field.Info.Key;
            var state = StateOf(field, model, access?.GetValueOrDefault(path) ?? inherited);
            return field.Accept(model, new Walker(path, state, access, access?.GetValueOrDefault(path) ?? inherited));
        });

    sealed class Walker(string path, SchemaFieldState state, IReadOnlyDictionary<string, SchemaFieldAccess>? access, SchemaFieldAccess? own)
        : ISchemaVisitor<IEnumerable<(object Owner, string Key, SchemaFieldState State, bool Blank, bool Required)>>
    {
        public IEnumerable<(object, string, SchemaFieldState, bool, bool)> Value<TM, TV>(SchemaValueField<TM, TV> field, TM model) =>
            [(model!, field.Info.Key, state, IsBlank(field.Get(model)), field.Info.Required)];

        public IEnumerable<(object, string, SchemaFieldState, bool, bool)> Object<TM, TC>(SchemaObjectField<TM, TC> field, TM model) where TC : class, IFormSchema<TC> =>
            !state.Visible || field.Get(model) is not { } child ? [] : Walk(SchemaObjectField<TM, TC>.Fields, child, path + ".", access, own);

        public IEnumerable<(object, string, SchemaFieldState, bool, bool)> List<TM, TI>(SchemaListField<TM, TI> field, TM model) where TI : class, IFormSchema<TI>, new() =>
            !state.Visible || field.Get(model) is not { } items ? [] : items.SelectMany(item => Walk(SchemaListField<TM, TI>.Fields, item, path + ".", access, own));
    }

    /// <summary>明细表一列的合计（写了 Sum 的列）：decimal 相加（不经过浮点数），带千分位与单位；没有能加的值时是 null。</summary>
    public static string? Total<TItem>(SchemaField<TItem> column, IEnumerable<TItem> items)
    {
        if (!column.Info.Sum) return null;
        decimal? sum = null;
        foreach (var item in items)
        {
            if (column.Read(item) is { } value && value is IConvertible && !(value is string or bool))
                sum = (sum ?? 0) + Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        if (sum is not { } total) return null;
        var text = total.ToString("#,0.############", CultureInfo.InvariantCulture);
        return column.Info.Unit is { } unit ? $"{text} {unit}" : text;
    }
}
