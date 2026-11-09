namespace Rune.E2E;

/// <summary>
/// 焦点环没有被裁（同 vue-tsx-pro 的 focus-ring.browser.test）：每个文档页的示例里每个可聚焦的元素，用键盘的方式聚焦后
/// 按 outline 的宽度与偏移算出焦点环的范围，检查它没有被 overflow 不是 visible 的祖先裁掉。被裁的改用 SharedStyles.FocusRingInset
/// （画在里面）、只在需要的方向裁（OverflowX = Clip）或给容器留出余量。元素本身就在容器外（滚走了、被有意截断）的不算。
/// </summary>
public class FocusRingTests(DemoServer demo) : E2ETest(demo)
{
    /// <summary>全部文档页。</summary>
    public static TheoryData<string> Pages => VisualTests.Pages;

    [Theory, MemberData(nameof(Pages))]
    public async Task 键盘聚焦时焦点环没有被祖先的overflow裁掉(string page)
    {
        var tab = await OpenAsync("static", page);
        // 先按一下 Tab：之后的 focus() 算键盘聚焦（:focus-visible）
        await tab.Keyboard.PressAsync("Tab");
        var clipped = await tab.EvaluateAsync<string[]>(Script);
        Assert.True(clipped.Length == 0, string.Join("\n", clipped));
    }

    const string Script = """
        () => {
          const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select, textarea, [tabindex]:not([tabindex="-1"])';
          const describe = (element) => [
            element.tagName.toLowerCase(),
            element.getAttribute('aria-label') ?? element.textContent?.replace(/\s+/g, ' ').trim().slice(0, 16),
            element.getAttribute('data-scope') && `${element.getAttribute('data-scope')}/${element.getAttribute('data-part')}`,
          ].filter(Boolean).join(' ');
          const clipperOf = (element, stop) => {
            const style = getComputedStyle(element);
            if (style.outlineStyle === 'none' || style.outlineStyle === '') return null;
            const reach = parseFloat(style.outlineWidth) + parseFloat(style.outlineOffset);
            if (!(reach > 0)) return null;
            const rect = element.getBoundingClientRect();
            if (rect.width === 0 || rect.height === 0) return null;
            for (let node = element.parentElement; node && node !== stop; node = node.parentElement) {
              const own = getComputedStyle(node);
              if (own.overflowX === 'visible' && own.overflowY === 'visible') continue;
              const box = node.getBoundingClientRect();
              const left = box.left + node.clientLeft;
              const top = box.top + node.clientTop;
              const right = left + node.clientWidth;
              const bottom = top + node.clientHeight;
              if (rect.left < left - 0.5 || rect.top < top - 0.5 || rect.right > right + 0.5 || rect.bottom > bottom + 0.5) return null;
              const tolerance = reach - 0.5;
              if ((own.overflowX !== 'visible' && (rect.left - left < tolerance || right - rect.right < tolerance))
                || (own.overflowY !== 'visible' && (rect.top - top < tolerance || bottom - rect.bottom < tolerance)))
                return node;
            }
            return null;
          };
          const clipped = [];
          for (const canvas of document.querySelectorAll('[data-demo-canvas]')) {
            for (const element of canvas.querySelectorAll(FOCUSABLE)) {
              element.focus({ preventScroll: true });
              if (document.activeElement !== element) continue;
              const clipper = clipperOf(element, canvas);
              if (!clipper) continue;
              const section = canvas.closest('section')?.querySelector('h2')?.id ?? 'example';
              clipped.push(`${section}：${describe(element)}（被 ${describe(clipper)} 裁掉）`);
            }
          }
          return clipped;
        }
        """;
}
