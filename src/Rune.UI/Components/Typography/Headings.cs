namespace Rune.UI;

/// <summary>
/// 标题 H1–H6 的基类：级别决定元素与默认字号（H1 Xxxl、H2 Xxl、H3 Xl、H4 Lg、H5–H6 Md），默认 Semibold、紧凑行高、各行长度均衡。
/// 视觉层级与文档结构分开：字号用 Size 改，级别要连续，不要为了字号跳级。
/// </summary>
public abstract class HeadingBase : TypographyBase
{
    /// <summary>级别（1–6）。</summary>
    protected abstract int Level { get; }

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
