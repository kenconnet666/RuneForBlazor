using Microsoft.AspNetCore.Components;

namespace Rune;

/// <summary>
/// 布局的基类（Blazor 的 <see cref="LayoutComponentBase"/> 换成 <see cref="RuneComponent"/>）：布局里也能用 Sx、主题与信号。
/// <code>@inherits RuneLayoutComponent
/// &lt;AppShell&gt;…@Body…&lt;/AppShell&gt;</code>
/// </summary>
public abstract class RuneLayoutComponent : RuneComponent
{
    /// <summary>布局里显示的页面内容。</summary>
    [Parameter] public RenderFragment? Body { get; set; }
}
