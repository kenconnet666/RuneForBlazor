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

    /// <summary>关键字当作普通值使用。</summary>
    public static implicit operator CssValue(CssKeyword keyword) => new(keyword.ToString(), 0);

    /// <summary>角度（度）：<c>Rotate = CssValue.Deg(180)</c>。数字直接写在 rotate 上不带单位，是无效值。</summary>
    public static CssValue Deg(double degrees) => new(Number(degrees) + "deg", 0);

    /// <summary>角度（圈）：<c>CssValue.Turn(0.5)</c> 即半圈。</summary>
    public static CssValue Turn(double turns) => new(Number(turns) + "turn", 0);

    internal static string Number(double number) => number.ToString(CultureInfo.InvariantCulture);

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

/// <summary>
/// 长度：间距、尺寸类的 token，带单位的写法（<c>CssLength.Em(0.875)</c>、<c>CssLength.Percent(100)</c>），常用关键字（<see cref="Auto"/>），
/// 或字符串原样（<c>"calc(100vw - 2em)"</c>）。数字按 px：组件内部的尺寸用 em（随字号整体缩放），px 只留给边框一类的细线（经 BorderWidths token）。
/// </summary>
public readonly record struct CssLength
{
    readonly string text;

    CssLength(string text) => this.text = text;

    /// <summary><c>auto</c>。</summary>
    public static readonly CssLength Auto = "auto";

    /// <summary><c>none</c>（max-width、translate 等）。</summary>
    public static readonly CssLength None = "none";

    /// <summary>em：相对元素自己的字号，组件内部尺寸都用它。</summary>
    public static CssLength Em(double value) => Unit(value, "em");

    /// <summary>rem：相对根元素字号（页面级的宽度、断点）。</summary>
    public static CssLength Rem(double value) => Unit(value, "rem");

    /// <summary>百分比。</summary>
    public static CssLength Percent(double value) => Unit(value, "%");

    /// <summary>视口宽度的百分之几。</summary>
    public static CssLength Vw(double value) => Unit(value, "vw");

    /// <summary>最近的容器查询容器（container-type）行内方向尺寸的百分之几。</summary>
    public static CssLength Cqi(double value) => Unit(value, "cqi");

    /// <summary>视口高度的百分之几（动态视口：手机地址栏收起、展开时跟着变）。</summary>
    public static CssLength Dvh(double value) => Unit(value, "dvh");

    /// <summary>字符宽（0 的宽度）：按字数定宽的输入框。</summary>
    public static CssLength Ch(double value) => Unit(value, "ch");

    static CssLength Unit(double value, string unit) => new(value == 0 ? "0" : CssValue.Number(value) + unit);

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

/// <summary>
/// 只收文字的值：关键字（<c>Display.Flex</c>、<c>CssWide.Inherit</c>）与组合写法（<c>"opacity 120ms ease"</c>）。
/// 值里没有数字的属性（display、cursor、font……）用它：写数字编译不过，不会输出 <c>5px</c> 这样的无效值。
/// </summary>
public readonly record struct CssKeyword
{
    readonly string text;

    CssKeyword(string text) => this.text = text;

    /// <inheritdoc/>
    public override string ToString() => text;

    /// <summary>字符串原样使用。</summary>
    public static implicit operator CssKeyword(string text) => new(text);
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
