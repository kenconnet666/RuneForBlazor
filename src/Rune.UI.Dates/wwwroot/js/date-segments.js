// 分段输入（[data-rune-date-segments]，同 vue-tsx-pro 的 date-segments.ts 与 DateSegmentGroup）：年、月、日、时、分……每段是 role="spinbutton"。
// 段的顺序、分隔符、占位文字、段名由 C# 按文化算好渲染出来，这里只管输入：
// - 打字、↑↓、Page Up / Down、Home / End、Backspace、段之间的焦点都在这里，不往返；所有段都填了才把 ISO 文字交给 C#（rune-command change），
//   填好的值删掉一段时交 ''。年份没打满四位先不交（离开这一段、按 Enter 时交）。
// - 只改 Blazor 渲染的属性与文字节点本身（不换节点）：C# 的渲染没变的部分 Blazor 不会写回；写回了（晚到的渲染、值变了）时按草稿重画。
// - data-value 是 C# 认定的值：是自己刚交出去的，草稿留着（删掉一段后其余段还在）；不是时以它为准（父级改了、清空了）。
// - 静态 SSR 下没有 C#：画出来的就是最终状态，隐藏的 input 随表单提交。
import { isRtl, sendCommand } from '../../Rune.UI/js/core/shared.js';

const states = new WeakMap();

// Page Up / Down 的步长（同 React Aria）
const BIG_STEPS = { year: 5, month: 2, day: 7, quarter: 2, week: 4, hour: 2, minute: 15, second: 15, dayPeriod: 1 };
const ALL_FIELDS = ['year', 'month', 'day', 'hour', 'minute', 'second'];

/** 开始管理一组分段（插入时调用；重复调用无副作用） */
export function watchDateSegments(root) {
  if (states.has(root)) return;
  const state = new SegmentState(root);
  states.set(root, state);
  root.addEventListener('keydown', (event) => state.onKeydown(event));
  root.addEventListener('beforeinput', (event) => state.onBeforeinput(event));
  root.addEventListener('compositionend', (event) => state.onCompositionend(event));
  root.addEventListener('focusin', (event) => state.setFocused(event.target.closest?.('[role="spinbutton"]')?.dataset.type ?? null));
  root.addEventListener('focusout', (event) => !root.contains(event.relatedTarget) && state.setFocused(null));
  // 粘贴的文字不进段里（会打乱文字节点）
  root.addEventListener('paste', (event) => event.preventDefault());
  root.parentElement?.addEventListener('click', (event) => {
    if (event.target.closest?.('[data-rune-segments-clear]')) state.clear();
  });
  new MutationObserver(() => state.sync()).observe(root, { subtree: true, attributes: true, characterData: true, childList: true });
  state.sync();
}

class SegmentState {
  constructor(root) {
    this.root = root;
    this.fields = root.dataset.fields.split(' ');
    this.draft = isoToFields(root.dataset.value, this.fields) ?? {};
    this.seen = root.dataset.value ?? '';
    // 交给 C# 还没回来的值：回来的是其中之一时草稿留着
    this.pending = [];
    this.buffer = '';
    this.pendingYear = false;
    this.focused = null;
    // 每段 Blazor 渲染的文字节点：输入法组合换掉节点后放回去
    this.texts = new WeakMap();
  }

  get hour12() {
    return this.root.hasAttribute('data-hour12');
  }

  get editable() {
    return this.root.getAttribute('aria-disabled') !== 'true' && !this.root.hasAttribute('data-readonly');
  }

  get segments() {
    return [...this.root.querySelectorAll(':scope > [role="spinbutton"]')];
  }

  get complete() {
    return this.fields.every((field) => this.draft[field] != null);
  }

  get periods() {
    return (this.root.dataset.periods ?? 'AM|PM').split('|');
  }

  /** C# 渲染了（值、属性变了）：值不是自己刚交出去的就以它为准，再按草稿重画 */
  sync() {
    const value = this.root.dataset.value ?? '';
    if (value !== this.seen) {
      this.seen = value;
      const mine = this.pending.indexOf(value);
      if (mine >= 0) this.pending.splice(0, mine + 1);
      else {
        this.pending = [];
        this.draft = isoToFields(value, this.fields) ?? {};
        this.buffer = '';
        this.pendingYear = false;
      }
    }
    this.paint();
  }

  range(type) {
    const draft = this.draft;
    switch (type) {
      case 'year': return [1, 9999];
      case 'month': return [1, 12];
      case 'day': return [1, daysInMonth(draft.year, draft.month)];
      case 'quarter': return [1, 4];
      case 'week': return [1, draft.year == null ? 53 : isoWeeksInYear(draft.year)];
      case 'hour': return this.hour12 ? [1, 12] : [0, 23];
      case 'minute':
      case 'second': return [0, 59];
      default: return [0, 1];
    }
  }

