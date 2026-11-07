using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Rune.UI;

/// <summary>
/// 时间选择器（同 vue-tsx-pro 的 TimePicker）：TimeField 加一个打开时间列表的按钮，按 Step 列出可选的时间（同 Google 日历的时间下拉）；
/// 可以直接键入，也可以点选。
/// <code>
/// &lt;Field Label="开会时间"&gt;&lt;TimePicker @bind-Value="time" Min="new(9, 0)" Max="new(18, 0)" /&gt;&lt;/Field&gt;
/// &lt;TimePicker @bind-Value="time" Step="15" aria-label="提醒时间" /&gt;
/// </code>
/// 列表从 Min（默认 00:00）起每 Step 分钟一项，到 Max 为止，按语言的 12 / 24 小时制显示；不在列表里的时间（14:47）照样能键入，
/// IsTimeUnavailable 的时间在列表里不可用、键入时显示出错。列表下方“此刻”选当前时间（此刻不可选时不可用）。
/// 列表是 Listbox（↑↓、Home / End、按数字跳转）；选好后关闭，焦点回到按钮；Esc、点外部关闭，分段里 Alt + ↓ 打开。
/// </summary>
/// <typeparam name="TValue">值的类型：TimeOnly 或 TimeOnly?。</typeparam>
public sealed class TimePicker<TValue> : TimeField<TValue>
{
    internal readonly string PopoverId = UniqueId.Next("time-picker");
    bool open;

    [Inject] IServiceProvider Services { get; set; } = null!;

    /// <summary>列表的间隔（分钟），默认 30。</summary>
    [Parameter] public int Step { get; set; } = 30;

    /// <summary>弹出位置，默认下方、与起始边对齐；空间不够时翻到另一侧。</summary>
    [Parameter] public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>打开按钮（IconButton）的部件属性。</summary>
    [Parameter] public Attrs? PropsButton { get; set; }

    /// <summary>弹层的部件属性。</summary>
    [Parameter] public Attrs? PropsPopover { get; set; }

    /// <summary>时间列表（Listbox）的部件属性。</summary>
    [Parameter] public Attrs? PropsList { get; set; }

    /// <summary>“此刻”按钮的部件属性。</summary>
    [Parameter] public Attrs? PropsNow { get; set; }

    internal bool IsOpen => open;

    internal Lang Lang => Config.Lang;

    internal string AnchorId => ShellId;

    /// <summary>列表的选项：Min 到 Max，每 Step 分钟一项，按语言（或 HourCycle）的小时制显示。</summary>
    internal IReadOnlyList<ListboxOption> Options
    {
        get
        {
            var lang = HourCycle is { } cycle ? Lang with { HourCycle = cycle == UI.HourCycle.H12 ? "h12" : "h23" } : Lang;
            var first = (int)(Min ?? TimeOnly.MinValue).ToTimeSpan().TotalMinutes;
            var last = (int)(Max ?? new TimeOnly(23, 59)).ToTimeSpan().TotalMinutes;
            var options = new List<ListboxOption>();
            for (var minutes = first; minutes <= last; minutes += Math.Max(Step, 1))
            {
                var time = new TimeOnly(minutes / 60, minutes % 60);
                options.Add(new(Iso(time), LangFormat.Date(time, lang, null, TimeStyle.Short), IsTimeUnavailable?.Invoke(time) == true));
            }
            return options;
        }
    }

    /// <summary>当前值正好是列表里的一项时高亮它（只比时分）。</summary>
    internal string? Selected => CurrentValue is TimeOnly time ? Iso(time) : null;

    /// <summary>此刻（精确到分，语言的时区）。</summary>
    internal TimeOnly Now
    {
        get
        {
            var clock = Services.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
            var now = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone);
            return new TimeOnly(now.Hour, now.Minute);
        }
    }

    /// <summary>这个时间能不能选：在 Min–Max 之内，不是不可选的时间。</summary>
    internal bool Selectable(TimeOnly time) => !IsOutOfRange(Iso(time));

    static string Iso(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>选了一个时间（列表、此刻）：写回，关闭弹层。</summary>
    internal async Task PickAsync(TimeOnly time)
    {
        if (!Editable) return;
        if (TryFromIso(time.ToString(Granularity == TimeGranularity.Second ? "HH:mm:ss" : "HH:mm", CultureInfo.InvariantCulture), out var value)) await WriteAsync(value);
        open = false;
    }

    internal void Toggled(OverlayToggleEventArgs e) => open = e.Open;

    private protected override RenderFragment Trigger => PickerParts.Trigger(BuiltInIcons.Clock, Lang.TimePickerOpen, Editable, open, PopoverId, InputStyles.Of(Appearance, Config.Styles)[InputPart.Button], PropsButton);

    private protected override RenderFragment Popover => builder =>
    {
        builder.OpenComponent<TimePickerPopover<TValue>>(0);
        builder.AddComponentParameter(1, nameof(TimePickerPopover<TValue>.Picker), this);
        builder.CloseComponent();
    };
}
