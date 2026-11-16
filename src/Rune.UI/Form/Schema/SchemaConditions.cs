namespace Rune.UI;

/// <summary>
/// 按模型给出条件构造器的字段（同 vue-tsx-pro 的 schemaConditionFields）：标签、类型、可选值与表单、查询栏同一份描述。
/// <code>&lt;FilterBar ConditionFields="SchemaConditions.FieldsOf&lt;OrderQuery&gt;(omit: [nameof(OrderQuery.Keyword)])" @bind-Condition="condition"&gt;…&lt;/FilterBar&gt;</code>
/// 嵌套的对象、列表、人员、附件不能当条件。
/// </summary>
public static class SchemaConditions
{
    /// <summary>模型的字段 → 条件字段（名字是属性名）。</summary>
    /// <param name="pick">只要这些字段，按这里的顺序。</param>
    /// <param name="omit">不要这些字段。</param>
    /// <param name="sources">数据来源：登记的字典给选项。</param>
    public static IReadOnlyList<ConditionField> FieldsOf<TModel>(IReadOnlyList<string>? pick = null, IReadOnlyList<string>? omit = null, FormSources? sources = null)
        where TModel : IFormSchema<TModel> =>
        [.. SchemaLayout.Pick(TModel.FormSchemaFields, pick, omit).SelectMany(field => TypeOf(field.Info.Control) is { } type
            ? [new ConditionField(field.Info.Key, field.Info.Label, type)
            {
                Group = field.Info.Group,
                Options = [.. (sources ?? FormSources.Empty).OptionsOf(field.Info).Select(option => new ConditionOption(option.Value, option.Label))],
            }]
            : Array.Empty<ConditionField>())];

    static ConditionFieldType? TypeOf(SchemaControl control) => control switch
    {
        SchemaControl.Text or SchemaControl.Textarea => ConditionFieldType.Text,
        SchemaControl.Number => ConditionFieldType.Number,
        SchemaControl.Date or SchemaControl.DateTime => ConditionFieldType.Date,
        SchemaControl.Select or SchemaControl.Radio or SchemaControl.MultiSelect or SchemaControl.Checkboxes => ConditionFieldType.Select,
        SchemaControl.Switch or SchemaControl.Checkbox => ConditionFieldType.Boolean,
        _ => null,
    };
}
