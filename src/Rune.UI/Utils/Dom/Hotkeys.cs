namespace Rune.UI;

/// <summary>
/// 快捷键的写法（同 vue-tsx-pro 的 hotkeys）：修饰键（Mod、Ctrl、Alt、Shift、Meta）加一个键，用 + 连接，不分大小写；
/// Mod 在 macOS 上是 ⌘，其他系统是 Ctrl。序列键用空格分隔（<c>"G I"</c>）。按下时的匹配在 JS（hotkeys.js）里。
/// <code>
/// Hotkeys.Format("Mod+Shift+K")            // "Ctrl+Shift+K"（macOS 上 JS 换成 ⌘⇧K，元素写 data-rune-hotkey-label）
/// Hotkeys.AriaKeyShortcut("Mod+K")         // "Control+K Meta+K"
/// </code>
/// </summary>
public static class Hotkeys
{
    static readonly string[] ModifierOrder = ["ctrl", "alt", "shift", "meta"];

    static readonly Dictionary<string, string> Arrows = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ArrowUp"] = "↑",
        ["ArrowDown"] = "↓",
        ["ArrowLeft"] = "←",
        ["ArrowRight"] = "→",
    };

    /// <summary>
    /// 显示的文字：服务器不知道用户的系统，按 Windows 的写法（Ctrl+Shift+K，方向键是箭头）；
    /// 显示它的元素带上 <c>data-rune-hotkey-label="原写法"</c>，macOS 上由 JS 换成符号。
    /// </summary>
    public static string Format(string combo) =>
        string.Join(' ', Steps(combo).Select(static step =>
        {
            var (modifiers, key) = Parse(step);
            var names = modifiers.Select(static m => m switch { "ctrl" => "Ctrl", "alt" => "Alt", "shift" => "Shift", _ => "Win" });
            return string.Join('+', [.. names, Arrows.GetValueOrDefault(key, key)]);
        }));

    /// <summary>
    /// 读屏用的 aria-keyshortcuts：Mod 写成 Control 与 Meta 两种（空格分隔表示“或”，服务器不知道系统）；
    /// 序列键表达不了，返回 null（这时显示的文字别对读屏隐藏）。
    /// </summary>
    public static string? AriaKeyShortcut(string combo)
    {
        var steps = Steps(combo);
        if (steps.Length != 1) return null;
        var parts = steps[0].Split('+', StringSplitOptions.RemoveEmptyEntries);
        var key = KeyName(parts[^1]);
        var lower = parts[..^1].Select(static p => p.ToLowerInvariant()).ToHashSet();
        string Write(bool macOS)
        {
            var names = new List<string>();
            if (lower.Contains("ctrl") || (lower.Contains("mod") && !macOS)) names.Add("Control");
            if (lower.Contains("alt")) names.Add("Alt");
            if (lower.Contains("shift")) names.Add("Shift");
            if (lower.Contains("meta") || (lower.Contains("mod") && macOS)) names.Add("Meta");
            names.Add(key);
            return string.Join('+', names);
        }
        return lower.Contains("mod") ? $"{Write(false)} {Write(true)}" : Write(false);
    }

    static string[] Steps(string combo) => combo.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>一步按键：修饰键（按固定顺序，Mod 在显示时算作 Ctrl）与键名。</summary>
    static (string[] Modifiers, string Key) Parse(string step)
    {
        var parts = step.Split('+', StringSplitOptions.RemoveEmptyEntries);
        var lower = parts[..^1].Select(static p => p.ToLowerInvariant() == "mod" ? "ctrl" : p.ToLowerInvariant()).ToHashSet();
        return ([.. ModifierOrder.Where(lower.Contains)], KeyName(parts[^1]));
    }

    // 键名同 KeyboardEvent.key 的写法：单个字母大写，多个单词的词首大写（arrowup → ArrowUp）
    static string KeyName(string key) => key.ToLowerInvariant() switch
    {
        " " or "space" => "Space",
        "arrowup" => "ArrowUp",
        "arrowdown" => "ArrowDown",
        "arrowleft" => "ArrowLeft",
        "arrowright" => "ArrowRight",
        "pageup" => "PageUp",
        "pagedown" => "PageDown",
        _ when key.Length == 1 => key.ToUpperInvariant(),
        _ => char.ToUpperInvariant(key[0]) + key[1..],
    };
}
