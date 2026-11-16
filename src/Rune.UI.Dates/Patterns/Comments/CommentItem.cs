using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>留言的一个附件（同 vue-tsx-pro 的 CommentAttachment）。</summary>
/// <param name="Name">文件名。</param>
public sealed record CommentAttachment(string Name)
{
    /// <summary>地址：文件名链到它（预览、下载）。</summary>
    public string? Url { get; init; }

    /// <summary>大小（字节）。</summary>
    public long? Size { get; init; }
}

/// <summary>一条留言（同 vue-tsx-pro 的 CommentItem）。</summary>
/// <param name="Id">标识。</param>
/// <param name="Author">作者。</param>
/// <param name="Content">内容：文字，“@名字”按 People 高亮。</param>
/// <param name="Time">发表的时间。</param>
public sealed record CommentItem(string Id, string Author, string Content, DateTimeOffset Time)
{
    /// <summary>作者的头像地址；没有时显示首字母。</summary>
    public string? Avatar { get; init; }

    /// <summary>作者的身份（岗位、“供应商”标记），在名字后面。</summary>
    public RenderFragment? Role { get; init; }

    /// <summary>改过。</summary>
    public bool Edited { get; init; }

    /// <summary>附件。</summary>
    public IReadOnlyList<CommentAttachment> Attachments { get; init; } = [];

    /// <summary>内容下面的补充（逐条回应的“采纳 / 不采纳”、引用的字段对比）。</summary>
    public RenderFragment? Extra { get; init; }

    /// <summary>回复（只分一层，回复里的 Replies 不显示）。</summary>
    public IReadOnlyList<CommentItem> Replies { get; init; } = [];
}

/// <summary>写好的一条留言：OnSend 收到它（同 vue-tsx-pro 的 CommentDraft）。</summary>
/// <param name="Content">内容（去掉首尾空白）。</param>
/// <param name="Mentions">内容里 @ 到的人（People 的值）。</param>
/// <param name="Attachments">上传完成的附件。</param>
public sealed record CommentDraft(string Content, IReadOnlyList<string> Mentions, IReadOnlyList<UploadFile> Attachments)
{
    /// <summary>回复哪一串（那一串第一条留言的 Id）；新的一串没有。</summary>
    public string? ReplyTo { get; init; }
}

/// <summary>写留言的人：输入框前面的头像。</summary>
/// <param name="Name">名字。</param>
/// <param name="Avatar">头像地址。</param>
public sealed record CommentAuthor(string Name, string? Avatar = null);

/// <summary>留言串的部件。</summary>
public enum CommentThreadPart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>列表（ul）。</summary>
    List,

    /// <summary>一条留言（li）。</summary>
    Item,

    /// <summary>头像。</summary>
    Avatar,

    /// <summary>头像右边的内容（article）。</summary>
    Body,

    /// <summary>作者、身份、时间那一行。</summary>
    Header,

    /// <summary>作者名。</summary>
    Author,

    /// <summary>时间、已编辑、附件大小。</summary>
    Meta,

    /// <summary>内容（MentionText）。</summary>
    Content,

    /// <summary>附件列表（ul）。</summary>
    Attachments,

    /// <summary>一个附件（li）。</summary>
    Attachment,

    /// <summary>回复按钮与 Actions 那一行。</summary>
    Actions,

    /// <summary>回复列表（ul）。</summary>
    Replies,

    /// <summary>没有留言时的 Empty。</summary>
    Empty,
}

/// <summary>留言输入框的部件。</summary>
public enum CommentComposerPart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>头像。</summary>
    Avatar,

    /// <summary>头像右边那一列（输入框、附件、按钮）。</summary>
    Main,

    /// <summary>发送失败的提示。</summary>
    Error,

    /// <summary>底部那一行。</summary>
    Footer,

    /// <summary>底部的两组按钮。</summary>
    Tools,

    /// <summary>“Ctrl + Enter 发送”的提示。</summary>
    Hint,
}