  /** 段上显示的值：12 小时制的时是 1–12，上午 / 下午是 0 / 1 */
  shown(type) {
    const hour = this.draft.hour;
    if (type === 'dayPeriod') return hour == null ? null : hour >= 12 ? 1 : 0;
    if (type === 'hour' && this.hour12 && hour != null) return hour % 12 || 12;
    return this.draft[type] ?? null;
  }

  text(type, value) {
    if (type === 'dayPeriod') return value == null ? '' : this.periods[value];
    if (this.buffer && this.focused === type) return String(Number(this.buffer));
    if (value == null) return '';
    const width = type === 'year' ? 4 : type === 'quarter' || (type === 'hour' && this.hour12) ? 1 : 2;
    return String(value).padStart(width, '0');
  }

  /** 按草稿画各段：文字、占位、aria 的取值；隐藏的 input 与清空按钮 */
  paint() {
    for (const segment of this.segments) {
      const type = segment.dataset.type;
      const value = this.shown(type);
      const [min, max] = this.range(type);
      const filled = this.text(type, value);
      setText(this, segment, filled || segment.dataset.empty || '––');
      toggle(segment, 'data-placeholder', !filled);
      setAttribute(segment, 'aria-valuenow', value == null ? null : String(value));
      setAttribute(segment, 'aria-valuemin', String(min));
      setAttribute(segment, 'aria-valuemax', String(max));
      setAttribute(segment, 'aria-valuetext', filled || this.root.dataset.emptyText || '');
    }
    const input = this.root.querySelector(':scope > input[type="hidden"]');
    const value = this.complete ? fieldsToIso(constrained(this.draft), this.fields) : '';
    if (input && input.value !== value) input.value = value;
    const clear = this.root.parentElement?.querySelector(':scope > [data-rune-segments-clear]');
    if (clear) toggle(clear, 'hidden', !this.fields.some((field) => this.draft[field] != null));
  }

  /** 写一段的值（显示值：12 小时制的时是 1–12）；emit 为 false 时只改草稿（年份没打满） */
  write(type, value, emit = true) {
    const next = { ...this.draft };
    if (type === 'dayPeriod') {
      if (next.hour == null || value == null) return;
      next.hour = (next.hour % 12) + value * 12;
    } else if (type === 'hour' && this.hour12 && value != null) {
      const afternoon = (this.draft.hour ?? this.start().hour ?? 0) >= 12;
      next.hour = (value % 12) + (afternoon ? 12 : 0);
    } else if (value == null) delete next[type];
    else next[type] = value;
    const wasComplete = this.complete;
    this.draft = next;
    this.pendingYear = !emit;
    if (!emit) return;
    if (this.complete) this.emit(constrained(next));
    else if (wasComplete) this.emit(null);
  }

  /** 交给 C#：日超过了这个月的天数（先填 31 再把月改成 2 月）时草稿也跟着改成交出去的 */
  emit(values) {
    if (values) this.draft = { ...values };
    const iso = values ? fieldsToIso(values, this.fields) : '';
    this.pending.push(iso);
    if (this.pending.length > 20) this.pending.shift();
    sendCommand(this.root, 'change', iso);
  }

  commit() {
    if (!this.pendingYear) return;
    this.pendingYear = false;
    if (this.complete) this.emit(constrained(this.draft));
  }

  /** ↑↓ 从空段开始时的起点：C# 写的“现在”（data-start）；选周时从这一周所属的年起步（12 月底的那周可能已经算明年的第 1 周） */
  start() {
    const start = isoToFields(this.root.dataset.start, ALL_FIELDS, true) ?? {};
    if (start.month != null) start.quarter = Math.ceil(start.month / 3);
    if (start.day != null && this.fields.includes('week')) Object.assign(start, isoWeekOf(start.year, start.month, start.day));
    return start;
  }

  setFocused(type) {
    if (type !== this.focused) {
      this.commit();
      this.buffer = '';
    }
    this.focused = type;
    this.paint();
  }

  /** 焦点移到相邻的段 */
  moveFocus(direction) {
    const list = this.segments;
    const index = list.findIndex((segment) => segment.dataset.type === this.focused);
    const next = list[index + direction];
    if (!next) return;
    this.setFocused(next.dataset.type);
    if (this.root.contains(document.activeElement) && document.activeElement !== next) next.focus();
  }

