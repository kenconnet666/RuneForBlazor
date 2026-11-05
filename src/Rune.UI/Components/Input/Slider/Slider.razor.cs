using System.Globalization;
using System.Numerics;
using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>滑块：单值绑 Value，范围绑 Range（<see cref="NumberRange{T}"/>）。用法见 Slider.razor 顶部的说明。</summary>
/// <typeparam name="TValue">数值类型。</typeparam>
public partial class Slider<TValue>
{
    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    readonly string partLabelId = UniqueId.Next("slider-part");
    Memo<(TValue Min, TValue Max, TValue Step, TValue? BigStep, Orientation Orientation, string Values, double Start, double End, long Ack, bool Disabled, bool ReadOnly), Attrs> rootManaged;
    // 处理过的 JS 序号（见 SlideEventArgs.Seq）
    long acknowledged;

    /// <summary>最小值，默认 0。</summary>
    [Parameter] public TValue Min { get; set; } = TValue.Zero;

    /// <summary>最大值，默认 100。</summary>
    [Parameter] public TValue Max { get; set; } = TValue.CreateChecked(100);

    /// <summary>步长，默认 1；取值总是对齐到 Min + n × Step。</summary>
    [Parameter] public TValue Step { get; set; } = TValue.One;

    /// <summary>Page Up / Page Down 的步长；不写时是范围的十分之一（至少一个 Step）。</summary>
    [Parameter] public TValue? BigStep { get; set; }

    /// <summary>方向，默认横排（起始端是最小值）；竖排时底端是最小值，高度默认 10em。</summary>
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>已选范围与滑块描边的色调，默认品牌色。</summary>
    [Parameter] public Tone Tone { get; set; } = Tone.Accent;

    /// <summary>字号档位，滑块与轨道随之缩放；不写时继承。</summary>
    [Parameter] public Size? Size { get; set; }

    /// <summary>刻度：数字或带文字的 <c>new(500, "500")</c>；有文字时滑块下方（竖排时右侧）留出位置。</summary>
    [Parameter] public IReadOnlyList<SliderMark<TValue>> Marks { get; set; } = [];

    /// <summary>读屏念的取值文字（aria-valuetext），如 <c>v => $"{v}%"</c>；不写时念数字。</summary>
    [Parameter] public Func<TValue, string>? ValueText { get; set; }

    /// <summary>表单字段名：给了就带隐藏的 input（范围时两个同名），随表单提交。</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>范围（<c>@bind-Range</c>）：给了就是范围选择，两个滑块，Value 不再使用。</summary>
    [Parameter] public NumberRange<TValue>? Range { get; set; }

    /// <summary>范围变了。</summary>
    [Parameter] public EventCallback<NumberRange<TValue>> RangeChanged { get; set; }

    /// <summary>单值时一次操作结束（松开、按键）时的值：保存、发请求放在这里，不必每次移动都发。</summary>
    [Parameter] public EventCallback<TValue> OnValueCommit { get; set; }

    /// <summary>范围时一次操作结束时的范围。</summary>
    [Parameter] public EventCallback<NumberRange<TValue>> OnRangeCommit { get; set; }

    /// <summary>轨道的部件属性。</summary>
    [Parameter] public Attrs? PropsTrack { get; set; }

    /// <summary>已选范围的部件属性；位置与长度由组件写。</summary>
    [Parameter] public Attrs? PropsRange { get; set; }

    /// <summary>刻度点的部件属性，按刻度计算。</summary>
    [Parameter] public Func<SliderMark<TValue>, Attrs?>? PropsMark { get; set; }

    /// <summary>刻度文字的部件属性，按刻度计算。</summary>
    [Parameter] public Func<SliderMark<TValue>, Attrs?>? PropsMarkLabel { get; set; }

    Bindable<NumberRange<TValue>> range { get => field ??= new(() => Range ?? default, () => RangeChanged); }

    // 范围选择：给了 Range（初始值或绑定）
    bool IsRange => Range is not null || RangeChanged.HasDelegate;

