namespace Rune.Demo.Client.Infrastructure;

/// <summary>
/// 把一个组件登记成演示：演示页按它生成目录，并能以四种渲染模式打开（<c>/server/collapsible</c>）。
/// 新增演示只要在 Demos 里放一个组件并写上：
/// <code>@attribute [ComponentDemo("collapsible", "折叠面板", Group = "展示")]</code>
/// </summary>
/// <param name="slug">地址里的名字（连字符小写，全局唯一）。</param>
/// <param name="title">目录里显示的名字。</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ComponentDemoAttribute(string slug, string title) : Attribute
{
    /// <summary>地址里的名字。</summary>
    public string Slug { get; } = slug;

    /// <summary>目录里显示的名字。</summary>
    public string Title { get; } = title;

    /// <summary>目录里的分组，默认“组件”。</summary>
    public string Group { get; init; } = "组件";

    /// <summary>同组内的排序，小的在前。</summary>
    public int Order { get; init; }
}

/// <summary>
/// 整页示例（同 vue-tsx-pro 文档站的 frames）：AppShell 这类按视口排版、占整页的组件单独渲染在 <c>/{模式}/frame/{演示}/{名字}</c>，
/// 演示页的 DemoSection 写 <c>Frame="名字"</c> 用 iframe 打开它。
/// <code>@attribute [DemoFrame("app-shell", "admin")]</code>
/// </summary>
/// <param name="slug">所属演示的名字。</param>
/// <param name="name">示例的名字（演示内唯一）。</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DemoFrameAttribute(string slug, string name) : Attribute
{
    /// <summary>所属演示的名字。</summary>
    public string Slug { get; } = slug;

    /// <summary>示例的名字。</summary>
    public string Name { get; } = name;
}
