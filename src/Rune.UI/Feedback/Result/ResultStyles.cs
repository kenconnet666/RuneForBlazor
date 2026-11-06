using Rune.Styles;

namespace Rune.UI;

/// <summary>结果页的常见出错状态：图标、色调、状态码、标题与说明都有默认值（标题与说明取语言包）。</summary>
public enum ResultStatus
{
    /// <summary>403：没有访问权限。</summary>
    Forbidden,

    /// <summary>404：找不到页面。</summary>
    NotFound,

    /// <summary>500：服务出错。</summary>
    ServerError,
}

/// <summary>结果页的部件。</summary>
public enum ResultPart
{
    /// <summary>外层（div）。</summary>
    Root,

    /// <summary>大图标（Icon 组件）。</summary>
    Icon,

    /// <summary>状态码（TextSpan，写了 Status 时）。</summary>
    Code,

    /// <summary>标题（TextSpan）。</summary>
    Title,

    /// <summary>说明（Paragraph）。</summary>
    Description,

    /// <summary>操作区（div）。</summary>
    Action,
}

/// <summary>结果页的默认值。</summary>
public sealed record ResultDefaults : IComponentDefaults<ResultDefaults>
{
    /// <summary>字号档位。</summary>
    public Size? Size { get; init; }

    /// <inheritdoc/>
    public ResultDefaults Over(ResultDefaults outer) => new() { Size = Size ?? outer.Size };
}

/// <summary>结果页的样式（同 vue-tsx-pro 的 resultRecipe）：图标、状态码、标题、说明、操作，居中排列。</summary>
public static class ResultStyles
{
    /// <summary>状态码（显示在标题上方）。</summary>
    public static string CodeOf(ResultStatus status) => status switch
    {
        ResultStatus.Forbidden => "403",
        ResultStatus.NotFound => "404",
        _ => "500",
    };

    /// <summary>状态的图标与色调。</summary>
    public static (IconData Icon, Tone Tone) LookOf(ResultStatus status) => status switch
    {
        ResultStatus.Forbidden => (BuiltInIcons.ShieldX, Tone.Warning),
        ResultStatus.NotFound => (BuiltInIcons.FileQuestionMark, Tone.Info),
        _ => (BuiltInIcons.ServerCrash, Tone.Danger),
    };

    /// <summary>某种字号、某套定制下各部件的默认层。</summary>
    public static PartLayers<ResultPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("result", size, styles, static (Size? size, ResultPart part) => part switch
        {
            ResultPart.Root => Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null),
            ResultPart.Icon => Icon,
            ResultPart.Code => Code,
            ResultPart.Title => Sx.Of(Title, SharedStyles.RelativeExtraLarge),
            ResultPart.Description => Description,
            _ => Action,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        PaddingBlock = Space.Xl,
        PaddingInline = Space.Lg,
        TextAlign = TextAlign.Center,
        Color = Colors.Text,
    });

    static Style Icon => Css.Define(static () => new Style
    {
        Width = CssLength.Em(4),
        Height = CssLength.Em(4),
        MarginBlockEnd = Space.Sm,
        StrokeWidth = StrokeWidths.Sm,
    });

    // 状态码：大号（随字号缩放）、次要颜色、等宽数字，压在标题上方
    static Style Code => Css.Define(static () => new Style
    {
        FontSize = CssLength.Em(2.5),
        FontWeight = FontWeights.Bold,
        LineHeight = 1,
        Color = Colors.TextMuted,
        FontVariantNumeric = FontVariantNumeric.TabularNums,
        LetterSpacing = LetterSpacings.Tight,
    });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold, LineHeight = LineHeights.Tight });

    static Style Description => Css.Define(static () => new Style { MaxWidth = CssLength.Em(36), Color = Colors.TextMuted });

    static Style Action => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Sm,
        MarginBlockStart = Space.Md,
    });
}
