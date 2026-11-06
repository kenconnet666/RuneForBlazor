using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>清单一项的状态。</summary>
public enum ChecklistStatus
{
    /// <summary>待办（默认）。</summary>
    Todo,

    /// <summary>完成。</summary>
    Done,

    /// <summary>超时：过了计划日期还没完成。</summary>
    Overdue,

    /// <summary>受阻：等别人、缺前提。</summary>
    Blocked,

    /// <summary>不适用：这一次不用做，不算进进度。</summary>
    Skipped,
}

/// <summary>清单的排法。</summary>
public enum ChecklistLayout
{
    /// <summary>一项一行（默认）：状态、名称与说明、负责人 · 日期 · 证据，操作在结束边。</summary>
    List,

    /// <summary>一行排开、放不下换行：状态、名称、日期、证据。</summary>
    Inline,

    /// <summary>只有一排状态图标：表格的格子里；悬停与读屏有每一项的说明。</summary>
    Icons,
}

/// <summary>完成清单的一项（同 vue-tsx-pro 的 ChecklistItem）。</summary>
/// <param name="Label">名称。</param>
public sealed record ChecklistItem(string Label)
{
    /// <summary>唯一的 key；不写时用名称。</summary>
    public string? Key { get; init; }

    /// <summary>状态，默认待办。</summary>
    public ChecklistStatus Status { get; init; }

    /// <summary>说明：怎么做、要什么材料、缺的由谁出具。</summary>
    public string? Description { get; init; }

    /// <summary>负责人（谁处理、谁出具）。</summary>
    public string? Owner { get; init; }

    /// <summary>日期：完成的日期，或计划日期（超时时用危险色）。</summary>
    public string? Date { get; init; }

    /// <summary>证据：附件、报告的链接。</summary>
    public RenderFragment? Evidence { get; init; }

    /// <summary>操作：上传证据、去处理、催办（结束边）。</summary>
    public RenderFragment? Action { get; init; }
}

/// <summary>勾选了清单的一项：哪一项、勾上（完成）还是取消。</summary>
/// <param name="Item">那一项。</param>
/// <param name="Done">新的完成状态。</param>
public sealed record ChecklistCheck(ChecklistItem Item, bool Done);
