using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>比较的方式。</summary>
public enum ConditionOp
{
    /// <summary>等于。</summary>
    Eq,

    /// <summary>不等于。</summary>
    Ne,

    /// <summary>大于（日期：晚于）。</summary>
    Gt,

    /// <summary>大于等于。</summary>
    Gte,

    /// <summary>小于（日期：早于）。</summary>
    Lt,

    /// <summary>小于等于。</summary>
    Lte,

    /// <summary>在两个值之间（含两端）：值是 [下限, 上限]，任一端为空时不限。</summary>
    Between,

    /// <summary>是其中之一：值是一组。</summary>
    In,

    /// <summary>不是其中任何一个：值是一组。</summary>
    NotIn,

    /// <summary>文字包含。</summary>
    Contains,

    /// <summary>文字不包含。</summary>
    NotContains,

    /// <summary>文字以它开头。</summary>
    StartsWith,

    /// <summary>有值（不是 null、空字符串、空的一组）。</summary>
    Exists,

    /// <summary>没有值。</summary>
    Empty,
}

/// <summary>字段的类型：决定能用哪些比较方式、值用什么控件填。</summary>
public enum ConditionFieldType
{
    /// <summary>文字：包含、等于、开头是。</summary>
    Text,

    /// <summary>数字：大小、介于。</summary>
    Number,

    /// <summary>日期（ISO 文字 <c>2026-11-16</c>）：早于、晚于、介于。</summary>
    Date,

    /// <summary>从选项里选：是、不是、是其中之一。</summary>
    Select,

    /// <summary>是或否。</summary>
    Boolean,
}

/// <summary>值的形状：Between 两个值，In / NotIn 一组值，Exists / Empty 没有，其余一个。</summary>
public enum ConditionValueShape
{
    /// <summary>没有值。</summary>
    None,

    /// <summary>一个值。</summary>
    Single,

    /// <summary>[下限, 上限]。</summary>
    Range,

    /// <summary>一组值。</summary>
    List,
}

/// <summary>
/// 条件（同 vue-tsx-pro 的 Condition；Rune.Styles 里已有 Condition，这里叫 ConditionExpression）：比较一个字段（<see cref="ConditionRule"/>），或用全部满足（<see cref="ConditionAll"/>）、
/// 任一满足（<see cref="ConditionAny"/>）、不满足（<see cref="ConditionNot"/>）组合的表达式树。ConditionBuilder 编辑它，FilterBar 的高级筛选用它。
/// <code>
/// ConditionExpression condition = new ConditionAll([new ConditionRule("amount", ConditionOp.Gte, 10000m), new ConditionRule("category", ConditionOp.In, new object?[] { "钢材", "铝材" })]);
/// rows.Where(row =&gt; Conditions.Matches(condition, field =&gt; Read(row, field)));
/// Conditions.Describe(condition, fields, lang);   // “金额 大于等于 10,000，且 品类 是其中之一 钢材、铝材”
/// </code>
/// 值的形式：Between 是两个值的一组（含两端，任一端为空时不限），In / NotIn 是一组，Exists / Empty 没有值；数字用 decimal（或别的数字类型），
/// 日期是 ISO 文字，按文字比就是先后。
/// </summary>
public abstract record ConditionExpression;

/// <summary>比较一个字段。</summary>
/// <param name="Field">字段名。</param>
/// <param name="Op">比较的方式。</param>
/// <param name="Value">比较的值；Exists、Empty 没有。一组值（In、NotIn、Between）是 <see cref="IReadOnlyList{T}"/>。</param>
public sealed record ConditionRule(string Field, ConditionOp Op, object? Value = null) : ConditionExpression;

/// <summary>全部满足。</summary>
/// <param name="Items">各项。</param>
public sealed record ConditionAll(IReadOnlyList<ConditionExpression> Items) : ConditionExpression;

/// <summary>任一满足。</summary>
/// <param name="Items">各项。</param>
public sealed record ConditionAny(IReadOnlyList<ConditionExpression> Items) : ConditionExpression;

/// <summary>不满足。</summary>
/// <param name="Inner">被否定的条件。</param>
public sealed record ConditionNot(ConditionExpression Inner) : ConditionExpression;

/// <summary>选项型字段的一个选项。</summary>
/// <param name="Value">值。</param>
/// <param name="Label">显示的文字。</param>
public sealed record ConditionOption(object Value, string Label);

