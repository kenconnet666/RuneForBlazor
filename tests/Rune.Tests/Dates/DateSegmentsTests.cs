using Rune.UI;

namespace Rune.Tests.Dates;

// 段的排法：期望值来自 Node 的 Intl.DateTimeFormat(…).formatToParts（年 numeric，其余 2-digit），vue-tsx-pro 的 date-segments.ts 就是它
public class DateSegmentsTests
{
    const DateSegmentType Y = DateSegmentType.Year;

    static string Describe(IEnumerable<SegmentPart> parts) =>
        string.Join(" ", parts.Select(part => part.Type is { } type ? DateSegments.Name(type) : $"\"{part.Literal}\""));

    public static TheoryData<string, string, bool, string> Layouts => new()
    {
        { "zh-CN", "year month day", false, "year \"/\" month \"/\" day" },
        { "zh-CN", "year month", false, "year \"年\" month \"月\"" },
        { "zh-CN", "year", false, "year \"年\"" },
        { "zh-CN", "hour minute", false, "hour \":\" minute" },
        { "zh-CN", "hour minute second", true, "dayPeriod hour \":\" minute \":\" second" },
        { "zh-CN", "year month day hour minute", false, "year \"/\" month \"/\" day \" \" hour \":\" minute" },
        { "en-US", "year month day", false, "month \"/\" day \"/\" year" },
        { "en-US", "year month", false, "month \"/\" year" },
        { "en-US", "year", false, "year" },
        { "en-US", "hour minute", false, "hour \":\" minute" },
        { "en-US", "hour minute second", true, "hour \":\" minute \":\" second \" \" dayPeriod" },
        { "en-US", "year month day hour minute", false, "month \"/\" day \"/\" year \", \" hour \":\" minute" },
        { "de-DE", "year month day", false, "day \".\" month \".\" year" },
        { "de-DE", "year month", false, "month \".\" year" },
        { "en-GB", "year month day", false, "day \"/\" month \"/\" year" },
    };

    [Theory]
    [MemberData(nameof(Layouts))]
    public void 段的顺序与分隔符同Intl(string culture, string fields, bool hour12, string expected) =>
        Assert.Equal(expected, Describe(DateSegments.Layout([.. fields.Split(' ').Select(field => Enum.Parse<DateSegmentType>(field, ignoreCase: true))], Lang.EnUs with { Code = culture }, hour12)));

    [Fact]
    public void 季度与周按语言包的排法()
    {
        Assert.Equal("year \"年第\" quarter \"季度\"", Describe(DateSegments.Layout(DateSegments.FieldsOf(DateGranularity.Quarter), Lang.ZhCn, false)));
        Assert.Equal("\"Week \" week \", \" year", Describe(DateSegments.Layout(DateSegments.FieldsOf(DateGranularity.Week), Lang.EnUs, false)));
    }

    [Fact]
    public void 空段的占位_后面已经是单位字时用横线()
    {
        Assert.Equal("––", DateSegments.Placeholder(Y, "年", Lang.ZhCn));
        Assert.Equal("年", DateSegments.Placeholder(Y, "/", Lang.ZhCn));
        Assert.Equal("yyyy", DateSegments.Placeholder(Y, null, Lang.EnUs));
    }

    [Theory]
    [InlineData(DateGranularity.Year, "2026")]
    [InlineData(DateGranularity.Month, "2026-10")]
    [InlineData(DateGranularity.Day, "2026-10-20")]
    [InlineData(DateGranularity.Minute, "2026-10-20T14:30")]
    [InlineData(DateGranularity.Second, "2026-10-20T14:30:05")]
    [InlineData(DateGranularity.Quarter, "2026-Q4")]
    [InlineData(DateGranularity.Week, "2026-W43")]
    public void ISO文字往返(DateGranularity granularity, string iso)
    {
        var fields = DateSegments.FieldsOf(granularity);
        Assert.Equal(iso, DateSegments.ToIso(DateSegments.FromIso(iso, fields)!, fields));
    }

    [Fact]
    public void 缺段或格式不对时没有值()
    {
        Assert.Null(DateSegments.FromIso("2026-10", DateSegments.FieldsOf(DateGranularity.Day)));
        Assert.Null(DateSegments.FromIso("10/20/2026", DateSegments.FieldsOf(DateGranularity.Day)));
        Assert.Equal(14, DateSegments.FromIso("14:30", DateSegments.FieldsOf(TimeGranularity.Minute))!.Hour);
    }
}
