using Rune.Styles;

namespace Rune.UI;

/// <summary>卡片的样式。</summary>
public enum CardVariant
{
    /// <summary>表面色加细边框（默认）。</summary>
    Surface,

    /// <summary>只有边框，底色透明。</summary>
    Outline,

    /// <summary>页面底色加阴影，浮在页面上。</summary>
    Elevated,

    /// <summary>没有底色与边框，只保留内边距与分区。</summary>
    Ghost,
}

/// <summary>卡片的装饰效果（官网、活动页的卡片）：光层对读屏隐藏、不接收指针；减少动态效果时不倾斜、流光不动。</summary>
public enum CardEffect
{
    /// <summary>指针在上面移动时整块向指针方向倾斜，带一层跟随指针的高光；离开后弹回平放。</summary>
    Tilt,

    /// <summary>一团柔和的光跟着指针，靠近边框的地方把边框也点亮；并排的几张卡片光会在它们之间传递。</summary>
    Spotlight,

    /// <summary>一段渐变的光（品牌色到紫色）沿着边框循环流动，用来突出推荐的那一张。</summary>
    Beam,
}

/// <summary>卡片的部件。</summary>
public enum CardPart
{
    /// <summary>卡片本身（div）。</summary>
    Root,

    /// <summary>页头：与正文之间有分隔线；有 Extra 时和它排成一行、两端对齐。</summary>
    Header,

    /// <summary>正文。</summary>
    Body,

    /// <summary>页脚：操作按钮，靠结束边排列，与正文之间有分隔线。</summary>
    Footer,

    /// <summary>有装饰效果时外面定位用的一层：未声明的属性落在它上面，倾斜时整层转动。</summary>
    Frame,

    /// <summary>铺满卡片的光层：倾斜的高光、聚光的表面光。</summary>
    Effect,

    /// <summary>只露出边框一圈的光层：聚光的边框光、流光的轨道。</summary>
    Ring,

    /// <summary>流光的光带：沿圆角矩形的路径循环走。</summary>
    Beam,
}

/// <summary>卡片的默认值。</summary>
public sealed record CardDefaults : IComponentDefaults<CardDefaults>
{
    /// <summary>样式。</summary>
    public CardVariant? Variant { get; init; }

    /// <summary>内边距。</summary>
    public SpaceScale? Padding { get; init; }

    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <inheritdoc/>
    public CardDefaults Over(CardDefaults outer) => new()
    {
        Variant = Variant ?? outer.Variant,
        Padding = Padding ?? outer.Padding,
        Radius = Radius ?? outer.Radius,
    };
}

/// <summary>卡片的外观：交给 <see cref="CardStyles.Of"/> 的档位组合。</summary>
/// <param name="Variant">样式。</param>
/// <param name="Padding">页头、正文、页脚的内边距。</param>
/// <param name="Radius">圆角。</param>
/// <param name="HasExtra">有页头结束边的内容：页头排成一行、两端对齐。</param>
/// <param name="Effect">装饰效果。</param>
public readonly record struct CardAppearance(CardVariant Variant, SpaceScale Padding, Radius Radius, bool HasExtra = false, CardEffect? Effect = null)
{
    /// <summary>外观：组件上写的 &gt; 配置的默认值（<see cref="CardDefaults"/>）&gt; 表面色、Lg 内边距、大圆角。</summary>
    public static CardAppearance Resolve(CardVariant? variant, SpaceScale? padding, Radius? radius, bool hasExtra, CardEffect? effect, UIConfig config)
    {
        var defaults = config.Defaults.Get<CardDefaults>();
        return new CardAppearance(
            variant ?? defaults?.Variant ?? CardVariant.Surface,
            padding ?? defaults?.Padding ?? SpaceScale.Lg,
            radius ?? defaults?.Radius ?? UI.Radius.Lg,
            hasExtra,
            effect);
    }
}

