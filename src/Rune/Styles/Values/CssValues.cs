using System.Globalization;

namespace Rune.Styles;

/// <summary>
/// 任意 CSS 值：关键字、带单位的字符串、主题 token，或数字。
/// 数字的单位由属性决定（见 <see cref="NumberUnit"/>）：<c>Opacity = 0.5</c> 不补单位，<c>TransitionDuration = 200</c> 补 ms，其余补 px。
/// </summary>
public readonly record struct CssValue
{
    readonly string? text;
    readonly double number;

    CssValue(string? text, double number)
    {
        this.text = text;
        this.number = number;
    }

    /// <summary>按属性的单位规则写成 CSS 文本。</summary>
    public string Format(NumberUnit unit) => text ?? FormatNumber(number, unit);

    /// <inheritdoc/>
    public override string ToString() => Format(NumberUnit.None);

    /// <summary>字符串原样使用。</summary>
    public static implicit operator CssValue(string text) => new(text, 0);

    /// <summary>整数，单位由属性决定。</summary>
    public static implicit operator CssValue(int number) => new(null, number);

    /// <summary>小数，单位由属性决定。</summary>
    public static implicit operator CssValue(double number) => new(null, number);

    /// <summary>长度当作普通值使用（比如写进不区分类型的属性）。</summary>
    public static implicit operator CssValue(CssLength length) => new(length.ToString(), 0);

    /// <summary>颜色当作普通值使用。</summary>
    public static implicit operator CssValue(CssColor color) => new(color.ToString(), 0);

    internal static string FormatNumber(double number, NumberUnit unit)
    {
        var digits = number.ToString(CultureInfo.InvariantCulture);
        // 0 不带单位：0px 与 0 等价，0ms 在部分属性里不等价但写 0 总是合法的
        if (number == 0) return "0";
        return unit switch
        {
            NumberUnit.Pixels => digits + "px",
            NumberUnit.Milliseconds => digits + "ms",
            _ => digits,
        };
    }
}

/// <summary>长度：数字按 px，字符串原样（<c>"50%"</c>、<c>"2em"</c>、<c>"auto"</c>），也可以是间距、尺寸类的 token。</summary>
public readonly record struct CssLength
{
    readonly string text;

    CssLength(string text) => this.text = text;

    /// <inheritdoc/>
    public override string ToString() => text;

    /// <summary>字符串原样使用。</summary>
    public static implicit operator CssLength(string text) => new(text);

    /// <summary>整数像素。</summary>
    public static implicit operator CssLength(int pixels) => new(CssValue.FormatNumber(pixels, NumberUnit.Pixels));

    /// <summary>小数像素。</summary>
    public static implicit operator CssLength(double pixels) => new(CssValue.FormatNumber(pixels, NumberUnit.Pixels));
}

/// <summary>颜色：任意 CSS 颜色写法（<c>"#3b82f6"</c>、<c>"rgb(0 0 0 / 50%)"</c>、<c>"transparent"</c>），也可以是颜色 token。</summary>
public readonly record struct CssColor
{
    readonly string text;

    CssColor(string text) => this.text = text;

    /// <inheritdoc/>
    public override string ToString() => text;

    /// <summary>字符串原样使用。</summary>
    public static implicit operator CssColor(string text) => new(text);
}

/// <summary>数字值补什么单位（与 StyleX 的规则一致）。</summary>
public enum NumberUnit
{
    /// <summary>补 px（大多数长度属性）。</summary>
    Pixels,

    /// <summary>不补单位（opacity、z-index、line-height、flex-grow 等）。</summary>
    None,

    /// <summary>补 ms（动画、过渡的时长与延迟）。</summary>
    Milliseconds,
}
