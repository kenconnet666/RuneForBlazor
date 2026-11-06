using System.Text.Json;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 用户可以选的几套主题（同 vue-tsx-pro 的 createThemePreference）：选“跟随系统”或某一套，记在浏览器的本地存储里，下次打开、其他标签页都沿用。
/// 写在 <see cref="RuneUIOptions.ThemeChoices"/>：最外层的 RuneProvider 输出每套主题的变量（按根元素的 <c>data-theme</c> 取用），head 里的 ThemeBootScript 页面显示前按记住的选择设好；
/// ThemeSwitcher 列出它们。切换全在浏览器里（<c>js/theme.js</c>），不用往返、不用重新渲染，三种渲染模式一样。
/// <code>
/// builder.Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);   // 服务端与 WebAssembly 项目都写
/// // App.razor 的 head：&lt;ThemeBootScript /&gt;；页面里放 &lt;ThemeSwitcher /&gt;
/// </code>
/// </summary>
/// <param name="Themes">可选的主题，按列出的顺序；名字（<see cref="Theme.Name"/>）要唯一。</param>
public sealed record ThemeChoices(IReadOnlyList<Theme> Themes)
{
    /// <summary>“跟随系统”的取值。</summary>
    public const string System = "system";

    /// <summary>全部内置主题（亮色、暗色、纸张……），跟随系统时用亮色、暗色。</summary>
    public static ThemeChoices Builtin => field ??= new(UI.Themes.All);

    /// <summary>跟随系统、系统是亮色时用的主题，默认 <see cref="UI.Themes.Light"/>。</summary>
    public Theme SystemLight { get; init; } = UI.Themes.Light;

    /// <summary>跟随系统、系统是暗色时用的主题，默认 <see cref="UI.Themes.Dark"/>。</summary>
    public Theme SystemDark { get; init; } = UI.Themes.Dark;

    /// <summary>记住选择的 localStorage 键，默认 rune-theme。</summary>
    public string StorageKey { get; init; } = "rune-theme";

    /// <summary>没记过（或记的主题已经没有了）时的选择：<see cref="System"/> 或某套主题的名字，默认跟随系统。</summary>
    public string Fallback { get; init; } = System;

    /// <summary>
    /// 交给浏览器端的配置（JSON）：存储键、默认选择、跟随系统的两套，以及每套主题的底色、文字色与配色方案
    /// （页面显示前就把根元素的颜色设对，不闪一下默认主题）。
    /// </summary>
    public string ToJson()
    {
        // 手写而不是序列化匿名对象：不用反射，WebAssembly 裁剪后也能用
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("key", StorageKey);
            json.WriteString("fallback", Fallback);
            json.WriteStartObject("system");
            json.WriteString("light", SystemLight.Name);
            json.WriteString("dark", SystemDark.Name);
            json.WriteEndObject();
            json.WriteStartObject("themes");
            foreach (var theme in Themes)
            {
                json.WriteStartObject(theme.Name);
                json.WriteString("background", theme.ValueOf(Colors.Background.ToString()));
                json.WriteString("text", theme.ValueOf(Colors.Text.ToString()));
                json.WriteString("colorScheme", theme.IsDark ? "dark" : "light");
                json.WriteEndObject();
            }
            json.WriteEndObject();
            json.WriteEndObject();
        }
        return global::System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
}
