using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>
/// 按字段的描述渲染控件（同 vue-tsx-pro SchemaForm 的 controlOf）：源生成器按属性的类型与 <c>[FormField(Control)]</c> 选其中一个，
/// 值、绑定表达式由 SchemaFields 交进来。日期在 Rune.UI.Dates 的 SchemaDateControls 里。
/// </summary>
public static class SchemaControls
{
    // 共同的绑定：值、改值、表达式（Field 据此取校验错误）
    static void Bind<TValue>(RenderTreeBuilder builder, SchemaControlArgs<TValue> args)
    {
        builder.AddComponentParameter(1, "Value", args.Value);
        builder.AddComponentParameter(2, "ValueChanged", args.ValueChanged);
        if (args.ValueExpression is not null) builder.AddComponentParameter(3, "ValueExpression", args.ValueExpression);
    }

    static void Placeholder(RenderTreeBuilder builder, SchemaFieldInfo field)
    {
        if (field.Placeholder is not null) builder.AddAttribute(4, "placeholder", field.Placeholder);
    }

    /// <summary>单行文字：TextField（查询栏里能清空）。</summary>
    public static RenderFragment Text(SchemaControlArgs<string> args) => builder =>
    {
        builder.OpenComponent<TextField>(0);
        Bind(builder, args);
        Placeholder(builder, args.Field);
        if (args.Clearable) builder.AddComponentParameter(5, nameof(TextField.Clearable), true);
        builder.CloseComponent();
    };

    /// <summary>多行文字：Textarea（3 行起，随内容增高）。</summary>
    public static RenderFragment Textarea(SchemaControlArgs<string> args) => builder =>
    {
        builder.OpenComponent<Textarea>(0);
        Bind(builder, args);
        Placeholder(builder, args.Field);
        builder.AddComponentParameter(5, nameof(UI.Textarea.Rows), 3);
        builder.CloseComponent();
    };

    /// <summary>数字：NumberField，单位写在结束边。</summary>
    public static RenderFragment Number<TValue>(SchemaControlArgs<TValue> args) => builder =>
    {
        builder.OpenComponent<NumberField<TValue>>(0);
        Bind(builder, args);
        Placeholder(builder, args.Field);
        if (args.Field.Unit is { } unit) builder.AddComponentParameter(5, nameof(NumberField<TValue>.Suffix), (RenderFragment)(b => b.AddContent(0, unit)));
        builder.CloseComponent();
    };

    /// <summary>开关：Switch。</summary>
    public static RenderFragment Switch(SchemaControlArgs<bool> args) => builder =>
    {
        builder.OpenComponent<Switch>(0);
        Bind(builder, args);
        builder.CloseComponent();
    };

    /// <summary>单个复选框：文字就是字段的标签（Field 的标签只给读屏）。</summary>
    public static RenderFragment Checkbox(SchemaControlArgs<bool> args) => builder =>
    {
        builder.OpenComponent<Checkbox>(0);
        Bind(builder, args);
        builder.AddComponentParameter(5, nameof(UI.Checkbox.ChildContent), (RenderFragment)(b => b.AddContent(0, args.Field.Label)));
        builder.CloseComponent();
    };

    /// <summary>下拉单选（文字值）：选项来自字段自己的或登记的字典；登记的是远程搜索时可以搜。</summary>
    public static RenderFragment Select(SchemaControlArgs<string> args) => builder => OpenSelect(builder, args.Field, args.Sources, args.Clearable, b =>
    {
        Bind(b, args);
    });

    /// <summary>下拉单选（枚举值）。</summary>
    public static RenderFragment EnumSelect<TEnum>(SchemaControlArgs<TEnum> args) where TEnum : struct, Enum => builder =>
        OpenSelect(builder, args.Field, args.Sources, args.Clearable, b =>
        {
            b.AddComponentParameter(1, "Value", args.Value.ToString());
            b.AddComponentParameter(2, "ValueChanged", EventCallback.Factory.Create<string?>(args.Owner, text =>
                Enum.TryParse<TEnum>(text, out var value) ? args.ValueChanged.InvokeAsync(value) : Task.CompletedTask));
        });

    /// <summary>下拉单选（可空的枚举值）：空着时为 null。</summary>
    public static RenderFragment NullableEnumSelect<TEnum>(SchemaControlArgs<TEnum?> args) where TEnum : struct, Enum => builder =>
        OpenSelect(builder, args.Field, args.Sources, args.Clearable, b =>
        {
            b.AddComponentParameter(1, "Value", args.Value?.ToString());
            b.AddComponentParameter(2, "ValueChanged", EventCallback.Factory.Create<string?>(args.Owner, text =>
                args.ValueChanged.InvokeAsync(Enum.TryParse<TEnum>(text, out var value) ? value : null)));
        });

