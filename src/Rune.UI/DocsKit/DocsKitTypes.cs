namespace Rune.UI;

/// <summary>CodePreview 效果区的排列（同 vue-tsx-pro 的 CodePreviewAligns）。</summary>
public enum CodePreviewAlign
{
    /// <summary>从起始边排起、占满宽度（默认）：表单、卡片、长内容。</summary>
    Start,

    /// <summary>居中：单个按钮、图标这类小东西。</summary>
    Center,
}

/// <summary>CodeGroup 里的一段代码（同 vue-tsx-pro 的 CodeGroupItem）。</summary>
/// <param name="Label">标签上的文字；也是这一段的值（没给 Value 时）。</param>
/// <param name="Code">代码文本。</param>
/// <param name="Lang">语言（见 CodeBlock 的 Lang）。</param>
public sealed record CodeGroupItem(string Label, string Code, string? Lang = null)
{
    /// <summary>这一段的值，默认等于 Label。</summary>
    public string? Value { get; init; }

    /// <summary>这一段代码块（CodeBlock 组件）的部件属性：行号、行高亮……</summary>
    public Attrs? Props { get; init; }

    /// <summary>标签与面板对应的值。</summary>
    public string Key => Value ?? Label;
}