  type(input) {
    if (!this.focused || !this.editable) return;
    for (const char of normalizeDigits(input)) {
      const segment = this.focused;
      if (segment === 'dayPeriod') {
        const lower = char.toLocaleLowerCase();
        const [am, pm] = this.periods.map((label) => label.toLocaleLowerCase());
        if (lower === 'a' || am.startsWith(lower)) this.write('dayPeriod', 0);
        else if (lower === 'p' || pm.startsWith(lower)) this.write('dayPeriod', 1);
        else continue;
        this.moveFocus(1);
        continue;
      }
      if (!/\d/.test(char)) continue;
      const [min, max] = this.range(segment);
      let next = this.buffer + char;
      // 打了这一位就超出范围：从这一位重新开始
      if (Number(next) > max) next = char;
      this.buffer = next;
      const number = Number(next);
      // 0 还不是合法值（月、日、12 小时制的时）：先记着，等下一位；年份打满四位才交
      if (number >= min) this.write(segment, number, segment !== 'year' || next.length >= 4);
      if (next.length >= String(max).length || number * 10 > max) {
        this.buffer = '';
        if (number < min) this.write(segment, min);
        this.moveFocus(1);
      }
    }
    this.paint();
  }

  increment(amount) {
    const type = this.focused;
    if (!type || !this.editable) return;
    this.buffer = '';
    const [min, max] = this.range(type);
    const value = this.shown(type);
    if (value == null) {
      const start = this.start();
      if (type === 'dayPeriod') {
        if (this.draft.hour == null) this.write('hour', this.hour12 ? (start.hour ?? 0) % 12 || 12 : (start.hour ?? 0));
        this.write('dayPeriod', (start.hour ?? 0) >= 12 ? 1 : 0);
      } else this.write(type, type === 'hour' && this.hour12 ? (start.hour ?? 0) % 12 || 12 : (start[type] ?? min));
    } else {
      const size = max - min + 1;
      this.write(type, ((((value - min + amount) % size) + size) % size) + min);
    }
    this.paint();
  }

  toEdge(edge) {
    const type = this.focused;
    if (!type || !this.editable) return;
    this.buffer = '';
    const [min, max] = this.range(type);
    this.write(type, edge === 'min' ? min : max);
    this.paint();
  }

  backspace() {
    const type = this.focused;
    if (!type || !this.editable) return;
    const value = this.shown(type);
    if (value == null && !this.buffer) {
      this.moveFocus(-1);
      return;
    }
    const left = type === 'dayPeriod' ? '' : (this.buffer || String(value)).slice(0, -1);
    this.buffer = left;
    const [min] = this.range(type);
    if (type === 'dayPeriod' || !left) {
      if (type === 'dayPeriod' || type === 'hour') {
        // 删掉上午 / 下午或时：时没有了，两段一起清空
        const wasComplete = this.complete;
        const next = { ...this.draft };
        delete next.hour;
        this.draft = next;
        if (wasComplete) this.emit(null);
      } else this.write(type, null);
    } else if (Number(left) >= min) this.write(type, Number(left));
    this.paint();
  }

  /** 清空按钮：清空所有段，焦点进第一段 */
  clear() {
    this.buffer = '';
    this.pendingYear = false;
    const wasComplete = this.complete;
    this.draft = {};
    if (wasComplete) this.emit(null);
    this.paint();
    this.segments[0]?.focus();
  }

  onKeydown(event) {
    // Enter 同原生的输入框：交出没打满的年份，再提交所在的表单
    if (event.key === 'Enter') {
      event.preventDefault();
      this.commit();
      this.root.closest('form')?.requestSubmit();
      return;
    }
    if (event.altKey || event.ctrlKey || event.metaKey || !this.focused) return;
    if (this.root.getAttribute('aria-disabled') === 'true') return;
    const forward = isRtl(this.root) ? -1 : 1;
    const moves = { ArrowRight: forward, ArrowLeft: -forward };
    if (event.key in moves) {
      event.preventDefault();
      this.moveFocus(moves[event.key]);
      return;
    }
    // 只读时只能换段
    if (!this.editable) return;
    const actions = {
      ArrowUp: () => this.increment(1),
      ArrowDown: () => this.increment(-1),
      PageUp: () => this.increment(BIG_STEPS[this.focused]),
      PageDown: () => this.increment(-BIG_STEPS[this.focused]),
      Home: () => this.toEdge('min'),
      End: () => this.toEdge('max'),
      Backspace: () => this.backspace(),
      Delete: () => this.backspace(),
    };
    const action = actions[event.key];
    if (!action) return;
    event.preventDefault();
    action();
  }

  onBeforeinput(event) {
    // 输入法组合中的文字取消不了，等 compositionend
    if (event.inputType === 'insertCompositionText') return;
    event.preventDefault();
    if (!this.editable) return;
    if (event.inputType.startsWith('delete')) this.backspace();
    else if (event.data) this.type(event.data);
  }

