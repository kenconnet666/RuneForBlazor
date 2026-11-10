namespace Rune.UI;

/// <summary>
/// 桌面窗口的文案（窗口按钮的名称，读屏软件读它）。Rune 独有、vue-tsx-pro 没有，所以不在生成的语言包里：
/// 按语言包的语言取，改几条用 <c>Lang.ZhCn with { Desktop = Lang.ZhCn.Desktop with { … } }</c>。
/// </summary>
public sealed record DesktopMessages
{
    /// <summary>最小化。</summary>
    public required string Minimize { get; init; }

    /// <summary>最大化。</summary>
    public required string Maximize { get; init; }

    /// <summary>还原（最大化之后）。</summary>
    public required string Restore { get; init; }

    /// <summary>关闭。</summary>
    public required string Close { get; init; }

    /// <summary>窗口按钮那一组的名称。</summary>
    public required string Controls { get; init; }

    /// <summary>中文。</summary>
    public static DesktopMessages ZhCn { get; } = new() { Minimize = "最小化", Maximize = "最大化", Restore = "还原", Close = "关闭", Controls = "窗口" };

    /// <summary>英文。</summary>
    public static DesktopMessages EnUs { get; } = new() { Minimize = "Minimize", Maximize = "Maximize", Restore = "Restore", Close = "Close", Controls = "Window" };

    /// <summary>按语言包的语言代码取：中文的用中文，其余用英文。</summary>
    public static DesktopMessages For(string code) => code.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? ZhCn : EnUs;
}

public sealed partial record Lang
{
    /// <summary>桌面窗口的文案；不写时按 <see cref="Code"/> 取（<see cref="DesktopMessages.For"/>）。</summary>
    public DesktopMessages Desktop { get => field ?? DesktopMessages.For(Code); init; }
}