    /// <summary>各滑块现在的值（单值时一个，范围时低、高两个）。</summary>
    IReadOnlyList<TValue> Thumbs => IsRange ? [range.Value.Low, range.Value.High] : [CurrentValue];

    bool Labelled => Marks.Any(static mark => mark.Label is not null);

    // 父级传入超出范围的值时按范围内显示（不改父级的值），滑块不会跑出轨道
    IReadOnlyList<TValue> Shown => [.. Thumbs.Select(value => TValue.Clamp(value, Min, Max))];

    /// <summary>第 index 个滑块能取的范围：范围时不能越过另一个（同 WAI-ARIA 多滑块）。</summary>
    (TValue Lower, TValue Upper) Bounds(int index, IReadOnlyList<TValue> values) =>
        values.Count < 2 ? (Min, Max) : index == 0 ? (Min, values[1]) : (values[0], Max);

    double PercentOf(TValue value) =>
        Max > Min ? Math.Round(double.CreateSaturating(value - Min) / double.CreateSaturating(Max - Min) * 100, 4) : 0;

    static string Format(TValue value) => value.ToString(null, Invariant);

    // 范围时滑块名称后面的“最小值 / 最大值”：用 Field 的标签时连上这段隐藏的文字（写了 aria-label 时直接拼在后面）
    string? PartLabelId(int index, int count) =>
        count > 1 && !HasOwnLabel && Field?.LabelId is not null ? $"{partLabelId}-{index}" : null;

    bool HasOwnLabel => AdditionalAttributes?.ContainsKey("aria-label") == true;

    // 外层：交给 JS 的取值规则、当前的值与位置、处理到的序号、状态，接住 JS 发来的值。
    // data-values 只由 C# 写：JS 在 C# 追上之后按它复原（父级没有采纳改动时，旧树里的值没变、Blazor 不会重写被 JS 改过的属性）
    Attrs RootManaged(IReadOnlyList<TValue> shown)
    {
        var start = shown.Count > 1 ? PercentOf(shown[0]) : 0;
        var end = PercentOf(shown[^1]);
        var values = string.Join(' ', shown.Select(Format));
        return rootManaged.Get((Min, Max, Step, BigStep, Orientation, values, start, end, acknowledged, IsDisabled, IsReadOnly), this, static (k, owner) => new Attrs
        {
            // 0 也带单位：calc(终点 - 起点) 里不带单位的 0 与百分比相减是无效的
            Vars = [SliderStyles.Start.Set(Percent(k.Start)), SliderStyles.End.Set(Percent(k.End))],
            ["data-rune-slider"] = true,
            ["data-min"] = Format(k.Min),
            ["data-max"] = Format(k.Max),
            ["data-step"] = Format(k.Step),
            ["data-big-step"] = k.BigStep is { } big ? Format(big) : null,
            ["data-orientation"] = k.Orientation == Orientation.Vertical ? "vertical" : "horizontal",
            ["data-range"] = k.Values.Contains(' ') ? true : null,
            ["data-values"] = k.Values,
            ["data-ack"] = k.Ack.ToString(Invariant),
            ["data-disabled"] = k.Disabled ? true : null,
            ["data-readonly"] = k.ReadOnly ? true : null,
            ["onruneslide"] = EventCallback.Factory.Create<SlideEventArgs>(owner, owner.SlideAsync),
        });
    }

