namespace Rune.UI;

/// <summary>分段输入的一段（JS 里的名字是 camelCase：dayPeriod）。</summary>
enum SegmentType
{
    Year,
    Month,
    Day,
    Quarter,
    Week,
    Hour,
    Minute,
    Second,
    DayPeriod,
}

/// <summary>排好的一段：可以输入的段（Type 有值），或分隔符（Literal）。</summary>
readonly record struct SegmentPart(SegmentType? Type, string Literal);

/// <summary>各段的值（时是 0–23）；没填的段是 null。</summary>
sealed class DateParts
{
    readonly int?[] values = new int?[8];

    public int? this[SegmentType type]
    {
        get => type == SegmentType.DayPeriod ? null : values[(int)type];
        set => values[(int)type] = value;
    }

    public int? Year { get => this[SegmentType.Year]; set => this[SegmentType.Year] = value; }

    public int? Month { get => this[SegmentType.Month]; set => this[SegmentType.Month] = value; }

    public int? Day { get => this[SegmentType.Day]; set => this[SegmentType.Day] = value; }

    public int? Quarter { get => this[SegmentType.Quarter]; set => this[SegmentType.Quarter] = value; }

    public int? Week { get => this[SegmentType.Week]; set => this[SegmentType.Week] = value; }

    public int? Hour { get => this[SegmentType.Hour]; set => this[SegmentType.Hour] = value; }

    public int? Minute { get => this[SegmentType.Minute]; set => this[SegmentType.Minute] = value; }

    public int? Second { get => this[SegmentType.Second]; set => this[SegmentType.Second] = value; }
}