    /// <summary>下拉多选（文字值）。</summary>
    public static RenderFragment MultiSelect(SchemaControlArgs<IReadOnlyList<string>> args) => builder =>
        OpenSelect(builder, args.Field, args.Sources, args.Clearable, b =>
        {
            b.AddComponentParameter(1, nameof(Select<ListboxOption>.SelectionMode), SelectionMode.Multiple);
            b.AddComponentParameter(2, nameof(Select<ListboxOption>.Values), args.Value ?? []);
            b.AddComponentParameter(3, nameof(Select<ListboxOption>.ValuesChanged), args.ValueChanged);
        });

    /// <summary>下拉多选（枚举值）。</summary>
    public static RenderFragment EnumMultiSelect<TEnum>(SchemaControlArgs<IReadOnlyList<TEnum>> args) where TEnum : struct, Enum => builder =>
        OpenSelect(builder, args.Field, args.Sources, args.Clearable, b =>
        {
            b.AddComponentParameter(1, nameof(Select<ListboxOption>.SelectionMode), SelectionMode.Multiple);
            b.AddComponentParameter(2, nameof(Select<ListboxOption>.Values), (IReadOnlyList<string>)[.. (args.Value ?? []).Select(value => value.ToString())]);
            b.AddComponentParameter(3, nameof(Select<ListboxOption>.ValuesChanged), EventCallback.Factory.Create<IReadOnlyList<string>>(args.Owner,
                texts => args.ValueChanged.InvokeAsync([.. texts.Select(Enum.Parse<TEnum>)])));
        });

    static void OpenSelect(RenderTreeBuilder builder, SchemaFieldInfo field, FormSources sources, bool clearable, Action<RenderTreeBuilder> bind)
    {
        builder.OpenComponent<Select<ListboxOption>>(0);
        builder.AddComponentParameter(10, nameof(Select<ListboxOption>.Items), sources.OptionsOf(field));
        if (field.Source is { } name && sources.Remote.TryGetValue(name, out var load))
        {
            builder.AddComponentParameter(11, nameof(Select<ListboxOption>.Load), load);
            builder.AddComponentParameter(12, nameof(Select<ListboxOption>.Searchable), true);
        }
        if (field.Placeholder is not null) builder.AddComponentParameter(13, nameof(Select<ListboxOption>.Placeholder), field.Placeholder);
        if (clearable) builder.AddComponentParameter(14, nameof(Select<ListboxOption>.Clearable), true);
        bind(builder);
        builder.CloseComponent();
    }

    /// <summary>单选组（文字值）：横排。</summary>
    public static RenderFragment Radio(SchemaControlArgs<string> args) => builder =>
    {
        var options = args.Sources.OptionsOf(args.Field);
        builder.OpenComponent<RadioGroup<string>>(0);
        Bind(builder, args);
        builder.AddComponentParameter(5, nameof(RadioGroup<string>.Items), options.Select(option => option.Value));
        builder.AddComponentParameter(6, nameof(RadioGroup<string>.ToOption), (Func<string, ListboxOption>)(value => options.FirstOrDefault(o => o.Value == value) ?? new(value, value)));
        builder.AddComponentParameter(7, nameof(RadioGroup<string>.Orientation), Orientation.Horizontal);
        builder.CloseComponent();
    };

    /// <summary>单选组（枚举值）：横排。</summary>
    public static RenderFragment EnumRadio<TEnum>(SchemaControlArgs<TEnum> args) where TEnum : struct, Enum => builder =>
    {
        var options = args.Sources.OptionsOf(args.Field);
        builder.OpenComponent<RadioGroup<TEnum>>(0);
        Bind(builder, args);
        builder.AddComponentParameter(5, nameof(RadioGroup<TEnum>.Items), options.Select(option => Enum.Parse<TEnum>(option.Value)));
        builder.AddComponentParameter(6, nameof(RadioGroup<TEnum>.ToOption), (Func<TEnum, ListboxOption>)(value => options.FirstOrDefault(o => o.Value == value.ToString()) ?? new(value.ToString(), value.ToString())));
        builder.AddComponentParameter(7, nameof(RadioGroup<TEnum>.Orientation), Orientation.Horizontal);
        builder.CloseComponent();
    };

