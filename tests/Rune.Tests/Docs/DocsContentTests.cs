using Microsoft.AspNetCore.Components;
using Rune.Docs.Components.Input.Rating.Demo;
using Rune.Docs.Site;
using Rune.UI;

namespace Rune.Tests.Docs;

public class DocsContentTests
{
    [Fact]
    public void 目录按分类排好_地址不重复_每页都有标题与简介()
    {
        var all = DocsCatalog.All;
        Assert.True(all.Count >= 50);
        Assert.Equal(all.Select(p => p.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count(), all.Count);
        Assert.Equal(DocCategory.Guide, all[0].Meta.Category);
        Assert.All(all, p => Assert.False(string.IsNullOrWhiteSpace(p.Meta.Description), p.Path));
        Assert.Equal("Rating 评分", DocsCatalog.Find("rating")!.Meta.Title);
        Assert.Equal("/components/rating", DocsCatalog.FindByPath("components/rating/")!.Path);
        // 分类的顺序与侧栏一致
        var categories = DocsCatalog.Sections.Select(s => s.Category).ToList();
        Assert.Equal(categories.Order().ToList(), categories);
        Assert.NotNull(DocsCatalog.FindFrame("app-shell", "admin"));
    }

    [Fact]
    public void 示例的源码就是示例文件()
    {
        var source = ExampleSource.Of(typeof(VariantsExample));
        Assert.StartsWith("@inherits RuneComponent\n", source);
        Assert.Contains("<Rating @bind-Value=\"like\"", source);
        Assert.Contains("Css.Marker(\"demo-folder-row\")", ExampleSource.Of(typeof(Rune.Docs.Guide.Demo.StyleStatesExample)));
        Assert.Throws<ArgumentException>(() => ExampleSource.Of(typeof(DocsContentTests)));
    }

    [Fact]
    public void API表_种类_可绑定_必填_默认值_枚举的可选值与说明()
    {
        var api = ComponentApiReader.Read(typeof(Collapsible));
        var open = api.Props.Single(p => p.Name == nameof(Collapsible.Open));
        Assert.Equal(ApiPropKind.Prop, open.Kind);
        Assert.True(open.Bindable);
        Assert.Equal("bool", open.Type);
        Assert.Equal(ApiPropKind.Event, api.Props.Single(p => p.Name == nameof(Collapsible.OpenChanged)).Kind);
        Assert.Equal(ApiPropKind.Content, api.Props.Single(p => p.Name == nameof(Collapsible.ChildContent)).Kind);
        Assert.Equal(ApiPropKind.Part, api.Props.Single(p => p.Name == nameof(Collapsible.PropsContent)).Kind);
        // 自己声明的参数在前，基类的（Sx）在后
        Assert.Equal("Sx", api.Props[^1].Name);

        var accordion = ComponentApiReader.Read(typeof(Accordion));
        var mode = accordion.Props.Single(p => p.Name == nameof(Accordion.SelectionMode));
        Assert.Equal("SelectionMode", mode.Type);
        Assert.Equal("SelectionMode.Single", mode.Default);
        Assert.Contains(mode.Values!, v => v.Value == nameof(SelectionMode.Multiple) && v.Description.Length > 0);
        Assert.False(string.IsNullOrEmpty(mode.Description));

        var rating = ComponentApiReader.Read(typeof(Rating));
        Assert.Equal("5", rating.Props.Single(p => p.Name == nameof(Rating.Count)).Default);
        Assert.Null(rating.Props.Single(p => p.Name == nameof(Rating.ReadOnly)).Default);

        // 泛型组件按满足约束的类型参数读（Slider 的 TValue 是数字）
        Assert.Equal("Slider<TValue>", ComponentApiReader.Read(typeof(Slider<>)).Component);
    }

    [Fact]
    public void API表_数据类型列公开属性_主构造参数与required成员必填_默认值取初始值()
    {
        // record 的主构造参数：没有无参构造也能读，参数排在前面、必填
        var item = ComponentApiReader.Read(typeof(TimelineItem));
        Assert.Equal(nameof(TimelineItem.Title), item.Props[0].Name);
        Assert.True(item.Props[0].Required);
        Assert.Equal(ApiPropKind.Content, item.Props.Single(p => p.Name == nameof(TimelineItem.Details)).Kind);

        var upload = ComponentApiReader.Read(typeof(UploadFile));
        Assert.True(upload.Props.Single(p => p.Name == nameof(UploadFile.Id)).Required);
        Assert.False(upload.Props.Single(p => p.Name == nameof(UploadFile.Url)).Required);

        var format = ComponentApiReader.Read(typeof(NumberFormat));
        Assert.Equal("1", format.Props.Single(p => p.Name == nameof(NumberFormat.Scale)).Default);
        Assert.Equal("true", format.Props.Single(p => p.Name == nameof(NumberFormat.UseGrouping)).Default);
    }

    [Fact]
    public void 本页目录_按登记顺序_API一组排在最后_注销后去掉()
    {
        var headings = new DocHeadings();
        var api = new object();
        var first = new object();
        var changes = 0;
        headings.Changed += () => changes++;

        headings.Add(api, new("api", "API"), DocHeadings.Last);
        headings.Add(first, new("basic", "基础用法"));
        headings.Add(first, new("basic", "基础用法"));
        Assert.Equal(["basic", "api"], headings.All.Select(h => h.Id));
        Assert.Equal(2, changes);

        headings.Remove(first);
        Assert.Equal(["api"], headings.All.Select(h => h.Id));
    }
}
