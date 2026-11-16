using System.ComponentModel.DataAnnotations;
using Rune.UI;

namespace Rune.Docs.Components.Form.Form.Demo;

public partial class SourcesExample
{
    /// <summary>采购申请。</summary>
    [FormSchema]
    public partial class PurchaseRequest
    {
        [Display(Name = "标题"), Required(ErrorMessage = "请填写标题"), FormField(FullRow = true)]
        public string? Title { get; set; }

        [Display(Name = "成本中心"), Required(ErrorMessage = "请选择成本中心"), FormField(Source = "costCenters")]
        public string? CostCenter { get; set; }

        [Display(Name = "供应商", Prompt = "输入名称搜索"), Required(ErrorMessage = "请选择供应商"), FormField(Source = "suppliers")]
        public string? Supplier { get; set; }

        [Display(Name = "经办人"), MinLength(1, ErrorMessage = "请选择经办人")]
        public IReadOnlyList<PeopleEntry> Handler { get; set; } = [];

        [Display(Name = "附件"), FormField(FullRow = true)]
        public IReadOnlyList<UploadFile> Files { get; set; } = [];
    }
}
