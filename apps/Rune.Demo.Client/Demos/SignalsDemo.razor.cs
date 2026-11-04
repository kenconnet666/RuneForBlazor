using Microsoft.AspNetCore.Components;

namespace Rune.Docs.Client.Demo;

/// <summary>交互演示（信号、双向绑定、样式覆盖）。</summary>
public partial class SignalsDemo : RuneComponent
{
    /// <summary>页面标题。</summary>
    [Parameter, EditorRequired] public string Heading { get; set; } = "";

    /// <summary>受控折叠面板的展开状态：组件里的 [State]，交给 @bind-Open。</summary>
    [State] public partial bool FiltersOpen { get; set; }
}
