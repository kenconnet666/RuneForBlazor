namespace Rune.UI;

/// <summary>日历一格的状态（自定义日期格的内容拿到它）。</summary>
/// <param name="Date">这一天。</param>
/// <param name="Selected">已选（区间里是两端与中间的每一天）。</param>
/// <param name="Disabled">不能选：范围外、不可选的日期。</param>
/// <param name="Today">今天。</param>
/// <param name="Outside">补齐的上下月。</param>
/// <param name="InRange">在已选的区间里（含两端）。</param>
public readonly record struct CalendarDay(DateOnly Date, bool Selected, bool Disabled, bool Today, bool Outside, bool InRange);
