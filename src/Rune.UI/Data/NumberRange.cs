namespace Rune.UI;

/// <summary>
/// 一段数值区间 [低, 高]：Slider 的范围（<c>NumberRange&lt;int&gt;</c>）、NumberRangeField 的值（端点用可空类型时一端可以空着，
/// <c>NumberRange&lt;decimal?&gt;(20, null)</c> 是“20 以上”）。
/// </summary>
/// <param name="Low">低端（含）。</param>
/// <param name="High">高端（含）。</param>
/// <typeparam name="T">端点的类型：数字，或可空的数字。</typeparam>
public readonly record struct NumberRange<T>(T Low, T High);
