using System.ComponentModel.DataAnnotations;
using Rune.UI;

namespace Rune.Docs.Components.Form.Form.Demo;

public partial class ReadViewExample
{
    /// <summary>交通方式。</summary>
    public enum Transport
    {
        [Display(Name = "火车")] Train,
        [Display(Name = "飞机")] Plane,
        [Display(Name = "自驾")] Car,
    }

    /// <summary>出差申请。</summary>
    [FormSchema]
    public partial class TripRequest
    {
        [Display(Name = "出差事由"), Required(ErrorMessage = "请填写事由"), FormField(FullRow = true)]
        public string? Title { get; set; }

        [Display(Name = "目的地"), Required(ErrorMessage = "请填写城市"), FormField(Tab = "行程")]
        public string? City { get; set; }

        [Display(Name = "天数"), Required(ErrorMessage = "请填写天数"), FormField(Unit = "天", Tab = "行程")]
        public int? Days { get; set; }

        [Display(Name = "交通方式"), FormField(Control = SchemaControl.Radio, Tab = "行程")]
        public Transport Transport { get; set; }

        [Display(Name = "预支借款"), FormField(Tab = "费用")]
        public bool Advance { get; set; }

        [Display(Name = "预算明细"), FormField(Control = SchemaControl.Table, Tab = "费用")]
        public List<ConditionalExample.ExpenseLine> Lines { get; set; } = [];

        // 只给财务看：查看时按权限不列
        [Display(Name = "借款账户"), FormField(Tab = "费用")]
        public string? Account { get; set; }
    }
}