/// <summary>留言串与输入框的样式（同 vue-tsx-pro 的 commentStyles）。</summary>
public static class CommentStyles
{
    /// <summary>留言串各部件的默认层。</summary>
    public static PartLayers<CommentThreadPart> Of(ComponentStyles styles) =>
        Recipe.Of("comment-thread", styles, static (CommentThreadPart part) => part switch
        {
            CommentThreadPart.Root => Root,
            CommentThreadPart.List => List,
            CommentThreadPart.Item => Item,
            CommentThreadPart.Body => Body,
            CommentThreadPart.Header => Header,
            CommentThreadPart.Author => Author,
            CommentThreadPart.Meta => Meta,
            CommentThreadPart.Content => Content,
            CommentThreadPart.Attachments => Attachments,
            CommentThreadPart.Attachment => Attachment,
            CommentThreadPart.Actions => Actions,
            CommentThreadPart.Replies => Replies,
            _ => Sx.Of((Style?)null),
        });

    /// <summary>输入框各部件的默认层；没有头像时只有一列。</summary>
    public static PartLayers<CommentComposerPart> ComposerOf(bool avatar, ComponentStyles styles) =>
        Recipe.Of("comment-composer", avatar, styles, static (bool avatar, CommentComposerPart part) => part switch
        {
            CommentComposerPart.Root => avatar ? Composer : Sx.Of(Composer, ComposerNoAvatar),
            CommentComposerPart.Main => ComposerMain,
            CommentComposerPart.Error => Error,
            CommentComposerPart.Footer => ComposerFooter,
            CommentComposerPart.Tools => ComposerTools,
            CommentComposerPart.Hint => Hint,
            _ => Sx.Of((Style?)null),
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Lg });

    static Style List => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Lg, Margin = 0, Padding = 0, ListStyle = ListStyle.None });

    // 头像一列、内容一列
    static Style Item => Css.Define(static () => new Style { Display = Display.Grid, GridTemplateColumns = "auto 1fr", ColumnGap = Space.Sm, AlignItems = AlignItems.Start });

    static Style Body => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Xs, MinWidth = 0 });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        ColumnGap = Space.Sm,
        RowGap = Space.Xs,
    });

    static Style Author => Css.Define(static () => new Style { FontWeight = FontWeights.Medium, Color = Colors.Text });

    static Style Meta => Css.Define(static () => new Style { FontSize = FontSizes.Sm, Color = Colors.TextMuted });

    static Style Content => Css.Define(static () => new Style { Color = Colors.Text });

    static Style Attachments => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        ColumnGap = Space.Md,
        RowGap = Space.Xs,
        Margin = 0,
        Padding = 0,
        ListStyle = ListStyle.None,
        FontSize = FontSizes.Sm,
    });

    static Style Attachment => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center, Gap = CssLength.Em(0.25) });

    static Style Actions => Css.Define(static () => new Style { Display = Display.Flex, FlexWrap = FlexWrap.Wrap, AlignItems = AlignItems.Center, Gap = Space.Sm, FontSize = FontSizes.Sm });

    // 回复：缩进在内容列里，左边一条竖线
    static Style Replies => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Gap = Space.Md,
        Margin = 0,
        MarginBlockStart = Space.Xs,
        PaddingInlineStart = Space.Md,
        ListStyle = ListStyle.None,
        BorderInlineStartWidth = BorderWidths.Sm,
        BorderInlineStartStyle = BorderStyle.Solid,
        BorderInlineStartColor = Colors.BorderMuted,
    });

    static Style Composer => Css.Define(static () => new Style { Display = Display.Grid, GridTemplateColumns = "auto 1fr", ColumnGap = Space.Sm, AlignItems = AlignItems.Start });

    static Style ComposerNoAvatar => Css.Define(static () => new Style { GridTemplateColumns = "1fr" });

    static Style ComposerMain => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Xs, MinWidth = 0 });

    static Style ComposerFooter => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Xs,
    });

    static Style ComposerTools => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Sm });

    static Style Hint => Css.Define(static () => new Style { FontSize = FontSizes.Xs, Color = Colors.TextMuted });

    static Style Error => Css.Define(static () => new Style { FontSize = FontSizes.Sm, Color = Tones.Danger.Text });
}
