using System.Globalization;

namespace Rune.UI;

/// <summary>
/// SVG 里的数字（同 vue-tsx-pro 的 svgNumber）：保留三位小数，四舍五入同 JS 的 Math.round（.5 往正方向进），
/// 写法同 JS 的数字转字符串（最短的往返表示）。服务端与浏览器算出的路径一致，也与 vue 版逐字相同。
/// </summary>
internal static class SvgFormat
{
    /// <summary>保留三位小数。</summary>
    public static double Round(double value) => Math.Floor(value * 1000 + 0.5) / 1000;

    /// <summary>保留三位小数写成字符串。</summary>
    public static string Number(double value) => Round(value) is var rounded && rounded == 0 ? "0" : rounded.ToString(CultureInfo.InvariantCulture);
}
