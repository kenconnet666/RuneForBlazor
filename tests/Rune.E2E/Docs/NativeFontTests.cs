namespace Rune.E2E;

/// <summary>
/// 原生控件继承字体（同 vue-tsx-pro 文档站 demos.browser.test.ts 的检查）：input、button、select、textarea 默认不继承，
/// Chromium 给 13.333px 与系统字体，按 em 定尺寸的方框、轨道就和文字、滑块按两套字号算。
/// 不和浏览器的默认值比（WebKit 的按钮默认 13px，正好等于写了 13px 的小按钮）：在最前面插一层探针样式（最先声明的层优先级最低），
/// 组件写了字体（inherit 或具体值）就会盖过它；看得见的控件读出来还是探针的字号或字体，就是没管字体。
/// </summary>
public class NativeFontTests(DemoServer demo) : E2ETest(demo)
{
    [Theory]
    [InlineData("checkbox")]
    [InlineData("color-field")]
    [InlineData("otp-field")]
    [InlineData("text-field")]
    [InlineData("number-field")]
    [InlineData("select")]
    [InlineData("tree")]
    [InlineData("date-field")]
    [InlineData("calendar")]
    [InlineData("button")]
    public async Task 原生控件继承外层的字体(string name)
    {
        var page = await OpenAsync("server", name);
        var wrong = await page.EvaluateAsync<string[]>("""
            () => {
              const probe = document.createElement('style');
              probe.textContent = '@layer font-probe { :where(input:not([type="hidden"]), button, select, textarea) { font-size: 77px; font-family: font-probe; } }';
              document.head.prepend(probe);
              try {
                return [...document.querySelectorAll('input:not([type="hidden"]), button, select, textarea')]
                  .filter((element) => {
                    const box = element.getBoundingClientRect();
                    return box.width > 1 && box.height > 1;
                  })
                  .filter((element) => {
                    const style = getComputedStyle(element);
                    return style.fontSize === '77px' || style.fontFamily.includes('font-probe');
                  })
                  .map((element) => element.outerHTML.slice(0, 120));
              } finally {
                probe.remove();
              }
            }
            """);
        Assert.Empty(wrong);
    }
}
