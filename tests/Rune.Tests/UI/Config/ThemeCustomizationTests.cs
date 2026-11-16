using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 主题定制（同 vue-tsx-pro 的 customize、palette、contrast）：内置主题的配色就是按色阶表取的（indigo + slate）；
/// 换品牌色、中性色整组替换；圆角按主题自己的取值缩放、字号与时长按默认值缩放；对比度按表检查。
/// </summary>
public class ThemeCustomizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 内置主题的配色按色阶表取自indigo与slate(bool dark)
    {
        var theme = dark ? Themes.Dark : Themes.Light;
        var custom = new Theme("probe", theme, ThemeCustomization.Overrides(theme, new() { Accent = AccentScale.Indigo, Neutral = NeutralScale.Slate })
            .Select(o => KeyValuePair.Create(o.Token, o.Value)));

        // 为对比度调整过的（同 vue 的 palette.test 的 adjusted）：暗色的实色悬停调亮
        string[] adjusted = dark ? [Tones.Accent.SolidHover.ToString()] : [];
        foreach (var step in ThemeCustomization.AccentSteps.Concat(ThemeCustomization.NeutralSteps).Where(step => !adjusted.Contains(step.Token.ToString())))
        {
            var token = step.Token.ToString();
            Assert.Equal(ColorValue.ToHex(theme.ValueOf(token)), ColorValue.ToHex(custom.ValueOf(token)));
        }
    }

    [Fact]
    public void 换品牌色整组替换_浅色实色配深色字_没动旋钮时原样返回()
    {
        var crimson = ThemeCustomization.Apply(Themes.Light, new() { Accent = AccentScale.Crimson });
        Assert.Equal("light-custom", crimson.Name);
        Assert.Equal("#e93d82", crimson.ValueOf(Tones.Accent.Solid.ToString()));
        Assert.Equal("#cb1d63", crimson.ValueOf(Tones.Accent.Text.ToString()));
        // 实色上的文字：白字够 4.5:1 就用白字（indigo），不够时换对比度更高的深色（亮黄）；中间亮度的两者都可能不够，取对比度高的
        Assert.Equal("#ffffff", ThemeCustomization.Apply(Themes.Light, new() { Accent = AccentScale.Indigo }).ValueOf(Tones.Accent.Contrast.ToString()));
        Assert.NotEqual("#ffffff", ThemeCustomization.Apply(Themes.Light, new() { Accent = AccentScale.Yellow }).ValueOf(Tones.Accent.Contrast.ToString()));
        foreach (var accent in new[] { AccentScale.Crimson, AccentScale.Yellow, AccentScale.Teal })
        {
            var theme = ThemeCustomization.Apply(Themes.Light, new() { Accent = accent });
            var solid = theme.ValueOf(Tones.Accent.Solid.ToString())!;
            var chosen = ColorContrast.Ratio(theme.ValueOf(Tones.Accent.Contrast.ToString())!, solid);
            Assert.True(chosen >= 4.5 || chosen >= new[] { "#ffffff", "#1c2024" }.Max(text => ColorContrast.Ratio(text, solid)));
        }
        // 只给品牌色时不动中性色
        Assert.Equal(Themes.Light.ValueOf(Colors.Background.ToString()), crimson.ValueOf(Colors.Background.ToString()));

        Assert.Same(Themes.Light, ThemeCustomization.Apply(Themes.Light, new()));
        // 同样的设置得到同一个对象
        Assert.Same(crimson, ThemeCustomization.Apply(Themes.Light, new() { Accent = AccentScale.Crimson }));
    }

    [Fact]
    public void 圆角按主题自己的取值缩放_字号与时长按默认值缩放_标题换衬线()
    {
        var theme = ThemeCustomization.Apply(Themes.Light, new() { Radius = ThemeRadius.Large, Scaling = 1.1, Motion = ThemeMotion.Fast, Font = ThemeFont.SerifHeadings });

        Assert.Equal("0.7em", theme.ValueOf(Radii.Md.ToString()));
        Assert.Equal("9999px", theme.ValueOf(Radii.Full.ToString()));
        Assert.Equal("15.4px", theme.ValueOf(FontSizes.Md.ToString()));
        Assert.Equal("120ms", theme.ValueOf(Durations.Md.ToString()));
        Assert.Equal(Themes.Base.ValueOf(Fonts.Serif.ToString()), theme.ValueOf(Fonts.Heading.ToString()));
        // 减少动态效果时主题的条件块仍把时长写成 0
        Assert.Contains(theme.ConditionalValues, block => block.Value.Any(v => v.Key == "--rune-durations-md" && v.Value == "0ms"));
    }

    [Fact]
    public void 内置主题通过对比度检查_对比度不够的组合列出来()
    {
        Assert.Empty(ThemeContrast.Check(Themes.Light));
        Assert.Empty(ThemeContrast.Check(Themes.Dark));

        var faded = new Theme("faded", Themes.Light, [KeyValuePair.Create(Colors.TextMuted.ToString(), "#c0c0c0")]);
        var issues = ThemeContrast.Check(faded);
        Assert.Contains(issues, issue => issue.Pair.Foreground == "textMuted" && issue.Pair.Background == "surface" && issue.Ratio < 4.5);
    }

    [Fact]
    public void 导出的代码以基础主题的名字开头_写出覆盖的token()
    {
        var code = ThemeCustomization.Code(Themes.Dark, new() { Accent = AccentScale.Grass, Radius = ThemeRadius.None });

        Assert.StartsWith("public static Theme MyTheme => Css.Define(static () => new Theme(\"my-theme\", basedOn: Themes.Dark)", code);
        Assert.Contains("[Tones.Accent.Solid] = \"#46a758\",", code);
        Assert.Contains("[Radii.Md] = \"0em\",", code);
    }
}