/// <summary>自己画值的输入时拿到的参数。</summary>
/// <param name="Field">字段。</param>
/// <param name="Op">比较的方式。</param>
/// <param name="Value">现在的值。</param>
/// <param name="OnChange">改值。</param>
/// <param name="Label">无障碍名称（“金额的值”）。</param>
public sealed record ConditionInputContext(ConditionField Field, ConditionOp Op, object? Value, Action<object?> OnChange, string Label);

/// <summary>能参与比较的字段。</summary>
/// <param name="Name">字段名：写进条件的 Field。</param>
/// <param name="Label">显示名。</param>
/// <param name="Type">类型。</param>
public sealed record ConditionField(string Name, string Label, ConditionFieldType Type)
{
    /// <summary>分组名：字段多时字段列表按它分组（如“申请单”“流程变量”）。</summary>
    public string? Group { get; init; }

    /// <summary>选项（Select 类型）。</summary>
    public IReadOnlyList<ConditionOption> Options { get; init; } = [];

    /// <summary>能用的比较方式，默认按类型（<see cref="Conditions.Operators"/>）。</summary>
    public IReadOnlyList<ConditionOp>? Operators { get; init; }

    /// <summary>自己画值的输入（人员选择器、带单位的数字、DatePicker……）；类型仍决定比较方式与摘要里值的写法。</summary>
    public RenderFragment<ConditionInputContext>? Input { get; init; }

    /// <summary>摘要里值的写法，默认选项的文字、日期原样、是否写成“是 / 否”。</summary>
    public Func<object?, string>? Format { get; init; }
}

/// <summary>条件的求值、写成一句话与比较方式（同 vue-tsx-pro 的 condition.ts）。</summary>
public static class Conditions
{
    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>各类型默认能用的比较方式，第一个是新条件默认的。</summary>
    public static IReadOnlyDictionary<ConditionFieldType, IReadOnlyList<ConditionOp>> Operators { get; } = new Dictionary<ConditionFieldType, IReadOnlyList<ConditionOp>>
    {
        [ConditionFieldType.Text] = [ConditionOp.Contains, ConditionOp.NotContains, ConditionOp.Eq, ConditionOp.Ne, ConditionOp.StartsWith, ConditionOp.Empty, ConditionOp.Exists],
        [ConditionFieldType.Number] = [ConditionOp.Eq, ConditionOp.Ne, ConditionOp.Gt, ConditionOp.Gte, ConditionOp.Lt, ConditionOp.Lte, ConditionOp.Between, ConditionOp.Empty, ConditionOp.Exists],
        [ConditionFieldType.Date] = [ConditionOp.Eq, ConditionOp.Lt, ConditionOp.Gt, ConditionOp.Lte, ConditionOp.Gte, ConditionOp.Between, ConditionOp.Empty, ConditionOp.Exists],
        [ConditionFieldType.Select] = [ConditionOp.Eq, ConditionOp.Ne, ConditionOp.In, ConditionOp.NotIn, ConditionOp.Empty, ConditionOp.Exists],
        [ConditionFieldType.Boolean] = [ConditionOp.Eq],
    };

    /// <summary>字段能用的比较方式。</summary>
    public static IReadOnlyList<ConditionOp> OperatorsOf(ConditionField field) => field.Operators ?? Operators[field.Type];

    /// <summary>比较方式要的值的形状。</summary>
    public static ConditionValueShape ShapeOf(ConditionOp op) => op switch
    {
        ConditionOp.Exists or ConditionOp.Empty => ConditionValueShape.None,
        ConditionOp.Between => ConditionValueShape.Range,
        ConditionOp.In or ConditionOp.NotIn => ConditionValueShape.List,
        _ => ConditionValueShape.Single,
    };

    /// <summary>语言包、JSON 里比较方式的名字（eq、notIn）。</summary>
    public static string KeyOf(ConditionOp op) => JsonNamingCamel(op.ToString());

    /// <summary>语言包里类型的名字（text、select）。</summary>
    public static string KeyOf(ConditionFieldType type) => JsonNamingCamel(type.ToString());

