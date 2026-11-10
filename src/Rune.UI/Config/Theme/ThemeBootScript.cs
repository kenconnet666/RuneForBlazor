using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>
/// 主题的防闪烁脚本（同 vue-tsx-pro 的 themeBootScript）：放在 App.razor 的 head 里，页面显示前按记住的选择
/// 给根元素写上 <c>data-theme</c>（RuneProvider 的主题类据此取那一套）与底色、文字色、配色方案。
/// <code>&lt;head&gt; … &lt;ThemeBootScript /&gt; &lt;/head&gt;</code>
/// 可选的主题取 <see cref="RuneUIOptions.ThemeChoices"/>。
/// 配置写在 <c>script#rune-theme-config</c>（JSON）里，之后的切换（<c>js/core/theme.js</c>）也读它。
/// Blazor Hybrid（MAUI、WPF、WinForms 的 BlazorWebView）没有 Razor 的 head：放在根组件（Routes）的开头，
/// 由 Blazor 插进页面，插入的脚本不执行，只输出配置，Rune 的脚本看到配置后应用。
/// </summary>
public sealed class ThemeBootScript : ComponentBase
{
    // 读记住的选择（隐私模式下存储会抛错）→ 解析跟随系统（先按对比度选一对，再按明暗）→ 写根元素；没有这套主题的颜色时只记下选择
    const string Script =
        "(()=>{const c=JSON.parse(document.getElementById('rune-theme-config').textContent);const r=document.documentElement;" +
        "let p;try{p=localStorage.getItem(c.key)}catch(e){}if(p!=='system'&&!c.themes[p])p=c.fallback;" +
        "const s=c.system.contrast&&matchMedia('(prefers-contrast: more)').matches?c.system.contrast:c.system;" +
        "const n=p==='system'?(matchMedia('(prefers-color-scheme: dark)').matches?s.dark:s.light):p;" +
        "r.dataset.themePreference=p;const t=c.themes[n];if(!t)return;r.dataset.theme=n;" +
        "r.style.colorScheme=t.colorScheme;r.style.backgroundColor=t.background;r.style.color=t.text})()";

    /// <summary>可选的主题；不写时取 <see cref="RuneUIOptions.ThemeChoices"/>。</summary>
    [Parameter] public ThemeChoices? Choices { get; set; }

    [Inject] UIConfig Config { get; set; } = default!;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // 组件里不能直接写 <script>：作为标记内容输出；JSON 里的 < 已被转义，不会提前结束 script。
        // 交互渲染时（Hybrid 的根组件里）由 Blazor 插进页面，脚本不会执行，不输出
        var config = $"<script type=\"application/json\" id=\"rune-theme-config\">{(Choices ?? Config.ThemeChoices ?? throw new InvalidOperationException("ThemeBootScript 要知道可选的主题：在 AddRuneUI 里设置 ThemeChoices")).ToJson()}</script>";
        builder.AddMarkupContent(0, RendererInfo.IsInteractive ? config : $"{config}<script>{Script}</script>");
    }
}
