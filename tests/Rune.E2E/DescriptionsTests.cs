namespace Rune.E2E;

/// <summary>描述列表：自适应栏数随容器宽度变化（容器查询），跨栏按栏数封顶。</summary>
public class DescriptionsTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 自适应栏数随容器宽度变化_跨栏封顶(string mode)
    {
        var page = await OpenAsync(mode, "descriptions");
        var box = page.GetByTestId("resizable");
        var list = page.GetByTestId("wide").Locator("dl");
        var address = list.Locator("> div").Last;

        async Task<(int Columns, int AddressWidth)> MeasureAt(int width)
        {
            await box.EvaluateAsync("(el, w) => el.style.width = w + 'px'", width);
            return (await list.EvaluateAsync<int>("el => getComputedStyle(el).gridTemplateColumns.split(' ').length"),
                (int)Math.Round(await address.EvaluateAsync<double>("el => el.getBoundingClientRect().width / el.parentElement.getBoundingClientRect().width * 100")));
        }

        // 盒子有 1rem 内边距：内容宽 = 宽度 - 32
        Assert.Equal((1, 100), await MeasureAt(300));
        Assert.Equal(2, (await MeasureAt(500)).Columns);
        Assert.Equal((3, 100), await MeasureAt(760));
        Assert.Equal(4, (await MeasureAt(1000)).Columns);
        Assert.InRange((await MeasureAt(1000)).AddressWidth, 70, 80);

        // 窄卡片里一直是一栏
        Assert.Equal(1, await page.GetByTestId("narrow").Locator("dl").EvaluateAsync<int>("el => getComputedStyle(el).gridTemplateColumns.split(' ').length"));
    }
}