  onCompositionend(event) {
    const segment = event.target.closest?.('[role="spinbutton"]');
    // 组合把文字写进了段里：放回 Blazor 的文字节点
    const original = segment && this.texts.get(segment);
    if (original) segment.replaceChildren(original);
    if (this.editable && event.data) this.type(event.data);
    else this.paint();
  }
}

/** 改段里的文字：改 Blazor 渲染的那个文字节点本身，不换节点（换了 Blazor 之后的更新就落空了） */
function setText(state, segment, text) {
  let node = state.texts.get(segment);
  if (!node || node.parentNode !== segment) {
    node = [...segment.childNodes].find((child) => child.nodeType === Node.TEXT_NODE) ?? segment.appendChild(document.createTextNode(''));
    state.texts.set(segment, node);
  }
  if (node.nodeValue !== text) node.nodeValue = text;
}

function setAttribute(element, name, value) {
  if (value == null) {
    if (element.hasAttribute(name)) element.removeAttribute(name);
  } else if (element.getAttribute(name) !== value) element.setAttribute(name, value);
}

function toggle(element, name, on) {
  if (element.hasAttribute(name) !== on) element.toggleAttribute(name, on);
}

/** 全角数字、阿拉伯-印度数字 → 半角 */
function normalizeDigits(text) {
  return text
    .replace(/[０-９]/g, (char) => String.fromCharCode(char.charCodeAt(0) - 0xfee0))
    .replace(/[٠-٩]/g, (char) => String(char.charCodeAt(0) - 0x0660))
    .replace(/[۰-۹]/g, (char) => String(char.charCodeAt(0) - 0x06f0));
}

const daysInMonth = (year, month) => (month == null ? 31 : new Date(Date.UTC(year ?? 2000, month, 0)).getUTCDate());

/** 某天所在的 ISO 周与它所属的年 */
function isoWeekOf(year, month, day) {
  const date = new Date(Date.UTC(year, month - 1, day));
  date.setUTCDate(date.getUTCDate() + 4 - (date.getUTCDay() || 7));
  const first = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
  return { year: date.getUTCFullYear(), week: Math.ceil(((date - first) / 86400000 + 1) / 7) };
}

/** 一年有几个 ISO 周（52 或 53）：12 月 28 日总在最后一周里 */
const isoWeeksInYear = (year) => isoWeekOf(year, 12, 28).week;

// 日超过了这个月的天数：取这个月的最后一天；周同理（这一年只有 52 周时）
function constrained(values) {
  if (values.week != null && values.year != null) return { ...values, week: Math.min(values.week, isoWeeksInYear(values.year)) };
  if (values.day == null) return values;
  return { ...values, day: Math.min(values.day, daysInMonth(values.year, values.month)) };
}

const pad = (value, width = 2) => String(value).padStart(width, '0');

/** 各段的值 → ISO 文字（同 C# 的 DateSegments.ToIso） */
function fieldsToIso(values, fields) {
  if (fields.includes('quarter')) return `${pad(values.year ?? 0, 4)}-Q${values.quarter ?? 1}`;
  if (fields.includes('week')) return `${pad(values.year ?? 0, 4)}-W${pad(values.week ?? 1)}`;
  const date = [
    fields.includes('year') && pad(values.year ?? 0, 4),
    fields.includes('month') && pad(values.month ?? 1),
    fields.includes('day') && pad(values.day ?? 1),
  ].filter((part) => part !== false).join('-');
  if (!fields.includes('hour')) return date;
  const time = [pad(values.hour ?? 0), pad(values.minute ?? 0), fields.includes('second') && pad(values.second ?? 0)].filter((part) => part !== false).join(':');
  return fields.includes('year') ? `${date}T${time}` : time;
}

/** ISO 文字 → 各段的值；格式不对、缺了要的段时 null（partial 时有几段算几段） */
function isoToFields(value, fields, partial = false) {
  if (!value) return null;
  const parsed = {};
  const period = /^(\d{4})-(?:Q([1-4])|W(\d{2}))$/.exec(value);
  if (period) {
    parsed.year = Number(period[1]);
    if (period[2] != null) parsed.quarter = Number(period[2]);
    if (period[3] != null) parsed.week = Number(period[3]);
  } else {
    const match = /^(?:(\d{4})(?:-(\d{2})(?:-(\d{2}))?)?)?(?:T?(\d{2}):(\d{2})(?::(\d{2}))?)?$/.exec(value);
    if (!match) return null;
    ALL_FIELDS.forEach((field, index) => {
      if (fields.includes(field) && match[index + 1] != null) parsed[field] = Number(match[index + 1]);
    });
  }
  if (partial) return parsed;
  return fields.every((field) => parsed[field] != null) ? parsed : null;
}
