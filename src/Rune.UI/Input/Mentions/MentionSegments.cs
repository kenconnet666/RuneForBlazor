using Rune.Styles;

namespace Rune.UI;

/// <summary>拆开的一段文字：普通文字，或提及（带它的选项）。</summary>
/// <param name="Text">这一段的文字（提及时含 @）。</param>
/// <param name="Mention">提及的选项；普通文字时为 null。</param>
public readonly record struct MentionSegment(string Text, ListboxOption? Mention);

/// <summary>
/// 文字里的提及（同 vue-tsx-pro 的 mentionSegments、mentionedValues）：“@名字”里的名字是某个选项的显示文字（有多个能匹配时取最长的）；
/// @ 前面是字母数字时不算（邮箱 name@example.com）。MentionText 显示它，Mentions 用它找出提到了谁。
/// </summary>
public static class MentionSegments
{
    static bool IsWordBefore(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '.';

    /// <summary>把文字拆成普通文字与提及。</summary>
    public static IReadOnlyList<MentionSegment> Of(string? text, IEnumerable<ListboxOption> options, string prefix = "@")
    {
        if (string.IsNullOrEmpty(text)) return [];
        var byLength = options.Where(o => o.Label.Length > 0).OrderByDescending(o => o.Label.Length).ToList();
        var segments = new List<MentionSegment>();
        var plain = new System.Text.StringBuilder();
        var index = 0;
        while (index < text.Length)
        {
            var mention = text.AsSpan(index).StartsWith(prefix, StringComparison.Ordinal) && (index == 0 || !IsWordBefore(text[index - 1]))
                ? byLength.FirstOrDefault(o => text.AsSpan(index + prefix.Length).StartsWith(o.Label, StringComparison.Ordinal))
                : null;
            if (mention is null)
            {
                plain.Append(text[index]);
                index++;
                continue;
            }
            if (plain.Length > 0) segments.Add(new(plain.ToString(), null));
            plain.Clear();
            var length = prefix.Length + mention.Label.Length;
            segments.Add(new(text.Substring(index, length), mention));
            index += length;
        }
        if (plain.Length > 0) segments.Add(new(plain.ToString(), null));
        return segments;
    }

    /// <summary>文字里提到了哪些选项（值，按第一次出现的顺序，不重复）。</summary>
    public static IReadOnlyList<string> Values(string? text, IEnumerable<ListboxOption> options, string prefix = "@") =>
        [.. Of(text, options, prefix).Where(s => s.Mention is not null).Select(s => s.Mention!.Value).Distinct()];
}

/// <summary>提及的样式（同 vue-tsx-pro 的 mentionStyles）。</summary>
public static class MentionStyles
{
    /// <summary>提及：强调色的文字，浅底。</summary>
    public static Style Mention => Css.Define(static () => new Style
    {
        Color = Tones.Accent.Text,
        BackgroundColor = Tones.Accent.Soft,
        BorderRadius = Radii.Sm,
        PaddingInline = CssLength.Em(0.125),
        FontWeight = FontWeights.Medium,
    });

    /// <summary>带提及的文字：换行照常。</summary>
    public static Style Text => Css.Define(static () => new Style { WhiteSpace = WhiteSpace.PreWrap, OverflowWrap = OverflowWrap.Anywhere });

    /// <summary>弹层贴着光标、不跟文本框同宽：给个合适的宽度。</summary>
    public static Style Panel => Css.Define(static () => new Style { Width = CssLength.Em(16) });
}
