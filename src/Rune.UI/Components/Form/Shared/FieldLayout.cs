using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 一组字段共同的布局与状态（同 vue-tsx-pro 的 field-layout）：标签放在上方还是起始边、标签宽度与对齐、整组只读或不可用。
/// Form、FormSection 设置（级联值），Field 与控件读取；层层叠加：标签的三项里层优先，没写的沿用外层；
/// 不可用与只读只要有一层是 true 就是 true（不可用的表单里没有可用的分组）。Field 与控件上显式写的优先。
/// </summary>
/// <param name="LabelPlacement">标签放在哪；null 时沿用外层，最外层默认上方。</param>
/// <param name="LabelWidth">标签在起始边时标签列的宽度；null 时沿用外层，最外层默认 8em。</param>
/// <param name="LabelAlign">标签在起始边时文字的对齐；null 时沿用外层，最外层默认靠结束边。</param>
/// <param name="Disabled">整组不可用。</param>
/// <param name="ReadOnly">整组只读（查看详情、审批时）。</param>
public sealed record FieldLayout(
    FieldLabelPlacement? LabelPlacement = null,
    CssLength? LabelWidth = null,
    FieldLabelAlign? LabelAlign = null,
    bool Disabled = false,
    bool ReadOnly = false)
{
    /// <summary>叠在外层的设置上。</summary>
    public FieldLayout Over(FieldLayout? outer) => outer is null ? this : new(
        LabelPlacement ?? outer.LabelPlacement,
        LabelWidth ?? outer.LabelWidth,
        LabelAlign ?? outer.LabelAlign,
        Disabled || outer.Disabled,
        ReadOnly || outer.ReadOnly);
}