/// <summary>
/// 卡片的样式（配方，同 vue-tsx-pro 的 cardRecipe）。装饰效果的倾斜角度与光的位置每帧变化：JS（data-rune-pointer）
/// 把阻尼后的指针位置写成 --rune-pointer-x / -y / -strength，这里用 calc 换算。
/// </summary>
public static class CardStyles
{
    // 指针位置（-1 到 1，向上为正）与强度（0 到 1），没写时都是 0：效果不可见、卡片平放
    const string X = "var(--rune-pointer-x, 0)";
    const string Y = "var(--rune-pointer-y, 0)";
    const string Strength = "var(--rune-pointer-strength, 0)";
    // 光的中心：指针位置换成百分比
    const string LightAt = $"calc(({X} + 1) * 50%) calc((1 - {Y}) * 50%)";

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<CardPart> Of(CardAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("card", appearance, styles, static (CardAppearance a, CardPart part) => part switch
        {
            CardPart.Root => Sx.Of(Root, Variant(a.Variant), ScaleStyles.Radius(a.Radius), a.Effect is null ? null : FillFrame),
            CardPart.Header => Sx.Of(Header, a.HasExtra ? HeaderWithExtra : null, ScaleStyles.Padding(a.Padding)),
            CardPart.Body => ScaleStyles.Padding(a.Padding),
            CardPart.Footer => Sx.Of(Footer, ScaleStyles.Padding(a.Padding)),
            CardPart.Frame => Sx.Of(Frame, ScaleStyles.Radius(a.Radius), a.Effect == CardEffect.Tilt ? Tilt : null),
            CardPart.Effect => Sx.Of(Layer, a.Effect == CardEffect.Tilt ? Glare : SpotlightSurface),
            CardPart.Ring => Sx.Of(Layer, a.Effect == CardEffect.Beam ? BeamTrack : SpotlightRing),
            _ => Sx.Of(Beam, BeamPath(a.Radius)),
        });

    static Style Variant(CardVariant variant) => Css.Define(variant, static variant => variant switch
    {
        CardVariant.Outline => new Style { BorderColor = Colors.Border },
        CardVariant.Elevated => new Style { BackgroundColor = Colors.SurfaceRaised, BoxShadow = Shadows.Md },
        CardVariant.Ghost => new Style(),
        _ => new Style { BackgroundColor = Colors.Surface, BorderColor = Colors.Border },
    });

    // 圆角裁掉子元素（页头的底色、正文里的图片）；边框先透明，样式需要时再上色，各样式的尺寸一致
    static Style Root => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        MinWidth = 0,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = CssColors.Transparent,
        Color = Colors.Text,
        Overflow = Overflow.Hidden,
    });

    static Style Header => Css.Define(static () => new Style
    {
        BorderBottomWidth = BorderWidths.Xs,
        BorderBottomStyle = BorderBottomStyle.Solid,
        BorderBottomColor = Colors.BorderMuted,
    });

    // 有 Extra 时：页头内容在起始边、Extra 在结束边，放不下时换行
    static Style HeaderWithExtra => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Sm,
    });

    static Style Footer => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Space.Sm,
        BorderTopWidth = BorderWidths.Xs,
        BorderTopStyle = BorderTopStyle.Solid,
        BorderTopColor = Colors.BorderMuted,
    });

    // 有效果时卡片撑满外层
    static Style FillFrame => Css.Define(static () => new Style { FlexGrow = 1 });

    static Style Frame => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        MinWidth = 0,
    });

    // 倾斜：朝指针方向最多转 10 度、放大 2%；减少动态效果时不倾斜（高光照样跟着）
    static Style Tilt => Css.Define(static () => new Style
    {
        TransformStyle = TransformStyle.Preserve3d,
        WillChange = "transform",
        Transform = $"perspective(60em) rotateX(calc({Y} * {Strength} * 10deg)) rotateY(calc({X} * {Strength} * 10deg)) scale(calc(1 + 0.02 * {Strength}))",
        [Media.ReducedMotion] = new() { Transform = "none" },
    });

    static Style Layer => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Inset = 0,
        BorderRadius = CssWide.Inherit,
        PointerEvents = PointerEvents.None,
    });

    static Style Glare => Css.Define(static () => new Style
    {
        MixBlendMode = MixBlendMode.SoftLight,
        Opacity = Strength,
        BackgroundImage = $"radial-gradient(circle at {LightAt}, color-mix(in oklab, {Tones.Accent.Soft} 40%, {Colors.SurfaceRaised}), transparent 60%)",
    });

    static Style SpotlightSurface => Css.Define(static () => new Style
    {
        Opacity = Strength,
        BackgroundImage = $"radial-gradient(20em circle at {LightAt}, color-mix(in oklab, {Tones.Accent.Solid} 16%, transparent), transparent 70%)",
    });

    // 只留边框那一圈：内容盒与整个盒子的遮罩相减
    static Style SpotlightRing => Css.Define(static () => new Style
    {
        Padding = BorderWidths.Md,
        Opacity = Strength,
        BackgroundImage = $"radial-gradient(12em circle at {LightAt}, color-mix(in oklab, {Tones.Accent.Solid} 85%, transparent), transparent 70%)",
        MaskImage = "linear-gradient(#000 0 0), linear-gradient(#000 0 0)",
        MaskClip = "content-box, border-box",
        MaskComposite = MaskComposite.Exclude,
    });

    // 流光的轨道：透明边框那一圈露出来，光带在里面走
    static Style BeamTrack => Css.Define(static () => new Style
    {
        BorderWidth = BorderWidths.Md,
        BorderStyle = BorderStyle.Solid,
        BorderColor = CssColors.Transparent,
        MaskImage = "linear-gradient(transparent, transparent), linear-gradient(#000, #000)",
        MaskClip = "padding-box, border-box",
        MaskComposite = MaskComposite.Intersect,
    });

    // 光带沿外层的圆角矩形走（offset-path）；循环的装饰动画不取 Durations，减少动态效果时停下
    static Style Beam => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Width = CssLength.Rem(5),
        AspectRatio = 1,
        OffsetAnchor = "90% 50%",
        BackgroundImage = $"linear-gradient(to left, {ChartColors.Series5}, {Tones.Accent.Solid}, transparent)",
        Keyframes = Travel,
        AnimationDuration = "6s",
        AnimationTimingFunction = AnimationTimingFunction.Linear,
        AnimationIterationCount = "infinite",
        [Media.ReducedMotion] = new() { AnimationName = "none" },
    });

    static Style BeamPath(Radius radius) => Css.Define(radius, static radius => new Style
    {
        OffsetPath = $"rect(0 auto auto 0 round {ScaleStyles.RadiusLength(radius)})",
    });

    static Keyframes Travel => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { OffsetDistance = CssLength.Percent(0) },
        ["to"] = new() { OffsetDistance = CssLength.Percent(100) },
    });
}
