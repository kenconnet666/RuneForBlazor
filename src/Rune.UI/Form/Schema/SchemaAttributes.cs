namespace Rune.UI;

/// <summary>
/// 按模型生成表单（同 vue-tsx-pro 的 SchemaForm，vue 从 zod schema 推字段，这里从 C# 模型推）：标在 partial 类上，
/// 源生成器为它实现 <see cref="IFormSchema{TSelf}"/>，SchemaForm、SchemaFields、SchemaView、SchemaFilters 读这份描述（不用反射，裁剪安全）。
/// <code>
/// [FormSchema]
/// public partial class SupplierApplication
/// {
///     [Display(Name = "公司全称", GroupName = "企业信息"), Required, StringLength(60)]
///     public string? Company { get; set; }
///
///     [Display(Name = "供货品类")]
///     public Category? Category { get; set; }                      // 枚举：下拉，选项文字取各成员的 [Display(Name)]
///
///     [Display(Name = "补充说明"), StringLength(500)]                // 最长超过 100：多行文本
///     [FormField(FullRow = true)]
///     public string? Note { get; set; }
/// }
/// </code>
/// 标签、说明、占位、分组、顺序写在 DataAnnotations 的 <c>[Display(Name, Description, Prompt, GroupName, Order)]</c> 上，
/// 校验照旧由 DataAnnotations 负责（<c>[Required]</c> 的字段标必填）；控件、栏宽、页签、单位、条件写在 <see cref="FormFieldAttribute"/> 上。
/// 公开的、可读可写的实例属性都生成字段，<c>[ScaffoldColumn(false)]</c> 的不生成。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FormSchemaAttribute : Attribute;

/// <summary>SchemaForm 生成的控件。</summary>
public enum SchemaControl
{
    /// <summary>按属性的类型选（默认）。</summary>
    Auto,

    /// <summary>单行文字：TextField。</summary>
    Text,

    /// <summary>多行文字：Textarea。</summary>
    Textarea,

    /// <summary>数字：NumberField。</summary>
    Number,

    /// <summary>开关：Switch。</summary>
    Switch,

    /// <summary>单个复选框：Checkbox（文字就是标签）。</summary>
    Checkbox,

    /// <summary>下拉单选：Select。</summary>
    Select,

    /// <summary>单选组：RadioGroup（选项少时）。</summary>
    Radio,

    /// <summary>下拉多选：Select 的多选。</summary>
    MultiSelect,

    /// <summary>复选组：CheckboxGroup。</summary>
    Checkboxes,

    /// <summary>日期：DatePicker（Rune.UI.Dates）。</summary>
    Date,

    /// <summary>日期加时分：DatePicker（Rune.UI.Dates，精确到分）。</summary>
    DateTime,

    /// <summary>嵌套的对象：一组字段。</summary>
    Object,

    /// <summary>对象的列表：可增删的行，每行一组字段。</summary>
    List,

    /// <summary>对象的列表排成表格（明细表）：一行一条、一列一个字段，能增删行；列写了 Sum 时表底有合计行。</summary>
    Table,

    /// <summary>人员、部门：PeoplePicker（数据来自 FormSources.People）。</summary>
    People,

    /// <summary>附件：Upload（上传交给 FormSources.Upload）。</summary>
    File,
}

/// <summary>
/// 字段在表单里的样子（同 vue-tsx-pro 的 zod meta 里 SchemaForm 用的那几项）。标签、说明、占位、分组写在 <c>[Display]</c> 上。
/// 条件写成模型上 bool 属性（或无参方法）的名字：<c>[FormField(VisibleWhen = nameof(Overseas))]</c>。
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class FormFieldAttribute : Attribute
{
    /// <summary>用哪个控件；默认按类型选。</summary>
    public SchemaControl Control { get; set; }

    /// <summary>分了多栏时占几栏；0 是一栏。</summary>
    public int Span { get; set; }

    /// <summary>占满一行。</summary>
    public bool FullRow { get; set; }

    /// <summary>页签：同一页签的字段放在一个标签页里（页签里照旧分组），没写的放在页签前面。</summary>
    public string? Tab { get; set; }

    /// <summary>数字的单位，写在输入框的结束边：元、天、kg。</summary>
    public string? Unit { get; set; }

    /// <summary>显示条件：模型上 bool 成员的名字；不成立时字段不显示（有条件的字段不要写 [Required]，必填用 RequiredWhen）。</summary>
    public string? VisibleWhen { get; set; }

    /// <summary>必填条件：成立时空着报“必填”。</summary>
    public string? RequiredWhen { get; set; }

    /// <summary>只读条件：成立时控件只读。</summary>
    public string? ReadOnlyWhen { get; set; }

    /// <summary>明细表（Table）的这一列在表底求和。</summary>
    public bool Sum { get; set; }

    /// <summary>选项来自登记的来源（<see cref="FormSources.Options"/>）：字典名、远程搜索的名字。</summary>
    public string? Source { get; set; }
}

/// <summary>字段的权限（同 vue-tsx-pro 的 SchemaFieldAccess，按字段路径给）：流程的环节权限（字段 × 环节）直接映射过来。</summary>
public enum SchemaFieldAccess
{
    /// <summary>照模型与条件。</summary>
    Editable,

    /// <summary>不显示、不校验。</summary>
    Hidden,

    /// <summary>只读、不算必填。</summary>
    ReadOnly,

    /// <summary>必填。</summary>
    Required,
}
