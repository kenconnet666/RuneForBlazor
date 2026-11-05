namespace Rune.E2E;

/// <summary>头像：图片的加载状态由 JS 写在图片上，加载失败时露出后备内容；没有往返，静态 SSR 也一样。</summary>
public class AvatarTests(DemoServer demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 图片加载好后盖住首字母_加载失败时露出首字母(string mode)
    {
        var page = await demo.OpenAsync(mode, "avatar");
        var broken = page.GetByTestId("broken");
        await Assertions.Expect(broken.Locator("img")).ToHaveAttributeAsync("data-state", "error");
        await Assertions.Expect(broken.Locator("img")).ToBeHiddenAsync();
        await Assertions.Expect(broken).ToContainTextAsync("默");

        var photo = page.GetByRole(AriaRole.Img, new() { Name = "林小满（md）" }).Locator("img");
        await Assertions.Expect(photo).ToHaveAttributeAsync("data-state", "loaded");
        Assert.Equal("1", await photo.EvaluateAsync<string>("img => getComputedStyle(img).opacity"));
    }

    [Fact]
    public async Task 角标计数随状态变化_清零后不显示()
    {
        var page = await demo.OpenAsync("server", "avatar");
        var unread = page.GetByTestId("unread");
        await Assertions.Expect(unread).ToContainTextAsync("8 条新消息");
        await page.GetByRole(AriaRole.Button, new() { Name = "来一条消息" }).ClickAsync();
        await Assertions.Expect(unread).ToContainTextAsync("9 条新消息");
        await page.GetByRole(AriaRole.Button, new() { Name = "全部已读" }).ClickAsync();
        await Assertions.Expect(unread.Locator("[data-placement]")).ToHaveCountAsync(0);
    }
}
