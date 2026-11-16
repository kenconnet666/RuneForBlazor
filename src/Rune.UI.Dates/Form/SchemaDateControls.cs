using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// SchemaForm 的日期控件（同 vue-tsx-pro SchemaForm 的 date、datetime）：DatePicker。引用了 Rune.UI.Dates 时，
/// 源生成器给 DateOnly、DateTime、DateTimeOffset（及可空）的字段选它们。
/// </summary>
public static class SchemaDateControls
{
    /// <summary>日期：DatePicker。</summary>
    public static RenderFragment Date<TValue>(SchemaControlArgs<TValue> args) => Picker(args, DateGranularity.Day);

    /// <summary>日期加时分：DatePicker（精确到分）。</summary>
    public static RenderFragment DateTime<TValue>(SchemaControlArgs<TValue> args) => Picker(args, DateGranularity.Minute);

    static RenderFragment Picker<TValue>(SchemaControlArgs<TValue> args, DateGranularity granularity) => builder =>
    {
        builder.OpenComponent<DatePicker<TValue>>(0);
        builder.AddComponentParameter(1, "Value", args.Value);
        builder.AddComponentParameter(2, "ValueChanged", args.ValueChanged);
        if (args.ValueExpression is not null) builder.AddComponentParameter(3, "ValueExpression", args.ValueExpression);
        builder.AddComponentParameter(4, nameof(DatePicker<TValue>.Granularity), granularity);
        builder.CloseComponent();
    };
}
