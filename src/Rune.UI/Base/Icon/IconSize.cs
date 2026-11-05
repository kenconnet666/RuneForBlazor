namespace Rune.UI;

/// <summary>
/// 图标的大小：字号档位或像素（同 vue-tsx-pro 的 size：档位名或数字）。
/// <c>&lt;Icon Size="Size.Xl" /&gt;</c>、<c>&lt;Icon Size="40" /&gt;</c>；不写时是 1em，跟随所在文字。
/// </summary>
public readonly record struct IconSize
{
    IconSize(Size? scale, double? pixels)
    {
        Scale = scale;
        Pixels = pixels;
    }

    /// <summary>字号档位（对应主题字号）；给的是像素时为 null。</summary>
    public Size? Scale { get; }

    /// <summary>像素；给的是档位时为 null。</summary>
    public double? Pixels { get; }

    /// <summary>字号档位。</summary>
    public static implicit operator IconSize(Size scale) => new(scale, null);

    /// <summary>像素。</summary>
    public static implicit operator IconSize(int pixels) => new(null, pixels);

    /// <summary>像素。</summary>
    public static implicit operator IconSize(double pixels) => new(null, pixels);
}
