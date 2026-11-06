using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class LangTests : BunitContext
{
    public LangTests() => Services.AddRuneUI();

    [Fact]
    public void 内置语言的插值文案翻译成了CSharp()
    {
        Assert.Equal("已输入 3 个字符，最多 20 个", Lang.ZhCn.CharacterCount(3, 20));
        Assert.Equal("已输入 3 个字符", Lang.ZhCn.CharacterCount(3, null));
        Assert.Equal("共 12,345 条", Lang.ZhCn.PaginationTotal(12345));
        Assert.Equal("（已完成）", Lang.ZhCn.StepState("complete"));
        Assert.Equal("二季度", Lang.ZhCn.CalendarQuarter(2));
        Assert.Equal("3 of 20 characters", Lang.EnUs.CharacterCount(3, 20));
        Assert.Same(Lang.ZhCn, Lang.ZhCn);
    }

    [Fact]
    public void 按界面文化选语言包_先认完整代码_再认语言_沿父文化往上()
    {
        Lang[] both = [Lang.ZhCn, Lang.EnUs];
        Assert.Same(Lang.EnUs, Lang.Match(new("en-US"), both));
        Assert.Same(Lang.EnUs, Lang.Match(new("en-GB"), both));
        Assert.Same(Lang.ZhCn, Lang.Match(new("zh-Hans-CN"), both));
        Assert.Same(Lang.ZhCn, Lang.Match(new("zh"), both));
        Assert.Null(Lang.Match(new("fr-FR"), both));
        Assert.Null(Lang.Match(System.Globalization.CultureInfo.InvariantCulture, both));
    }

    [Fact]
    public void 写了Languages时每个作用域按当时的界面文化取语言包()
    {
        var services = new ServiceCollection().AddRuneUI(o => o.Languages = [Lang.ZhCn, Lang.EnUs]).BuildServiceProvider();
        var original = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = new("en-GB");
            using var english = services.CreateScope();
            var config = english.ServiceProvider.GetRequiredService<UIConfig>();
            Assert.Same(Lang.EnUs, config.Lang);
            // 同一作用域里是同一个对象：RuneProvider 据此判断配置没变
            Assert.Same(config, english.ServiceProvider.GetRequiredService<UIConfig>());

            System.Globalization.CultureInfo.CurrentUICulture = new("fr-FR");
            using var french = services.CreateScope();
            Assert.Same(Lang.ZhCn, french.ServiceProvider.GetRequiredService<UIConfig>().Lang);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void 组件的文案默认取语言包_Provider可以换语言()
    {
        var chinese = Render<CloseButton>();
        var english = Render<RuneProvider>(p => p
            .Add(x => x.Lang, Lang.EnUs)
            .AddChildContent<CloseButton>());

        Assert.Equal("关闭", chinese.Find("button").GetAttribute("aria-label"));
        Assert.Equal("Close", english.Find("button").GetAttribute("aria-label"));
        Assert.Equal("en-US", english.Find("[lang]").GetAttribute("lang"));
        Assert.Equal("ltr", english.Find("[lang]").GetAttribute("dir"));
    }

    [Fact]
    public void 改几条用with()
    {
        var custom = Lang.ZhCn with { Close = "关掉" };
        var cut = Render<RuneProvider>(p => p.Add(x => x.Lang, custom).AddChildContent<CloseButton>());

        Assert.Equal("关掉", cut.Find("button").GetAttribute("aria-label"));
        Assert.Equal("清空", custom.Clear);
    }
}