    /// <summary>单选组（可空的枚举值）：横排，没选时为 null。</summary>
    public static RenderFragment NullableEnumRadio<TEnum>(SchemaControlArgs<TEnum?> args) where TEnum : struct, Enum => builder =>
    {
        var options = args.Sources.OptionsOf(args.Field);
        builder.OpenComponent<RadioGroup<TEnum?>>(0);
        Bind(builder, args);
        builder.AddComponentParameter(5, nameof(RadioGroup<TEnum?>.Items), options.Select(option => (TEnum?)Enum.Parse<TEnum>(option.Value)));
        builder.AddComponentParameter(6, nameof(RadioGroup<TEnum?>.ToOption), (Func<TEnum?, ListboxOption>)(value =>
            options.FirstOrDefault(o => o.Value == value?.ToString()) ?? new(value?.ToString() ?? "", value?.ToString() ?? "")));
        builder.AddComponentParameter(7, nameof(RadioGroup<TEnum?>.Orientation), Orientation.Horizontal);
        builder.CloseComponent();
    };

    /// <summary>复选组（文字值）：横排。</summary>
    public static RenderFragment Checkboxes(SchemaControlArgs<IReadOnlyList<string>> args) => builder =>
    {
        builder.OpenComponent<CheckboxGroup<ListboxOption>>(0);
        builder.AddComponentParameter(1, "Value", args.Value ?? []);
        builder.AddComponentParameter(2, "ValueChanged", args.ValueChanged);
        if (args.ValueExpression is not null) builder.AddComponentParameter(3, "ValueExpression", args.ValueExpression);
        builder.AddComponentParameter(5, nameof(CheckboxGroup<ListboxOption>.Items), args.Sources.OptionsOf(args.Field));
        builder.AddComponentParameter(6, nameof(CheckboxGroup<ListboxOption>.Orientation), Orientation.Horizontal);
        builder.CloseComponent();
    };

    /// <summary>复选组（枚举值）：横排。</summary>
    public static RenderFragment EnumCheckboxes<TEnum>(SchemaControlArgs<IReadOnlyList<TEnum>> args) where TEnum : struct, Enum => builder =>
    {
        builder.OpenComponent<CheckboxGroup<ListboxOption>>(0);
        builder.AddComponentParameter(1, "Value", (IReadOnlyList<string>)[.. (args.Value ?? []).Select(value => value.ToString())]);
        builder.AddComponentParameter(2, "ValueChanged", EventCallback.Factory.Create<IReadOnlyList<string>>(args.Owner,
            texts => args.ValueChanged.InvokeAsync([.. texts.Select(Enum.Parse<TEnum>)])));
        builder.AddComponentParameter(5, nameof(CheckboxGroup<ListboxOption>.Items), args.Sources.OptionsOf(args.Field));
        builder.AddComponentParameter(6, nameof(CheckboxGroup<ListboxOption>.Orientation), Orientation.Horizontal);
        builder.CloseComponent();
    };

    /// <summary>人员、部门：PeoplePicker，数据来自 <see cref="FormSources.People"/>。</summary>
    public static RenderFragment People(SchemaControlArgs<IReadOnlyList<PeopleEntry>> args) => builder =>
    {
        var source = args.Sources.People ?? throw new InvalidOperationException($"字段 {args.Field.Key} 是人员控件，要在 Sources 里给 People");
        builder.OpenComponent<PeoplePicker>(0);
        builder.AddComponentParameter(1, "Value", args.Value ?? []);
        builder.AddComponentParameter(2, "ValueChanged", args.ValueChanged);
        if (args.ValueExpression is not null) builder.AddComponentParameter(3, "ValueExpression", args.ValueExpression);
        builder.AddComponentParameter(5, nameof(PeoplePicker.Source), source);
        if (args.Field.Placeholder is not null) builder.AddComponentParameter(6, nameof(PeoplePicker.Placeholder), args.Field.Placeholder);
        builder.CloseComponent();
    };

    /// <summary>附件：Upload（多个文件），上传交给 <see cref="FormSources.Upload"/>。</summary>
    public static RenderFragment File(SchemaControlArgs<IReadOnlyList<UploadFile>> args) => builder =>
    {
        builder.OpenComponent<Upload>(0);
        builder.AddComponentParameter(1, "Value", args.Value ?? []);
        builder.AddComponentParameter(2, "ValueChanged", args.ValueChanged);
        if (args.ValueExpression is not null) builder.AddComponentParameter(3, "ValueExpression", args.ValueExpression);
        builder.AddComponentParameter(5, nameof(Upload.Multiple), true);
        if (args.Sources.Upload is { } upload) builder.AddComponentParameter(6, nameof(Upload.UploadAsync), upload);
        builder.CloseComponent();
    };
}
