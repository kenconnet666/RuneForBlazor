namespace Rune.UI;

/// <summary>日期输入精确到哪一段（同 vue-tsx-pro 的 DateFieldGranularity）。</summary>
public enum DateGranularity
{
    /// <summary>只有年。</summary>
    Year,

    /// <summary>年月（同原生 input type="month"）。</summary>
    Month,

    /// <summary>年月日（默认）。</summary>
    Day,

    /// <summary>年与季度。</summary>
    Quarter,

    /// <summary>年与周（ISO 8601，周一开始；同原生 input type="week"）。</summary>
    Week,

    /// <summary>日期加时。</summary>
    Hour,

    /// <summary>日期加时分（同原生 input type="datetime-local"）。</summary>
    Minute,

    /// <summary>日期加时分秒。</summary>
    Second,
}

/// <summary>时间输入精确到哪一段。</summary>
public enum TimeGranularity
{
    /// <summary>只有时。</summary>
    Hour,

    /// <summary>时分（默认）。</summary>
    Minute,

    /// <summary>时分秒。</summary>
    Second,
}

/// <summary>12 / 24 小时制（同 Intl 的 hourCycle h12、h23）。</summary>
public enum HourCycle
{
    /// <summary>12 小时制，带上午 / 下午。</summary>
    H12,

    /// <summary>24 小时制。</summary>
    H23,
}
