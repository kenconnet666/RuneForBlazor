using Rune.UI;

namespace Rune.Tests.UI;

// 期望值由 vue-tsx-pro 用的 culori 4.0.2 算出（parse → rgb → formatHex / formatHex8）
public class ColorValueTests
{
    [Theory]
    [InlineData("#abc", "#aabbcc")]
    [InlineData("#abcd", "#aabbccdd")]
    [InlineData("#2563EB", "#2563eb")]
    [InlineData("#2563eb80", "#2563eb80")]
    [InlineData("rgb(37, 99, 235)", "#2563eb")]
    [InlineData("rgba(37,99,235,0.5)", "#2563eb80")]
    [InlineData("rgb(37 99 235 / 50%)", "#2563eb80")]
    [InlineData("rgb(10% 50% 100%)", "#1a80ff")]
    [InlineData("hsl(220, 83%, 53%)", "#2466eb")]
    [InlineData("hsl(220deg 83% 53% / 0.25)", "#2466eb40")]
    [InlineData("hsl(0.5turn 100% 50%)", "#00ffff")]
    [InlineData("hwb(220 10% 20%)", "#1a55cc")]
    [InlineData("hwb(0 60% 60%)", "#808080")]
    [InlineData("oklch(0.62 0.19 260)", "#3a81f6")]
    [InlineData("oklch(62% 0.19 260 / 0.8)", "#3a81f6cc")]
    [InlineData("oklab(0.7 0.1 -0.1)", "#bf81da")]
    [InlineData("oklch(0.9 0.4 30)", "#ff0000")]
    [InlineData("rebeccapurple", "#663399")]
    [InlineData("Transparent", "#00000000")]
    [InlineData("RED", "#ff0000")]
    public void 各种写法规范成hex_同culori(string text, string hex) => Assert.Equal(hex, ColorValue.ToHex(text));

    [Theory]
    [InlineData("")]
    [InlineData("#12")]
    [InlineData("#gggggg")]
    [InlineData("rgb(1, 2)")]
    [InlineData("blurple")]
    [InlineData("lab(50 20 30)")]
    public void 解析不了时是null(string text) => Assert.Null(ColorValue.ToHex(text));
}
