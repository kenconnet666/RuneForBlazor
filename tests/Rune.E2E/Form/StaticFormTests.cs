namespace Rune.E2E;

/// <summary>静态 SSR 的表单回发：控件按字段生成的 name 让 [SupplyParameterFromForm] 把值绑回模型，校验在服务端。</summary>
public class StaticFormTests(DemoServer demo) : E2ETest(demo)
{
    [Fact]
    public async Task 提交后在服务端校验_改好再提交_值绑回模型()
    {
        var page = await OpenAsync("static", "form-post");
        var email = page.GetByRole(AriaRole.Textbox, new() { Name = "邮箱" });
        var submit = page.GetByRole(AriaRole.Button, new() { Name = "提交" });

        await submit.ClickAsync();
        await Assertions.Expect(page.GetByText("请填写邮箱").First).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "邮箱" })).ToHaveAttributeAsync("aria-invalid", "true");

        await page.GetByRole(AriaRole.Textbox, new() { Name = "邮箱" }).FillAsync("lin@example.com");
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "同意服务条款" }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "提交" }).ClickAsync();
        // 数字框与选择框提交的是隐藏框里的值（没改过：初始的 2 与上海）
        await Assertions.Expect(page.GetByTestId("result")).ToHaveTextAsync("lin@example.com · 2 · 上海 · True");
        await Assertions.Expect(email).ToHaveValueAsync("lin@example.com");
    }
}
