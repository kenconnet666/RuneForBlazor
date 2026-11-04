using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>
/// 标题 H1–H6 的基类：级别决定元素与默认字号（H1 Xxxl、H2 Xxl、H3 Xl、H4 Lg、H5–H6 Md），默认 Semibold、紧凑行高、各行长度均衡。
/// 视觉层级与文档结构分开：字号用 Size 改，级别要连续，不要为了字号跳级。
/// </summary>
public abstract class HeadingBase : TypographyBase
{
    /// <summary>级别（1–6）。</summary>
    protected abstract int Level { get; }

    /// <summary>有 id 时在末尾放一个指向本节的锚点链接（#），悬停标题或聚焦时显示：文档里方便复制本节的链接。</summary>
    [Parameter] public bool Anchor { get; set; }

    /// <summary>锚点链接的名称（读屏读出），不写时取语言包（UIConfig.Lang）的“链接到本节”。</summary>
    [Parameter] public string? AnchorLabel { get; set; }

    /// <summary>锚点链接（a）的部件属性。</summary>
    [Parameter] public Attrs? PropsAnchor { get; set; }

    Memo<(string Id, string Label), Attrs> anchorAttrs;

    /// <inheritdoc/>
    protected override void BuildTrailing(RenderTreeBuilder builder)
    {
        if (!Anchor || Rest["id"] is not string { Length: > 0 } id) return;
        var managed = anchorAttrs.Get((id, AnchorLabel ?? Config.Lang.HeadingAnchor), static k => new Attrs { ["href"] = $"#{k.Id}", AriaLabel = k.Label });
        var parts = TypographyStyles.Heading(Appearance, Config.Styles);
        builder.OpenElement(10, "a");
        builder.AddMultipleAttributes(11, Attrs.Merge(parts[HeadingPart.Anchor], PropsAnchor, managed)!);
        builder.OpenComponent<Icon>(12);
        builder.AddComponentParameter(13, nameof(Icon.Data), BuiltInIcons.Hash);
        builder.CloseComponent();
        builder.CloseElement();
    }

    /// <inheritdoc/>
    protected override string Tag => Level switch
    {
        1 => "h1",
        2 => "h2",
        3 => "h3",
        4 => "h4",
        5 => "h5",
        _ => "h6",
    };

    /// <inheritdoc/>
    protected override TypographyKind Kind => TypographyKind.Heading;

    /// <inheritdoc/>
    protected override Size? DefaultSize => Level switch
    {
        1 => UI.Size.Xxxl,
        2 => UI.Size.Xxl,
        3 => UI.Size.Xl,
        4 => UI.Size.Lg,
        _ => UI.Size.Md,
    };

    /// <inheritdoc/>
    protected override Weight? DefaultWeight => UI.Weight.Semibold;

    /// <inheritdoc/>
    protected override Attrs RootLayer => TypographyStyles.Heading(Appearance, Config.Styles)[HeadingPart.Root];
}

/// <summary>一级标题（h1）：页面主标题，默认 30px。</summary>
public sealed class H1 : HeadingBase
{
    /// <inheritdoc/>
    protected override int Level => 1;
}

/// <summary>二级标题（h2），默认 24px。</summary>
public sealed class H2 : HeadingBase
{
    /// <inheritdoc/>
    protected override int Level => 2;
}

/// <summary>三级标题（h3），默认 20px。</summary>
public sealed class H3 : HeadingBase
{
    /// <inheritdoc/>
    protected override int Level => 3;
}

/// <summary>四级标题（h4），默认 16px。</summary>
public sealed class H4 : HeadingBase
{
    /// <inheritdoc/>
    protected override int Level => 4;
}

/// <summary>五级标题（h5），默认 14px。</summary>
public sealed class H5 : HeadingBase
{
    /// <inheritdoc/>
    protected override int Level => 5;
}

/// <summary>六级标题（h6），默认 14px。</summary>
public sealed class H6 : HeadingBase
{
    /// <inheritdoc/>
    protected override int Level => 6;
}
