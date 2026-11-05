using System.Numerics;

namespace Rune.UI;

/// <summary>一段数值范围 [低, 高]：RangeSlider 的值（价格区间、时间段）。</summary>
/// <param name="Low">低端（含）。</param>
/// <param name="High">高端（含）。</param>
/// <typeparam name="T">数值类型。</typeparam>
public readonly record struct NumberRange<T>(T Low, T High) where T : struct, INumber<T>;
