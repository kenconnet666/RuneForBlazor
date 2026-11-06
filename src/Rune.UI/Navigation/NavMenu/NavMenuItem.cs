namespace Rune.UI;

/// <summary>
/// 菜单树的一项（同 vue-tsx-pro 的 NavMenuItem / NavBarItem）：同一份菜单数据可以同时给顶部的 NavBar 和侧栏的 NavMenu，多层嵌套。
/// </summary>
/// <param name="Label">显示的文字。</param>
/// <param name="Href">地址；有可显示的子项时这一项是一组，不用写。</param>
/// <param name="Icon">图标。</param>
/// <param name="Badge">计数（待办数）；0 时不显示。</param>
/// <param name="Disabled">不可用。</param>
/// <param name="Active">是否是当前页；不写时按地址判断。</param>
/// <param name="Children">子项。</param>
/// <param name="Key">同一层里唯一的标识（列表的 key），默认取 Href，没有 Href 时取 Label。</param>
public sealed record NavMenuItem(
    string Label,
    string? Href = null,
    IconData? Icon = null,
    int Badge = 0,
    bool Disabled = false,
    bool? Active = null,
    IReadOnlyList<NavMenuItem>? Children = null,
    string? Key = null)
{
    /// <summary>这一项（或它的子孙）有没有能显示的页面：没有的目录不画。</summary>
    public bool IsVisible => Children is { Count: > 0 } children ? children.Any(child => child.IsVisible) : Href is not null;

    internal string KeyOrDefault => Key ?? Href ?? Label;
}

/// <summary>顶层有子项的项怎么显示。</summary>
public enum NavMenuTopLevel
{
    /// <summary>带小标题的小节，一直展开（默认）。</summary>
    Section,

    /// <summary>可收起的分组。</summary>
    Group,
}
