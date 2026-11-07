namespace Rune.E2E;

/// <summary>
/// 原生控件继承字体（同 vue-tsx-pro 文档站 demos.browser.test.ts 的检查）：input、button、select、textarea 默认不继承，
/// Chromium 给 13.333px 与系统字体，按 em 定尺寸的方框、轨道就和文字、滑块按两套字号算。看得见的原生控件字号或字体与浏览器默认一样、
/// 外层却不一样时判失败。
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
    public async Task 原生控件继承外层的字体(string name)
    {
        var page = await OpenAsync("server", name);
        var wrong = await page.EvaluateAsync<string[]>("""
            () => {
              // 浏览器默认：一个没有样式的 input
              const probe = document.createElement('input');
              probe.style.all = 'revert';
              document.body.append(probe);
              const base = getComputedStyle(probe);
              const defaults = { size: base.fontSize, family: base.fontFamily };
              probe.remove();
              return [...document.querySelectorAll('input, button, select, textarea')]
                .filter((element) => element.type !== 'hidden' && element.getClientRects().length > 0 && element.parentElement)
                .filter((element) => {
                  const own = getComputedStyle(element);
                  const outer = getComputedStyle(element.parentElement);
                  return (own.fontSize === defaults.size && outer.fontSize !== defaults.size)
                    || (own.fontFamily === defaults.family && outer.fontFamily !== defaults.family);
                })
                .map((element) => element.outerHTML.slice(0, 120));
            }
            """);
        Assert.Empty(wrong);
    }
}
