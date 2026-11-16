using System.ComponentModel.DataAnnotations;
using Rune.UI;

namespace Rune.Docs.Components.Form.Form.Demo;

public partial class GeneratedExample
{
    /// <summary>供货品类：下拉的文字取各成员的 [Display(Name)]。</summary>
    public enum SupplyCategory
    {
        [Display(Name = "机械件")] Machined,
        [Display(Name = "塑料件")] Plastic,
        [Display(Name = "钣金件")] SheetMetal,
        [Display(Name = "标准件")] Standard,
        [Display(Name = "线缆")] Cable,
    }

    /// <summary>准入申请：[FormSchema] 让源生成器给出字段描述（要写在 .cs 里，生成器看不到 .razor 的 @code）。</summary>
    [FormSchema]
    public partial class AdmissionForm : IValidatableObject
    {
        [Display(Name = "公司全称", GroupName = "企业"), Required(ErrorMessage = "请填写公司全称"), MinLength(2, ErrorMessage = "请填写公司全称")]
        public string? Company { get; set; }

        [Display(Name = "信用代码", Prompt = "18 位", GroupName = "企业"), Required(ErrorMessage = "请填写信用代码")]
        [RegularExpression("^[0-9A-Z]{18}$", ErrorMessage = "统一社会信用代码是 18 位数字或大写字母")]
        public string? CreditCode { get; set; }

        [Display(Name = "供货品类", Prompt = "请选择", GroupName = "企业"), Required(ErrorMessage = "请选择供货品类")]
        public SupplyCategory? Category { get; set; }

        [Display(Name = "联系人", GroupName = "联系与有效期"), Required(ErrorMessage = "请填写联系人")]
        public string? Contact { get; set; }

        [Display(Name = "手机号", GroupName = "联系与有效期"), Required(ErrorMessage = "请填写手机号"), RegularExpression(@"^1\d{10}$", ErrorMessage = "手机号是 11 位数字")]
        public string? Phone { get; set; }

        [Display(Name = "有效期起", GroupName = "联系与有效期"), Required(ErrorMessage = "请选择开始日期")]
        public DateOnly? ValidFrom { get; set; }

        [Display(Name = "有效期止", GroupName = "联系与有效期"), Required(ErrorMessage = "请选择结束日期")]
        public DateOnly? ValidTo { get; set; }

        [Display(Name = "补充说明", GroupName = "确认"), StringLength(200), FormField(Control = SchemaControl.Textarea, FullRow = true)]
        public string? Note { get; set; }

        [Display(Name = "已阅读并同意准入须知", GroupName = "确认"), FormField(Control = SchemaControl.Checkbox)]
        [Range(typeof(bool), "true", "true", ErrorMessage = "请确认已阅读准入须知")]
        public bool Agreed { get; set; }

        /// <summary>跨字段的规则：结束日期要晚于开始日期。</summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ValidFrom is { } from && ValidTo is { } to && to <= from)
                yield return new("结束日期要晚于开始日期", [nameof(ValidTo)]);
        }
    }
}
