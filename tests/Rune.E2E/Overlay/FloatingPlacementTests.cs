namespace Rune.E2E;

/// <summary>
/// 浮层定位的纯函数 computePlacement（js/overlay/floating.js）：十二种位置、放不下时翻到对侧、两侧都放不下时选空间大的、
/// 沿对齐方向挪回视口、从右到左镜像、箭头对准锚点、这一侧能用的高度。用例与 vue-tsx-pro 的 utils/dom/.test/floating.test.ts 一一对应，
/// 两边的定位算法保持一致。没有 JS 的单元测试工具，在浏览器里导入模块跑。
/// </summary>
public class FloatingPlacementTests(DemoServer demo) : E2ETest(demo)
{
    [Fact]
    public async Task 与vue的定位用例结果一致()
    {
        var page = await OpenAsync("static", "popover");
        var failures = await page.EvaluateAsync<string[]>("""
            async () => {
              const { computePlacement } = await import('/_content/RuneForBlazor.UI/js/overlay/floating.js');
              const viewport = { left: 0, top: 0, width: 1000, height: 800 };
              // 视口正中的锚点：100 × 40，四周都放得下
              const center = { left: 450, top: 380, width: 100, height: 40 };
              const place = (overrides = {}) => computePlacement({
                anchor: center, width: 200, height: 120, viewport, placement: 'bottom-start', gap: 6, rtl: false, padding: 8, ...overrides,
              });
              const failures = [];
              const expect = (name, actual, expected) => {
                if (JSON.stringify(actual) !== JSON.stringify(expected)) failures.push(`${name}: ${JSON.stringify(actual)} ≠ ${JSON.stringify(expected)}`);
              };

              // 十二种位置：主方向隔开 gap，start / center / end 对齐锚点的起始边、中间、结束边
              const at = (placement) => { const { x, y } = place({ placement }); return [x, y]; };
              expect('bottom-start', at('bottom-start'), [450, 426]);
              expect('bottom', at('bottom'), [400, 426]);
              expect('bottom-end', at('bottom-end'), [350, 426]);
              expect('top-start', at('top-start'), [450, 254]);
              expect('top', at('top'), [400, 254]);
              expect('top-end', at('top-end'), [350, 254]);
              expect('right-start', at('right-start'), [556, 380]);
              expect('right', at('right'), [556, 340]);
              expect('right-end', at('right-end'), [556, 300]);
              expect('left-start', at('left-start'), [244, 380]);
              expect('left', at('left'), [244, 340]);
              expect('left-end', at('left-end'), [244, 300]);

              // 从右到左：上下弹出时 start / end 镜像，左右弹出时不变
              expect('rtl bottom-start', place({ placement: 'bottom-start', rtl: true }).x, 350);
              expect('rtl bottom-end', place({ placement: 'bottom-end', rtl: true }).x, 450);
              expect('rtl bottom', place({ placement: 'bottom', rtl: true }).x, 400);
              expect('rtl right-start', place({ placement: 'right-start', rtl: true }).y, 380);

              // 放不下时翻到对侧，对齐方式不变
              const flipped = place({ anchor: { ...center, top: 740 }, placement: 'bottom-end' });
              expect('flip', [flipped.side, flipped.align, flipped.y], ['top', 'end', 740 - 6 - 120]);
              expect('flip right', place({ anchor: { ...center, left: 880 }, placement: 'right' }).side, 'left');
              expect('no flip', place({ placement: 'top' }).side, 'top');

              // 两侧都放不下：选空间大的一侧
              expect('roomier below', place({ anchor: { ...center, top: 200 }, height: 600, placement: 'top-start' }).side, 'bottom');
              expect('roomier above', place({ anchor: { ...center, top: 560 }, height: 600, placement: 'bottom-start' }).side, 'top');

              // crossAxisFallback：主方向与对侧都放不下时再试垂直的两侧（先下后上）
              const wide = { width: 900, height: 120, placement: 'right' };
              expect('wide', place(wide).side, 'right');
              expect('wide fallback', place({ ...wide, crossAxisFallback: true }).side, 'bottom');
              expect('wide fallback low', place({ ...wide, anchor: { ...center, top: 700 }, crossAxisFallback: true }).side, 'top');

              // 沿对齐的方向挪回视口；比视口还宽时贴着起始边
              expect('shift right', place({ anchor: { ...center, left: 900 } }).x, 1000 - 8 - 200);
              expect('shift left', place({ anchor: { ...center, left: 0 }, placement: 'bottom-end' }).x, 8);
              expect('too wide', place({ width: 1200 }).x, 8);
              expect('shift top', place({ anchor: { ...center, top: 0 }, placement: 'right' }).y, 8);

              // 视口不从 0 开始（可见区域 300–600）
              const visible = place({ viewport: { left: 0, top: 300, width: 1000, height: 300 }, height: 200, placement: 'top' });
              expect('visual viewport', [visible.side, visible.availableHeight], ['bottom', 600 - 8 - 426]);

              // 这一侧能用的高度
              expect('height bottom', place({ placement: 'bottom' }).availableHeight, 800 - 8 - 426);
              expect('height top', place({ placement: 'top' }).availableHeight, 380 - 6 - 8);
              expect('height right', place({ placement: 'right' }).availableHeight, 800 - 16);

              // 箭头对准锚点的中间，不画到浮层两端的圆角外
              const arrow = { size: 10, padding: 10 };
              expect('arrow center', place({ placement: 'bottom', arrow }).arrowX, 95);
              expect('arrow start', place({ placement: 'bottom-start', arrow }).arrowX, 45);
              expect('arrow corner', place({ anchor: { left: 985, top: 380, width: 10, height: 10 }, placement: 'bottom', arrow }).arrowX, 200 - 10 - 10);
              const side = place({ placement: 'right', arrow });
              expect('arrow side', [side.arrowY, side.arrowX === undefined], [55, true]);
              return failures;
            }
            """);

        Assert.Empty(failures);
    }
}
