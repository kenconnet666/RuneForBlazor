using System.Text.RegularExpressions;
using Rune.Styles;

namespace Rune.UI;

/// <summary>头像的部件。</summary>
public enum AvatarPart
{
    /// <summary>外层（span，role="img"）。</summary>
    Root,

    /// <summary>图片。</summary>
    Image,

    /// <summary>后备内容（首字母、图标）。</summary>
    Fallback,
}

/// <summary>头像的外观。</summary>
/// <param name="Size">字号档位，头像边长是它的 2.5 倍；null 时继承。</param>
/// <param name="Radius">圆角。</param>
/// <param name="Variant">后备内容的底色样式（Soft 或 Solid）。</param>
/// <param name="Tone">后备内容的色调。</param>
/// <param name="Grouped">在头像组里：往前叠，加一圈页面底色的描边。</param>
public readonly record struct AvatarAppearance(Size? Size, Radius Radius, StatusVariant Variant, Tone Tone, bool Grouped) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Size, Radius, Variant, Tone];
}

/// <summary>头像的默认值（<see cref="ComponentDefaults"/> 里设置）。</summary>
public sealed record AvatarDefaults : IComponentDefaults<AvatarDefaults>
{
    /// <summary>字号档位。</summary>
    public Size? Size { get; init; }

    /// <summary>圆角；团队、组织、应用的图标常用 Md（圆角方形）。</summary>
    public Radius? Radius { get; init; }

    /// <summary>后备内容的底色样式。</summary>
    public StatusVariant? Variant { get; init; }

    /// <summary>后备内容的色调。</summary>
    public Tone? Tone { get; init; }

    /// <inheritdoc/>
    public AvatarDefaults Over(AvatarDefaults outer) => new()
    {
        Size = Size ?? outer.Size,
        Radius = Radius ?? outer.Radius,
        Variant = Variant ?? outer.Variant,
        Tone = Tone ?? outer.Tone,
    };
}

/// <summary>
/// 头像的样式（配方，同 vue-tsx-pro 的 avatarRecipe）。图片盖在后备内容上面：还没加载好时透明、加载失败时不显示，
/// 露出下面的首字母（JS 初始化器按 load / error 在图片上写 data-state），不用等往返；换图时图片按地址重建，状态跟着重置。
/// </summary>
public static partial class AvatarStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<AvatarPart> Of(AvatarAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("avatar", appearance, styles, static (AvatarAppearance a, AvatarPart part) => part switch
        {
            AvatarPart.Root => Sx.Of(
                SharedStyles.StatusColors(a.Variant, a.Tone),
                Root,
                a.Grouped ? Grouped : null,
                ScaleStyles.Radius(a.Radius),
                a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            AvatarPart.Image => Image,
            _ => Fallback,
        });

    /// <summary>名字的首字母：中文取最后一个字（通常是名），其他语言取前两个词的首字母。</summary>
    public static string Initials(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0) return "";
        if (CjkPattern().IsMatch(trimmed)) return trimmed[^1..];
        var words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return (words.Length > 1 ? $"{words[0][0]}{words[^1][0]}" : $"{words[0][0]}").ToUpperInvariant();
    }

    [GeneratedRegex("[㐀-鿿]")]
    private static partial Regex CjkPattern();

    /// <summary>外层（头像组的“+N”也用它，和头像一样大）。</summary>
    internal static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        FlexShrink = 0,
        Width = CssLength.Em(2.5),
        Height = CssLength.Em(2.5),
        Overflow = Overflow.Hidden,
        VerticalAlign = VerticalAlign.Middle,
        UserSelect = UserSelect.None,
    });

    /// <summary>头像组里：往前叠 0.5em，一圈页面底色的描边把相邻的分开（“+N”也一样）。</summary>
    internal static Style Grouped => Css.Define(static () => new Style
    {
        BoxShadow = $"0 0 0 {BorderWidths.Sm} {Colors.Background}",
        MarginInlineStart = CssLength.Em(-0.5),
        [When.FirstChild] = new() { MarginInlineStart = 0 },
    });

    // 加载中盖在后备内容上、透明；加载好了回到正常流（同 vue-tsx-pro：加载中 absolute + 透明），加载失败时不显示
    static Style Image => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        ObjectFit = ObjectFit.Cover,
        Opacity = 0,
        [When.Attribute("""[data-state="loaded"]""")] = new() { Position = Position.Static, Opacity = 1 },
        [When.Attribute("""[data-state="error"]""")] = new() { Display = Display.None },
    });

    // 图片加载好了就不显示（同 vue-tsx-pro 不再渲染后备内容）
    static Style Fallback => Css.Define(static () => new Style
    {
        FontWeight = FontWeights.Medium,
        LineHeight = 1,
        TextTransform = TextTransform.Uppercase,
        [When.Parent(When.Has("""> img[data-state="loaded"]"""))] = new() { Display = Display.None },
    });
}