    static string JsonNamingCamel(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    static bool IsBlank(object? value) => value is null || value is string { Length: 0 } || value is System.Collections.ICollection { Count: 0 };

    /// <summary>值写成文字：null 是空字符串；数字、日期用不随语言环境的写法。</summary>
    public static string ToText(object? value) => value switch
    {
        null => "",
        string text => text,
        bool flag => flag ? "true" : "false",
        DateOnly date => date.ToString("yyyy-MM-dd", Invariant),
        IFormattable formattable => formattable.ToString(null, Invariant),
        _ => value.ToString() ?? "",
    };

    /// <summary>比较已经填完整：要值的有值，Between 至少一端有值。</summary>
    public static bool IsComplete(ConditionRule rule) => ShapeOf(rule.Op) switch
    {
        ConditionValueShape.None => true,
        ConditionValueShape.Range => rule.Value is IReadOnlyList<object?> range && range.Any(end => !IsBlank(end)),
        _ => !IsBlank(rule.Value),
    };

    // 数字（各种数字类型、十进制写法的文字）按精确的数值比；其余写成文字后按编码顺序（ISO 日期按文字比就是先后）
    static decimal? NumberOf(object? value) => value switch
    {
        decimal d => d,
        int or long or short or byte or uint or ulong or ushort or sbyte => Convert.ToDecimal(value, Invariant),
        double or float => Convert.ToDouble(value, Invariant) is var d && double.IsFinite(d) ? (decimal)d : null,
        string text when decimal.TryParse(text, NumberStyles.Number, Invariant, out var parsed) => parsed,
        _ => null,
    };

    static int Compare(object? a, object? b) =>
        NumberOf(a) is { } x && NumberOf(b) is { } y ? x.CompareTo(y) : string.CompareOrdinal(ToText(a), ToText(b));

    // 等于：数字与数字文字视为相等（表单里的 "5" 与数据里的 5）
    static bool Same(object? a, object? b) => Equals(a, b) || (a is not null && b is not null && Compare(a, b) == 0);

    static string Lower(object? value) => ToText(value).ToLowerInvariant();

    // 对一个（不是一组的）值，这条比较成不成立
    static bool MatchesOne(ConditionOp op, object? actual, object? value) => op switch
    {
        ConditionOp.Eq => Same(actual, value),
        ConditionOp.Ne => !Same(actual, value),
        ConditionOp.Gt => actual is not null && Compare(actual, value) > 0,
        ConditionOp.Gte => actual is not null && Compare(actual, value) >= 0,
        ConditionOp.Lt => actual is not null && Compare(actual, value) < 0,
        ConditionOp.Lte => actual is not null && Compare(actual, value) <= 0,
        ConditionOp.Between => actual is not null && value is IReadOnlyList<object?> { Count: 2 } range
            && (IsBlank(range[0]) || Compare(actual, range[0]) >= 0) && (IsBlank(range[1]) || Compare(actual, range[1]) <= 0),
        ConditionOp.In => value is IEnumerable<object?> list && list.Any(item => Same(actual, item)),
        ConditionOp.NotIn => value is not IEnumerable<object?> list || !list.Any(item => Same(actual, item)),
        ConditionOp.Contains => Lower(actual).Contains(Lower(value), StringComparison.Ordinal),
        ConditionOp.NotContains => !Lower(actual).Contains(Lower(value), StringComparison.Ordinal),
        ConditionOp.StartsWith => Lower(actual).StartsWith(Lower(value), StringComparison.Ordinal),
        ConditionOp.Exists => !IsBlank(actual),
        _ => IsBlank(actual),
    };

    // 字段的值是一组时对每一项比：不等于、不在其中、不包含是“没有一项满足”，其余是“有一项满足”；有值、没有值看这一组是不是空的
    static bool MatchesRule(ConditionRule rule, object? actual)
    {
        if (rule.Op is ConditionOp.Exists or ConditionOp.Empty || actual is string || actual is not System.Collections.IEnumerable items)
            return MatchesOne(rule.Op, actual, rule.Value);
        var list = items.Cast<object?>();
        return rule.Op is ConditionOp.Ne or ConditionOp.NotIn or ConditionOp.NotContains
            ? list.All(item => MatchesOne(rule.Op, item, rule.Value))
            : list.Any(item => MatchesOne(rule.Op, item, rule.Value));
    }

    /// <summary>
    /// 记录满不满足条件：<paramref name="read"/> 按字段名取值（嵌套字段、计算字段由它决定）。没填完整的比较与空的组不加条件，
    /// 组里只看加了条件的那几项；不等于、不在其中、不包含把空值算作满足（同 vue 与后端的规则）。
    /// </summary>
    public static bool Matches(ConditionExpression condition, Func<string, object?> read) => Evaluate(condition, read) ?? true;

    // 求值；不加条件（没填完整的比较、空的组）时是 null
    static bool? Evaluate(ConditionExpression condition, Func<string, object?> read)
    {
        switch (condition)
        {
            case ConditionNot not:
                return !Evaluate(not.Inner, read);
            case ConditionAll or ConditionAny:
                var items = condition is ConditionAll all ? all.Items : ((ConditionAny)condition).Items;
                var results = items.Select(item => Evaluate(item, read)).OfType<bool>().ToList();
                if (results.Count == 0) return null;
                return condition is ConditionAll ? results.All(static r => r) : results.Any(static r => r);
            case ConditionRule rule:
                return IsComplete(rule) ? MatchesRule(rule, read(rule.Field)) : null;
            default:
                return null;
        }
    }

    /// <summary>比较方式的文字：日期、选项、是否这几种类型有自己的说法（早于、是其中之一）。</summary>
    public static string OperatorLabel(Lang lang, ConditionFieldType type, ConditionOp op) =>
        lang.ConditionTypeOperators.TryGetValue(KeyOf(type), out var own) && own.TryGetValue(KeyOf(op), out var label)
            ? label
            : lang.ConditionOperators.GetValueOrDefault(KeyOf(op), KeyOf(op));

    /// <summary>值在摘要里的写法。</summary>
    public static string FormatValue(Lang lang, ConditionField? field, object? value)
    {
        string One(object? item)
        {
            if (field?.Format is { } format) return format(item);
            if (field?.Type == ConditionFieldType.Boolean) return item is true ? lang.ConditionTrue : lang.ConditionFalse;
            if (field?.Options.FirstOrDefault(option => Same(option.Value, item)) is { } option) return option.Label;
            // 数字按语言写千分位（同 vue 的 toLocaleString）
            if (item is not string && NumberOf(item) is { } number) return number.ToString("#,0.############", CultureInfo.GetCultureInfo(lang.Code));
            return ToText(item);
        }
        return value is IEnumerable<object?> list ? string.Join(lang.ConditionValueSeparator, list.Select(One)) : One(value);
    }

    /// <summary>条件写成一句话（收起的高级筛选用）：不认识的字段原样写字段名；嵌套的组加括号，否定写成“不满足：…”。</summary>
    public static string Describe(ConditionExpression condition, IReadOnlyList<ConditionField> fields, Lang lang)
    {
        string DescribeOne(ConditionExpression item, bool nested)
        {
            switch (item)
            {
                case ConditionNot not:
                    return lang.ConditionNotPrefix + DescribeOne(not.Inner, true);
                case ConditionAll or ConditionAny:
                    var list = item is ConditionAll all ? all.Items : ((ConditionAny)item).Items;
                    var text = string.Join(item is ConditionAll ? lang.ConditionAndJoiner : lang.ConditionOrJoiner, list.Select(child => DescribeOne(child, true)));
                    return nested && list.Count > 1 ? $"（{text}）" : text;
                case ConditionRule rule:
                    var field = fields.FirstOrDefault(candidate => candidate.Name == rule.Field);
                    var parts = new List<string> { field?.Label ?? rule.Field, OperatorLabel(lang, field?.Type ?? ConditionFieldType.Text, rule.Op) };
                    var shape = ShapeOf(rule.Op);
                    if (shape == ConditionValueShape.Range)
                    {
                        var range = rule.Value as IReadOnlyList<object?>;
                        parts.Add(lang.ConditionRange(FormatValue(lang, field, range is { Count: > 0 } ? range[0] : null), FormatValue(lang, field, range is { Count: > 1 } ? range[1] : null)));
                    }
                    else if (shape != ConditionValueShape.None)
                    {
                        parts.Add(FormatValue(lang, field, rule.Value));
                    }
                    return string.Join(' ', parts);
                default:
                    return "";
            }
        }
        return DescribeOne(condition, false);
    }

    /// <summary>条件里有几条比较（按钮旁的计数）。</summary>
    public static int CountRules(ConditionExpression condition) => condition switch
    {
        ConditionNot not => CountRules(not.Inner),
        ConditionAll all => all.Items.Sum(CountRules),
        ConditionAny any => any.Items.Sum(CountRules),
        _ => 1,
    };
}
