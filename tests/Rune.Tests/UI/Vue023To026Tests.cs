using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>跟进 vue-tsx-pro 0.23.0–0.26.0 的行为修正与新参数：每条对应上游改过的一处。</summary>
public class Vue023To026Tests : BunitContext
{
    public Vue023To026Tests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Theory]
    [InlineData("张三😀", "😀")]
    [InlineData("😀 smile", "😀S")]
    [InlineData("𠮷野", "野")]
    public void 头像的首字母按码点取_不把表情切成半个(string name, string expected) =>
        Assert.Equal(expected, AvatarStyles.Initials(name));

    [Fact]
    public void 头像只是装饰时对读屏隐藏_名字仍用来生成首字母()
    {
        var cut = Render<Avatar>(p => p.Add(x => x.Name, "陈默").Add(x => x.Decorative, true));

        var root = cut.Find("span");
        Assert.Equal("true", root.GetAttribute("aria-hidden"));
        Assert.False(root.HasAttribute("role"));
        Assert.Contains("默", root.TextContent);
    }

    [Fact]
    public void 用户按钮里名字写在旁边时头像是装饰_只剩头像时不是()
    {
        var full = Render<UserButton>(p => p.Add(x => x.Name, "陈默"));
        Assert.Equal("true", full.Find("""[data-scope="user-button"][data-part="avatar"]""").GetAttribute("aria-hidden"));

        var compact = Render<UserButton>(p => p.Add(x => x.Name, "陈默").Add(x => x.Compact, true));
        Assert.Equal("img", compact.Find("""[data-scope="user-button"][data-part="avatar"]""").GetAttribute("role"));
    }

    [Fact]
    public void 分页不可用时页码与上一页下一页都不能点()
    {
        var page = 2;
        var cut = Render<Pagination>(p => p.Add(x => x.Page, page).Add(x => x.PageChanged, v => page = v).Add(x => x.PageCount, 5).Add(x => x.Disabled, true));

        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public void 分页条不可用时页码_每页条数_跳页都不能操作()
    {
        var cut = Render<PaginationBar>(p => p.Add(x => x.Total, 200).Add(x => x.Disabled, true));

        Assert.All(cut.FindAll("nav button"), button => Assert.True(button.HasAttribute("disabled")));
        Assert.All(cut.FindAll("input"), input => Assert.True(input.HasAttribute("disabled")));
    }

    [Fact]
    public void 开关的说明在文字下面_读屏在名称之后读出()
    {
        var cut = Render<Switch>(p => p.Add(x => x.Description, "有新的审批时发邮件").AddChildContent("接收通知"));

        var description = cut.Find("""[data-part="description"]""");
        Assert.Equal("有新的审批时发邮件", description.TextContent);
        Assert.Equal(description.Id, cut.Find("input").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void 静态表格没有行时表体里一行占满_默认暂无数据()
    {
        var cut = Render<ColumnTable<Row>>(p => p.Add(x => x.Items, []).AddChildContent<PropertyColumn<Row, string>>(c => c.Add(x => x.Property, r => r.Name).Add(x => x.Title, "名称"))
            .AddChildContent<PropertyColumn<Row, int>>(c => c.Add(x => x.Property, r => r.Count).Add(x => x.Title, "数量")));

        var cell = cut.WaitForElement("tbody > tr > td");
        Assert.Equal("2", cell.GetAttribute("colspan"));
        Assert.Equal("暂无数据", cell.TextContent.Trim());
    }

    [Fact]
    public void 列表框只读时带aria_readonly()
    {
        var cut = Render<Listbox<string>>(p => p.Add(x => x.Items, ["甲", "乙"]).Add(x => x.ReadOnly, true).AddUnmatched("aria-label", "选项"));

        Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-readonly"));
    }

    [Fact]
    public void 加载遮罩_外观延迟出现之前先盖一层拦住指针()
    {
        // 一开始就在加载的直接显示遮罩；加载是后来开始的才延迟
        var cut = Render<LoadingOverlay>(p => p.Add(x => x.Loading, false).AddChildContent("<button>保存</button>"));
        cut.Render(p => p.Add(x => x.Loading, true));

        // 遮罩（带加载指示）还藏着，前面有一层看不见的
        Assert.True(cut.Find("""[data-part="overlay"]""").HasAttribute("hidden"));
        Assert.NotNull(cut.Find("""[data-part="root"] > div[aria-hidden="true"]:not([data-part])"""));

        var idle = Render<LoadingOverlay>(p => p.Add(x => x.Loading, false).AddChildContent("<button>保存</button>"));
        Assert.Empty(idle.FindAll("""[data-part="root"] > div[aria-hidden="true"]:not([data-part])"""));
    }

    [Fact]
    public void 速拨的主按钮没写名称时用语言包的默认名称()
    {
        var cut = Render<FloatButtonGroup>(p => p.Add(x => x.Icon, BuiltInIcons.Plus).AddChildContent("<span></span>"));

        Assert.Equal("更多操作", cut.Find("[data-rune-speed-dial-trigger]").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(999_999, "1 MB")]
    [InlineData(999_949, "999.9 kB")]
    [InlineData(999, "999 B")]
    [InlineData(1_000, "1 kB")]
    [InlineData(999_999_999, "1 GB")]
    public void 文件大小取整后进到一千的换下一个单位(long bytes, string expected) => Assert.Equal(expected, LangFormat.FileSize(bytes, "en-US"));

    [Fact]
    public void 语言没配置时Provider不往元素上写lang与dir_配置了才写()
    {
        var plain = Render<RuneProvider>(p => p.AddChildContent("<span></span>"));
        var wrapper = plain.Find("[data-rune-theme]");
        Assert.False(wrapper.HasAttribute("lang"));
        Assert.False(wrapper.HasAttribute("dir"));

        var english = Render<RuneProvider>(p => p.Add(x => x.Lang, Lang.EnUs).AddChildContent("<span></span>"));
        Assert.Equal(("en-US", "ltr"), (english.Find("[lang]").GetAttribute("lang"), english.Find("[lang]").GetAttribute("dir")));
    }

    [Fact]
    public void 应用配置了语言时最外层的Provider写lang与dir()
    {
        using var context = new BunitContext();
        context.Services.AddRuneUI(options => options.Lang = Lang.EnUs);

        var cut = context.Render<RuneProvider>(p => p.AddChildContent("<span></span>"));
        Assert.Equal("en-US", cut.Find("[data-rune-theme]").GetAttribute("lang"));
    }

    [Fact]
    public void 对齐的双轴_全是负数时上端是0不是1()
    {
        var axes = AxisTicks.Aligned([-120, -80, -40], [-0.3, -0.1]);

        Assert.Equal(0, axes.Primary.Ticks.Max());
        Assert.True(axes.Primary.Ticks.Min() <= -120);
        Assert.True(axes.Secondary.Ticks.Max() <= 0.000001);
    }

    sealed record Row(string Name, int Count);
}
