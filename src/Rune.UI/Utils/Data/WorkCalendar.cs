namespace Rune.UI;

/// <summary>
/// 工作日历（同 vue-tsx-pro 的 createWorkCalendar）：周末、法定节假日与调休补班。节假日每年发布一次，组件库不内置：
/// 应用把数据交给 <see cref="RuneUIOptions.WorkCalendar"/> 或 <see cref="RuneProvider"/> 的 WorkCalendar，组件经 <see cref="UIConfig.WorkCalendar"/> 取用。
/// <code>
/// new WorkCalendar
/// {
///     Holidays = [.. WorkCalendar.Range(new(2026, 10, 1), new(2026, 10, 8))],   // 放假
///     Workdays = [new(2026, 9, 27), new(2026, 10, 10)],                          // 调休上班的周末
/// }
/// </code>
/// </summary>
public sealed class WorkCalendar
{
    /// <summary>只按周末（周六、周日）算：没配置工作日历时用它。</summary>
    public static WorkCalendar Weekdays { get; } = new();

    /// <summary>休息的星期几，默认周六、周日。</summary>
    public IReadOnlyCollection<DayOfWeek> Weekend { get; init; } = [DayOfWeek.Saturday, DayOfWeek.Sunday];

    readonly HashSet<DateOnly> holidays = [];
    readonly HashSet<DateOnly> workdays = [];

    /// <summary>放假的日子（区间用 <see cref="Range"/> 展开）。</summary>
    public IReadOnlyCollection<DateOnly> Holidays { get => holidays; init => holidays = [.. value]; }

    /// <summary>调休上班的日子（落在周末的补班）。</summary>
    public IReadOnlyCollection<DateOnly> Workdays { get => workdays; init => workdays = [.. value]; }

    /// <summary>从 start 到 end（含两端）的每一天：写放假区间用。</summary>
    public static IEnumerable<DateOnly> Range(DateOnly start, DateOnly end)
    {
        for (var day = start; day <= end; day = day.AddDays(1)) yield return day;
    }

    /// <summary>这一天上不上班。</summary>
    public bool IsWorkday(DateOnly day) => workdays.Contains(day) || (!holidays.Contains(day) && !Weekend.Contains(day.DayOfWeek));

    /// <summary>from 之后到 to（含）有几个工作日：周五到下周一是 1；to 在 from 之前时为负数（同 date-fns 的方向）。</summary>
    public int WorkdaysBetween(DateOnly from, DateOnly to)
    {
        if (to < from) return -WorkdaysBetween(to, from);
        var count = 0;
        for (var day = from.AddDays(1); day <= to; day = day.AddDays(1))
            if (IsWorkday(day)) count++;
        return count;
    }

    /// <summary>从 from 起数 count 个工作日后的那一天（count 为负往前数，为 0 时原样）。</summary>
    public DateOnly AddWorkdays(DateOnly from, int count)
    {
        var step = count >= 0 ? 1 : -1;
        var day = from;
        for (var left = Math.Abs(count); left > 0;)
        {
            day = day.AddDays(step);
            if (IsWorkday(day)) left--;
        }
        return day;
    }

    /// <summary>
    /// 算时限：截止日、还剩几个工作日、状态。是否超期按日期比：周五截止、周六还没完成也算超期（虽然中间没有工作日）。
    /// </summary>
    /// <param name="due">截止日。</param>
    /// <param name="today">今天。</param>
    /// <param name="done">完成的日期；完成了按完成那天算，状态是 <see cref="DeadlineState.Done"/>。</param>
    /// <param name="soon">剩下几个工作日以内算快到期。</param>
    public Deadline DeadlineOf(DateOnly due, DateOnly today, DateOnly? done = null, int soon = 2)
    {
        var at = done ?? today;
        var remaining = WorkdaysBetween(at, due);
        var state = done is not null ? DeadlineState.Done
            : at > due ? DeadlineState.Overdue
            : at == due ? DeadlineState.Due
            : remaining <= soon ? DeadlineState.Soon
            : DeadlineState.Pending;
        return new Deadline(due, remaining, state);
    }
}

/// <summary>时限的状态。</summary>
public enum DeadlineState
{
    /// <summary>还早。</summary>
    Pending,

    /// <summary>快到期：剩下的工作日不多于 soon。</summary>
    Soon,

    /// <summary>今天到期。</summary>
    Due,

    /// <summary>超期。</summary>
    Overdue,

    /// <summary>已完成（按完成那天算剩余或超期）。</summary>
    Done,
}

/// <summary>时限的结果。</summary>
/// <param name="Due">截止日。</param>
/// <param name="Remaining">离截止还有几个工作日：截止当天为 0，超期为负；完成了按完成那天算。</param>
/// <param name="State">状态。</param>
public sealed record Deadline(DateOnly Due, int Remaining, DeadlineState State);
