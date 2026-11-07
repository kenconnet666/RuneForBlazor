namespace Rune.E2E;

/// <summary>均衡换列（Grid 的 Balanced）：按放得下的列数与项数取列数，最后一行最多空一格；容器变窄后重新取。</summary>
public class GridTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    public async Task 均衡换列_最后一行最多空一格_变窄后重新取(string mode)
    {
        var page = await OpenAsync(mode, "stat");
        var grid = page.Locator("[data-rune-balanced]").First;

        foreach (var width in new[] { 1280, 760 })
        {
            await page.SetViewportSizeAsync(width, 900);
            // 列数（计算后的列轨道数）、项数、按最小宽度放得下几列
            await page.WaitForFunctionAsync("g => g.style.gridTemplateColumns !== ''", await grid.ElementHandleAsync());
            var (columns, count, fits) = await grid.EvaluateAsync<int[]>("""
                g => {
                  const style = getComputedStyle(g);
                  const min = parseFloat(style.getPropertyValue('--rune-grid-min-item-width')) * parseFloat(style.fontSize);
                  const gap = parseFloat(style.columnGap) || 0;
                  const fits = Math.max(1, Math.floor((g.clientWidth + gap) / (Math.min(min, g.clientWidth) + gap)));
                  return [style.gridTemplateColumns.split(' ').length, g.children.length, fits];
                }
                """) switch { var r => (r[0], r[1], r[2]) };
            Assert.Equal(Balanced(count, fits), columns);
            Assert.True(count % columns == 0 || count % columns >= columns - 1, $"{count} 项排 {columns} 列，最后一行空了不止一格");
        }
    }

    static int Balanced(int count, int fits)
    {
        for (var columns = Math.Min(fits, count); columns > 1; columns--)
            if (count % columns is var last && (last == 0 || last >= columns - 1)) return columns;
        return 1;
    }
}
