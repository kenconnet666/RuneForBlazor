// 查询栏（data-rune-filter-bar="每个条件的最小宽度"，同 vue-tsx-pro 的 FilterBar）：默认只显示一排——按钮区旁边放得下几个条件就显示几个
// （常用条件在前、More 在后），其余的收进“展开”。条件都在同一个栅格里（Blazor 的内容片段拆不开成一个个节点）：这里量出第一排放得下几格，
// 把栅格的列数写成 --rune-filter-template，收起时把其余的藏起来（display: none 写在元素上），展开时全部显示、列宽与第一排每格相同。
// 全部放得下时不出“展开”；一个也放不下时条件独占一排、按钮区换到下面一行。算法同 vue 的 measure / layoutFields。

const watched = new WeakSet();

// CSS 长度（14em）在这一排里是多少 px
function lengthToPx(length, within) {
  const probe = document.createElement('div');
  probe.style.cssText = `position:absolute;visibility:hidden;inline-size:${length}`;
  within.append(probe);
  const px = probe.offsetWidth;
  probe.remove();
  return px;
}

/** 查询栏出现时调用。 */
export function watchFilterBar(form) {
  if (watched.has(form)) return;
  watched.add(form);
  const row = form.querySelector('[data-part="row"]');
  const fields = row?.querySelector(':scope > [data-part="fields"]');
  const actions = row?.querySelector(':scope > [data-part="actions"]');
  if (!fields || !actions) return;
  // “展开”按钮因为全部放得下而藏起来时量不到它：用上次量到的宽度，两种情况比的是同一个基准，不会来回切换
  let toggleWidth = 0;
  let frame = 0;

  const layout = () => {
    frame = 0;
    if (!form.isConnected) return;
    const toggle = actions.querySelector(':scope > [data-part="toggle"]');
    // More 外面一层 display: contents（data-part="more"）：里面的条件才是栅格的格子
    const more = fields.querySelector(':scope > [data-part="more"]');
    const items = [...fields.children].flatMap((child) => (child === more ? [...child.children] : [child]));
    const min = lengthToPx(form.getAttribute('data-rune-filter-bar') || '14em', row);
    if (!min || !row.clientWidth) return;
    const actionsGap = parseFloat(getComputedStyle(actions).columnGap) || 0;
    const shownToggle = toggle && toggle.style.display !== 'none';
    if (shownToggle) toggleWidth = toggle.offsetWidth + actionsGap;
    const m = {
      row: row.clientWidth,
      gap: parseFloat(getComputedStyle(row).columnGap) || 0,
      actions: actions.offsetWidth + (shownToggle ? 0 : toggleWidth),
      toggle: toggleWidth,
    };
    const fit = (width) => {
      const n = Math.floor((m.row - width) / (min + m.gap));
      return n > 0 ? n : null;
    };
    // 条件独占一排时排得下几格
    const alone = Math.max(1, Math.floor((m.row + m.gap) / (min + m.gap)));
    const bare = fit(m.actions - m.toggle);
    const needToggle = bare == null ? alone < items.length : bare < items.length;
    const first = needToggle ? fit(m.actions) : bare;
    const shown = Math.max(1, Math.min(items.length, first ?? alone));
    const expanded = form.getAttribute('data-expanded') === 'true';

    if (toggle) toggle.style.display = needToggle ? '' : 'none';
    // 量过了：More 里的条件交给下面逐个显示、藏起
    if (more) more.style.display = 'contents';
    fields.style.setProperty('--rune-filter-template', `repeat(${shown}, minmax(0, 1fr))`);
    // 一个也放不下：条件独占一排，按钮区换到下面
    fields.style.flexBasis = first == null ? '100%' : '';
    items.forEach((item, index) => {
      item.style.display = needToggle && !expanded && index >= shown ? 'none' : '';
    });
  };
  const schedule = () => {
    // 改布局放到下一帧：不在 ResizeObserver 的回调里同步改（会报 loop）
    if (!frame) frame = requestAnimationFrame(layout);
  };

  // 按钮区也看：字体加载完、按钮文字变了时它变宽，这一排的宽度却没变（只看这一排时第一次量的结果就留着，随字体加载的先后不同）
  const resize = new ResizeObserver(schedule);
  resize.observe(row);
  resize.observe(actions);
  new MutationObserver(schedule).observe(form, { attributes: true, attributeFilter: ['data-expanded', 'data-rune-filter-bar'] });
  new MutationObserver(schedule).observe(fields, { childList: true, subtree: true });
  layout();
}
