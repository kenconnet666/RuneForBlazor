namespace Rune.UI;

/// <summary>分段输入的一段（同 vue-tsx-pro 的 DateSegmentType；JS 里的名字是 camelCase：dayPeriod）。</summary>
public enum DateSegmentType
{
    /// <summary>年。</summary>
    Year,

    /// <summary>月。</summary>
    Month,

    /// <summary>日。</summary>
    Day,

    /// <summary>季度（1–4）。</summary>
    Quarter,

    /// <summary>一年里的第几周（ISO 8601：周一开始，含 1 月 4 日的那周是第 1 周）。</summary>
    Week,

    /// <summary>时（值总是 0–23，12 小时制时显示 1–12）。</summary>
    Hour,

    /// <summary>分。</summary>
    Minute,

    /// <summary>秒。</summary>
    Second,

    /// <summary>上午 / 下午（12 小时制时自动加上）。</summary>
    DayPeriod,
}

/// <summary>排好的一段：可以输入的段（Type 有值），或分隔符（Literal）。</summary>
readonly record struct SegmentPart(DateSegmentType? Type, string Literal);

/// <summary>各段的值（时是 0–23）；没填的段是 null。</summary>
sealed class DateParts
{
    readonly int?[] values = new int?[8];

    public int? this[DateSegmentType type]
    {
        get => type == DateSegmentType.DayPeriod ? null : values[(int)type];
        set => values[(int)type] = value;
    }

    public int? Year { get => this[DateSegmentType.Year]; set => this[DateSegmentType.Year] = value; }

    public int? Month { get => this[DateSegmentType.Month]; set => this[DateSegmentType.Month] = value; }

    public int? Day { get => this[DateSegmentType.Day]; set => this[DateSegmentType.Day] = value; }

    public int? Quarter { get => this[DateSegmentType.Quarter]; set => this[DateSegmentType.Quarter] = value; }

    public int? Week { get => this[DateSegmentType.Week]; set => this[DateSegmentType.Week] = value; }

    public int? Hour { get => this[DateSegmentType.Hour]; set => this[DateSegmentType.Hour] = value; }

    public int? Minute { get => this[DateSegmentType.Minute]; set => this[DateSegmentType.Minute] = value; }

    public int? Second { get => this[DateSegmentType.Second]; set => this[DateSegmentType.Second] = value; }
}
