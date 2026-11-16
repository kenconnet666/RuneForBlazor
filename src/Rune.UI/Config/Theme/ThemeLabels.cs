namespace Rune.UI;

public static partial class Themes
{
    /// <summary>
    /// 内置主题与“跟随系统”在当前语言下的名字（同 vue-tsx-pro 的 builtinThemeLabel）；不是内置主题时返回 null。
    /// ThemeSwitcher 的菜单、选主题的网格用它。
    /// </summary>
    /// <param name="lang">语言包。</param>
    /// <param name="name">主题名（Theme.Name），或 <see cref="ThemeChoices.System"/>。</param>
    public static string? LabelOf(Lang lang, string name) => name switch
    {
        ThemeChoices.System => lang.ThemeSystem,
        "light" => lang.ThemeLight,
        "dark" => lang.ThemeDark,
        "paper" => lang.ThemePaper,
        "midnight" => lang.ThemeMidnight,
        "high-contrast" => lang.ThemeHighContrast,
        "high-contrast-dark" => lang.ThemeHighContrastDark,
        "forest" => lang.ThemeForest,
        "lemon" => lang.ThemeLemon,
        "sakura" => lang.ThemeSakura,
        "festive" => lang.ThemeFestive,
        "cyberpunk" => lang.ThemeCyberpunk,
        "big-screen" => lang.ThemeBigScreen,
        "lavender" => lang.ThemeLavender,
        "ember" => lang.ThemeEmber,
        "mono" => lang.ThemeMono,
        "celadon" => lang.ThemeCeladon,
        _ => null,
    };
}
