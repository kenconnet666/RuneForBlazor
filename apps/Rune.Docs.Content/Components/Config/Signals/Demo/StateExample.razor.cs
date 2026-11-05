namespace Rune.Docs.Components.Config.Signals.Demo;

/// <summary>组件里的 [State] 与双向绑定。</summary>
public partial class StateExample : RuneComponent
{
    /// <summary>折叠面板的展开状态：组件里的 [State]，交给 @bind-Open。</summary>
    [State] public partial bool FiltersOpen { get; set; }
}
