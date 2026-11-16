using System.ComponentModel.DataAnnotations;
using Rune.UI;

namespace Rune.Docs.Components.Form.Form.Demo;

public partial class ConditionalExample
{
    /// <summary>费用类型。</summary>
    public enum ExpenseKind
    {
        [Display(Name = "差旅")] Travel,
        [Display(Name = "招待")] Meal,
        [Display(Name = "其他")] Other,
    }

    /// <summary>报销单。有条件的字段不写 [Required]，必填由 RequiredWhen 负责。</summary>
    [FormSchema]
    public partial class ExpenseClaim
    {
        [Display(Name = "费用类型"), Required(ErrorMessage = "请选择费用类型"), FormField(Control = SchemaControl.Radio)]
        public ExpenseKind? Kind { get; set; }

        [Display(Name = "出差城市"), FormField(VisibleWhen = nameof(IsTravel))]
        public string? City { get; set; }

        [Display(Name = "招待人数"), FormField(Unit = "人", VisibleWhen = nameof(IsMeal), RequiredWhen = nameof(IsMeal))]
        public int? Guests { get; set; }

        [Display(Name = "金额"), Required(ErrorMessage = "请填写金额"), FormField(Unit = "元")]
        public decimal? Amount { get; set; }

        // 超过 5000 要写说明
        [Display(Name = "说明"), FormField(Control = SchemaControl.Textarea, FullRow = true, RequiredWhen = nameof(OverLimit))]
        public string? Note { get; set; }

        [Display(Name = "核定金额"), FormField(Unit = "元")]
        public decimal? Approved { get; set; }

        [Display(Name = "明细"), FormField(Control = SchemaControl.Table)]
        public List<ExpenseLine> Lines { get; set; } = [];

        public bool IsTravel => Kind == ExpenseKind.Travel;

        public bool IsMeal => Kind == ExpenseKind.Meal;

        public bool OverLimit => Amount > 5000;
    }

    /// <summary>明细的一行。</summary>
    [FormSchema]
    public partial class ExpenseLine
    {
        [Display(Name = "项目"), Required(ErrorMessage = "请填写项目")]
        public string? Item { get; set; }

        [Display(Name = "金额"), Required(ErrorMessage = "请填写金额"), FormField(Unit = "元", Sum = true)]
        public decimal? Cost { get; set; }
    }
}
