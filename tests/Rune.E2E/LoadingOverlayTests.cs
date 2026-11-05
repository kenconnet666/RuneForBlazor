namespace Rune.E2E;

/// <summary>加载遮罩：外层立即 aria-busy，遮罩推迟出现、淡入到不透明，结束后淡出并移出排版。</summary>
public class LoadingOverlayTests(DemoServer demo)
{
    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 保存时盖上遮罩_淡入_结束后隐藏(string mode)
    {
        var page = await demo.OpenAsync(mode, "progress");
        var root = page.GetByTestId("overlay");
        var overlay = root.Locator("[data-part=overlay]");
        await Assertions.Expect(overlay).ToBeHiddenAsync();

        await root.GetByRole(AriaRole.Button, new() { Name = "保存" }).ClickAsync();
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-busy", "true");
        await Assertions.Expect(overlay).ToBeVisibleAsync();
        await Assertions.Expect(overlay).ToHaveCSSAsync("opacity", "1");
        await Assertions.Expect(root.GetByRole(AriaRole.Status)).ToHaveTextAsync("正在保存");

        await Assertions.Expect(root).Not.ToHaveAttributeAsync("aria-busy", "true", new() { Timeout = 5000 });
        await Assertions.Expect(overlay).ToBeHiddenAsync();
    }
}
