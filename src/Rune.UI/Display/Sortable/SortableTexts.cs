using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Rune.UI;

/// <summary>
/// 拖动排序时读屏念的句子，交给 JS（data-texts）：Sortable 与 Upload 的图片墙共用。
/// 名称是 {label}，位置与总数是 900001、900002（JS 换成实际的数）；每个语言包算一次。
/// </summary>
internal static class SortableTexts
{
    static readonly ConditionalWeakTable<Lang, string> cache = [];

    internal static string Of(Lang lang) => cache.GetValue(lang, static lang => JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["grabbed"] = lang.SortableGrabbed("{label}", 900001, 900002),
        ["moved"] = lang.SortableMoved("{label}", 900001, 900002),
        ["dropped"] = lang.SortableDropped("{label}", 900001, 900002),
        ["cancelled"] = lang.SortableCancelled("{label}", 900001),
    }, UIJson.Relaxed.DictionaryStringString));
}
