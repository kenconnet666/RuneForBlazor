namespace Rune.UI;

/// <summary><see cref="FormDialogField"/> 的控件。</summary>
public enum FormDialogFieldType
{
    /// <summary>单行文字（默认）。</summary>
    Text,

    /// <summary>多行文字。</summary>
    Textarea,

    /// <summary>下拉选择（<see cref="FormDialogField.Options"/>；<see cref="FormDialogField.Multiple"/> 时多选）。</summary>
    Select,

    /// <summary>日期：值是 ISO 日期（<c>2026-11-16</c>），可限 Min / Max。用浏览器自带的日期输入（Rune.UI 不依赖 Rune.UI.Dates）。</summary>
    Date,
}

/// <summary><see cref="DialogService.PromptAsync"/> 对话框里的一个字段。</summary>
/// <param name="Name">字段名：结果里的 key。</param>
/// <param name="Label">标签。</param>
public sealed record FormDialogField(string Name, string Label)
{
    /// <summary>控件，默认单行文字。</summary>
    public FormDialogFieldType Type { get; init; }

    /// <summary>必填：没填时提示“必填”、不提交。</summary>
    public bool Required { get; init; }

    /// <summary>下拉的选项（Select）；Description 是选项的第二行，Group 相同的归在一组标题下。</summary>
    public IReadOnlyList<ListboxOption> Options { get; init; } = [];

    /// <summary>多选（Select）：值在 <see cref="FormDialogValues.All"/> 里，不经过 <see cref="Validate"/>。</summary>
    public bool Multiple { get; init; }

    /// <summary>初始值（单值字段）。</summary>
    public string? DefaultValue { get; init; }

    /// <summary>初始选中的值（多选）。</summary>
    public IReadOnlyList<string> DefaultValues { get; init; } = [];

    /// <summary>占位文字。</summary>
    public string? Placeholder { get; init; }

    /// <summary>标签下面的说明。</summary>
    public string? Description { get; init; }

    /// <summary>最多几个字（Text、Textarea）。</summary>
    public int? MaxLength { get; init; }

    /// <summary>最早可选的日期（Date，ISO 日期）。</summary>
    public string? Min { get; init; }

    /// <summary>最晚可选的日期（Date，ISO 日期）。</summary>
    public string? Max { get; init; }

    /// <summary>校验：返回错误说明时不提交；第二个参数是全部单值字段的值（已去掉首尾空白）。</summary>
    public Func<string, IReadOnlyDictionary<string, string>, string?>? Validate { get; init; }
}

/// <summary><see cref="DialogService.PromptAsync"/> 的选项。</summary>
public sealed record FormDialogOptions
{
    /// <summary>标题，同时是对话框的名称。</summary>
    public required string Title { get; init; }

    /// <summary>标题下面的说明。</summary>
    public string? Description { get; init; }

    /// <summary>字段。</summary>
    public required IReadOnlyList<FormDialogField> Fields { get; init; }

    /// <summary>确认按钮的文字，默认取语言包（确定）。</summary>
    public string? ConfirmText { get; init; }

    /// <summary>确认按钮的色调，默认 Accent；驳回、终止用 Danger。</summary>
    public Tone Tone { get; init; } = Tone.Accent;

    /// <summary>对话框的宽度档位，默认 Sm（40rem）。</summary>
    public DialogSize? Size { get; init; }

    /// <summary>提交：执行期间确认按钮加载中；返回 false 时对话框留着（提交被拒绝，由调用方提示原因），可以再试或取消。</summary>
    public Func<FormDialogValues, Task<bool>>? OnSubmit { get; init; }
}

/// <summary><see cref="DialogService.PromptAsync"/> 填好的值：单值字段是去掉首尾空白的文字，多选字段是选中的值。</summary>
public sealed class FormDialogValues
{
    readonly IReadOnlyDictionary<string, string> texts;
    readonly IReadOnlyDictionary<string, IReadOnlyList<string>> lists;

    internal FormDialogValues(IReadOnlyDictionary<string, string> texts, IReadOnlyDictionary<string, IReadOnlyList<string>> lists)
    {
        this.texts = texts;
        this.lists = lists;
    }

    /// <summary>单值字段的文字（没有这个字段时是空字符串）。</summary>
    public string this[string name] => texts.GetValueOrDefault(name, "");

    /// <summary>多选字段选中的值（没有这个字段时是空的）。</summary>
    public IReadOnlyList<string> All(string name) => lists.GetValueOrDefault(name, []);

    /// <summary>全部单值字段。</summary>
    public IReadOnlyDictionary<string, string> Texts => texts;
}