    // 滑块：role="slider" 与取值、状态、名称；第一个拿 Field 的 id
    Attrs ThumbManaged(int index, IReadOnlyList<TValue> shown)
    {
        var value = shown[index];
        var (lower, upper) = Bounds(index, shown);
        var range = shown.Count > 1;
        var part = range ? (index == 0 ? Config.Lang.SliderMin : Config.Lang.SliderMax) : null;
        var ownLabel = HasOwnLabel ? AdditionalAttributes!["aria-label"] as string : null;
        var labelledBy = ownLabel is null ? Field?.LabelId : null;
        return new Attrs
        {
            Id = index == 0 ? ControlId : null,
            Role = "slider",
            ["tabindex"] = IsDisabled ? null : "0",
            ["aria-valuemin"] = Format(lower),
            ["aria-valuemax"] = Format(upper),
            ["aria-valuenow"] = Format(value),
            ["aria-valuetext"] = ValueText?.Invoke(value),
            ["aria-orientation"] = Orientation == Orientation.Vertical ? "vertical" : "horizontal",
            AriaDisabled = IsDisabled ? true : null,
            ["aria-readonly"] = IsReadOnly ? "true" : null,
            ["aria-invalid"] = IsInvalid ? "true" : null,
            ["aria-label"] = part is not null && ownLabel is not null ? $"{ownLabel} {part}" : ownLabel,
            AriaLabelledBy = PartLabelId(index, shown.Count) is { } partId ? $"{labelledBy} {partId}" : labelledBy,
            AriaDescribedBy = Field?.DescribedBy,
            ["data-index"] = index.ToString(Invariant),
            Sx = SliderStyles.ThumbAt(range && index == 0, Orientation),
        };
    }

    // 刻度点：位置，在已选范围里时 data-active（拖动时 JS 跟着改）
    Attrs MarkManaged(SliderMark<TValue> mark, IReadOnlyList<TValue> shown)
    {
        var (low, high) = shown.Count > 1 ? (shown[0], shown[1]) : (Min, shown[0]);
        var active = mark.Value >= low && mark.Value <= high;
        return new Attrs
        {
            Vars = [SliderStyles.MarkAt.Set(Percent(PercentOf(mark.Value)))],
            ["data-value"] = Format(mark.Value),
            ["data-active"] = active ? true : null,
        };
    }

    Attrs MarkPosition(SliderMark<TValue> mark) => new() { Vars = [SliderStyles.MarkAt.Set(Percent(PercentOf(mark.Value)))] };

    static CssLength Percent(double value) => $"{value.ToString(Invariant)}%";

    // JS 发来的值：再夹一次范围、对齐步长（不信任客户端），变了才写回；一次操作结束时调用 OnValueCommit / OnRangeCommit
    async Task SlideAsync(SlideEventArgs e)
    {
        acknowledged = Math.Max(acknowledged, e.Seq);
        var shown = Shown;
        if (IsDisabled || IsReadOnly || e.Index < 0 || e.Index >= shown.Count) return;
        if (!decimal.TryParse(e.Value, NumberStyles.Number, Invariant, out var raw)) return;
        var (lower, upper) = Bounds(e.Index, shown);
        var min = decimal.CreateSaturating(Min);
        var step = decimal.CreateSaturating(Step);
        // 与 JS 的 Math.round 一致：正好一半时往大取（Math.Round 默认是银行家舍入）
        var snapped = step > 0 ? min + Math.Floor((raw - min) / step + 0.5m) * step : raw;
        var value = TValue.Clamp(TValue.CreateSaturating(snapped), lower, upper);
        if (IsRange)
        {
            var next = e.Index == 0 ? new NumberRange<TValue>(value, shown[1]) : new NumberRange<TValue>(shown[0], value);
            if (range.Value != next) await range.SetAsync(next);
            if (e.Commit) await OnRangeCommit.InvokeAsync(next);
            return;
        }
        if (value != CurrentValue) await SetValueAsync(value);
        if (e.Commit) await OnValueCommit.InvokeAsync(value);
    }
}

/// <summary>滑块轨道上的一个刻度；数字可以直接写（隐式转换）。</summary>
/// <param name="Value">刻度所在的值。</param>
/// <param name="Label">刻度下方（竖排时右侧）的文字。</param>
/// <typeparam name="TValue">数值类型。</typeparam>
public readonly record struct SliderMark<TValue>(TValue Value, string? Label = null) where TValue : struct, INumber<TValue>
{
    /// <summary>只有位置、没有文字的刻度。</summary>
    public static implicit operator SliderMark<TValue>(TValue value) => new(value);
}
