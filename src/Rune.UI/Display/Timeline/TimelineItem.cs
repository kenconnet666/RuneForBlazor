using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 时间线上的一项（同 vue-tsx-pro 的 TimelineItem）。文字字段直接写；要放组件时用对应的 RenderFragment 字段，Razor 里写成
/// <c>DescriptionContent = @&lt;FieldCompare … /&gt;</c>。
/// </summary>
/// <param name="Title">标题。</param>
public sealed record TimelineItem(string Title)
{
    /// <summary>带格式的标题（版本号加“最新”徽标）：代替 Title 显示。</summary>
    public RenderFragment? TitleContent { get; init; }

    /// <summary>唯一的 key；不写时用位置。</summary>
    public string? Key { get; init; }

    /// <summary>时间（显示的文字）。</summary>
    public string? Time { get; init; }

    /// <summary>机器可读的时间（ISO 8601），写到 time 元素的 datetime 上。</summary>
    public string? DateTime { get; init; }

    /// <summary>说明（一段文字）。</summary>
    public string? Description { get; init; }

    /// <summary>带格式的说明（字段差异 FieldCompare、附件链接）：代替 Description 显示。</summary>
    public RenderFragment? DescriptionContent { get; init; }

    /// <summary>操作人：显示在时间前面，没有头像时显示首字母。</summary>
    public string? Actor { get; init; }

    /// <summary>操作人的头像地址。</summary>
    public string? Avatar { get; init; }

    /// <summary>收起的详情：项里一个“查看详情”的按钮，点开才渲染、显示。</summary>
    public RenderFragment? Details { get; init; }

    /// <summary>节点的图标；不写时是圆点。</summary>
    public IconData? Icon { get; init; }

    /// <summary>节点的色调，默认 Neutral。</summary>
    public Tone? Tone { get; init; }
}
