namespace Rune.UI;

/// <summary>输入框的字数计数（ShowCount）共用的小工具。</summary>
static class CharacterCounting
{
    /// <summary>使用方写的 maxlength（字数上限）；没写或写错时为 null。</summary>
    public static int? MaxLengthOf(IReadOnlyDictionary<string, object>? attributes) =>
        attributes?.TryGetValue("maxlength", out var value) == true && int.TryParse(value?.ToString(), out var max) ? max : null;

    /// <summary>输入框的 aria-describedby 接上计数的 id（字段的说明、错误在前）。</summary>
    public static string? DescribedBy(string? field, string? countId) =>
        countId is null ? field : field is null ? countId : $"{field} {countId}";
}
