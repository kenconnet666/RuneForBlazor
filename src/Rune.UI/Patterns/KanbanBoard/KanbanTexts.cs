using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Rune.UI;

/// <summary>
/// 看板拖动时读屏念的句子，交给 JS（data-texts）。卡片名是 {label}，列名是 {group}，原因是 {reason}，位置是 900001（JS 换成实际的值）；
/// 每个语言包算一次。
/// </summary>
internal static class KanbanTexts
{
    static readonly ConditionalWeakTable<Lang, string> cache = [];

    internal static string Of(Lang lang) => cache.GetValue(lang, static lang => JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["grabbed"] = lang.SortableGroupGrabbed("{label}", "{group}", 900001),
        ["over"] = lang.SortableOver("{label}", "{group}", 900001, null),
        ["denied"] = lang.SortableOver("{label}", "{group}", 900001, "{reason}"),
        ["dropped"] = lang.SortableGroupDropped("{label}", "{group}", 900001),
        ["cancelled"] = lang.SortableGroupCancelled("{label}", "{group}", 900001),
        ["rejected"] = lang.SortableRejected("{label}", "{reason}"),
    }, UIJson.Relaxed.DictionaryStringString));
}
