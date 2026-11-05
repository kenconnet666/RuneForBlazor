namespace Rune.Demo.Client.Infrastructure;

/// <summary>示例里的团队成员与“照片”（同 vue-tsx-pro 文档站的 photos.ts）：渐变色的 SVG（data URL），不依赖外部图片服务。</summary>
public static class DemoPeople
{
    /// <summary>一个成员：名字与头像（没有头像的显示首字母）。</summary>
    /// <param name="Name">名字。</param>
    /// <param name="Photo">头像地址；null 时没有头像。</param>
    public sealed record Member(string Name, string? Photo);

    /// <summary>示例里的团队成员。</summary>
    public static readonly IReadOnlyList<Member> Members =
    [
        new("林小满", Photo("#f97316", "#db2777")),
        new("周一航", Photo("#0ea5e9", "#6366f1")),
        new("Chen Mo", Photo("#10b981", "#0d9488")),
        new("吴悠", null),
        new("Zhao Qing", Photo("#a855f7", "#ec4899")),
        new("孙浩然", null),
        new("Li Wei", Photo("#eab308", "#f97316")),
    ];

    /// <summary>渐变底色加一个白色人形的“照片”。</summary>
    public static string Photo(string from, string to)
    {
        var svg = $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="{from}"/><stop offset="1" stop-color="{to}"/></linearGradient></defs><rect width="64" height="64" fill="url(#g)"/><circle cx="32" cy="26" r="11" fill="#fff" fill-opacity=".85"/><path d="M12 60c2-13 11-19 20-19s18 6 20 19z" fill="#fff" fill-opacity=".85"/></svg>""";
        return $"data:image/svg+xml,{Uri.EscapeDataString(svg)}";
    }
}
