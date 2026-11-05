using Rune.Styles;

namespace Rune.UI;

/// <summary>头像组的部件。</summary>
public enum AvatarGroupPart
{
    /// <summary>外层（div，role="group"）。</summary>
    Root,

    /// <summary>“+N”。</summary>
    More,

    /// <summary>“+N”里的文字。</summary>
    MoreText,
}

/// <summary>头像组交给里面的头像的信息：在组里（叠放）。</summary>
public sealed class AvatarGroupContext
{
    /// <summary>唯一的实例（级联值不变，头像不随组重新渲染）。</summary>
    public static readonly AvatarGroupContext Instance = new();

    AvatarGroupContext() { }
}

/// <summary>头像组的样式（同 vue-tsx-pro 的 avatarGroupStyles）。</summary>
public static class AvatarGroupStyles
{
    /// <summary>某个字号档位、某套定制下各部件的默认层。</summary>
    public static PartLayers<AvatarGroupPart> Of(Size? size, ComponentStyles styles) =>
        Recipe.Of("avatar-group", size, styles, static (Size? size, AvatarGroupPart part) => part switch
        {
            AvatarGroupPart.Root => Sx.Of(Root, size is { } s ? ScaleStyles.FontSize(s) : null),
            // “+N”和组里的头像一样大、一样叠放
            AvatarGroupPart.More => Sx.Of(AvatarStyles.Root, AvatarStyles.Grouped, ScaleStyles.Radius(Radius.Full), More),
            _ => MoreText,
        });

    static Style Root => Css.Define(static () => new Style { Display = Display.InlineFlex, AlignItems = AlignItems.Center });

    static Style More => Css.Define(static () => new Style { BackgroundColor = Colors.Fill, Color = Colors.Text });

    // 字号缩在文字上：“+N”的圆仍和头像一样大
    static Style MoreText => Css.Define(static () => new Style { FontSize = CssLength.Em(0.875), FontWeight = FontWeights.Medium });
}
