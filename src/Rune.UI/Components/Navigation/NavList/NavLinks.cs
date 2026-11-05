using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Rune.UI;

/// <summary>导航链接的地址与当前页判断（同 vue-tsx-pro 路由适配的 href / isActive）。</summary>
public static class NavLinks
{
    /// <summary>
    /// 链接实际写到 href 上的地址：只有锚点（<c>#orders</c>）时接在当前页后面。
    /// Blazor 按 &lt;base href&gt; 解析相对地址，<c>#orders</c> 会变成站点根目录下的锚点，点了就离开了当前页。
    /// </summary>
    public static string Resolve(NavigationManager navigation, string href) =>
        href.StartsWith('#') ? WithoutFragment(navigation.Uri) + href : href;

    /// <summary>
    /// 是不是当前页：比较路径（不分大小写，忽略末尾的 /，不看查询串）；地址带锚点时锚点也要相同。
    /// <see cref="NavLinkMatch.Prefix"/> 时当前页在它下面也算（/orders 在 /orders/42 页面也高亮），同 Blazor 的 NavLink。
    /// </summary>
    public static bool IsCurrent(NavigationManager navigation, string href, NavLinkMatch match)
    {
        var target = navigation.ToAbsoluteUri(Resolve(navigation, href));
        var current = new Uri(navigation.Uri);
        if (target.Fragment.Length > 0 && target.Fragment != current.Fragment) return false;
        var targetPath = Path(target);
        var currentPath = Path(current);
        if (!string.Equals(target.GetLeftPart(UriPartial.Authority), current.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(targetPath, currentPath, StringComparison.OrdinalIgnoreCase)) return true;
        return match == NavLinkMatch.Prefix && currentPath.StartsWith(targetPath + "/", StringComparison.OrdinalIgnoreCase);
    }

    static string Path(Uri uri) => uri.AbsolutePath.TrimEnd('/');

    static string WithoutFragment(string uri) => uri.IndexOf('#') is var hash and >= 0 ? uri[..hash] : uri;
}
