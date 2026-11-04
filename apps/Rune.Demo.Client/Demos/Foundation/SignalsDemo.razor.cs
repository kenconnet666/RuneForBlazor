namespace Rune.Demo.Client.Demos.Foundation;

/// <summary>信号与共享状态的演示。</summary>
public partial class SignalsDemo : RuneComponent
{
    /// <summary>折叠面板的展开状态：组件里的 [State]，交给 @bind-Open。</summary>
    [State] public partial bool FiltersOpen { get; set; }
}
