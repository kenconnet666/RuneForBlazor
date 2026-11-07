// 代码高亮（data-rune-highlight="语言"，同 vue-tsx-pro 的 createShikiHighlighter）：CodeBlock 先输出纯文本，插入页面后这里用 Shiki 上色，
// 换成同样文字的 span（等宽字体下排版不变，不跳动）。Shiki 与各语言的语法在第一次用到时才加载（wwwroot/vendor/shiki，按语言拆块）。
//
// - 正则引擎用 JavaScript 版（不加载 wasm），forgiving：语法不完全支持时尽量高亮而不是报错；不认识的语言保持纯文本。
// - 颜色用 Shiki 的 CSS 变量主题，默认值指向主题的代码颜色 token（--rune-code-colors-*）：切换主题、覆盖 token 都不用重新高亮。
// - 按行渲染时（行号、行高亮、diff），各行文字在 [data-rune-code-text] 里：高亮结果按 <br> 拆开分给各行。
// - CodeBlock 的代码、语言变了时整个 code 元素重建（@key），这里只处理新元素，不和 Blazor 的差异更新抢同一批节点。

const SHIKI = '../../vendor/shiki/';

/** 打包了的语言（与 tools/vendor-gen 的 SHIKI_LANGUAGES 一致）与常用的别名 */
const LANGUAGES = new Set(['razor', 'csharp', 'html', 'css', 'javascript', 'typescript', 'json', 'xml', 'shellscript', 'powershell', 'diff']);
const ALIASES = {
  cshtml: 'razor', blazor: 'razor', cs: 'csharp', 'c#': 'csharp', js: 'javascript', mjs: 'javascript', ts: 'typescript',
  sh: 'shellscript', bash: 'shellscript', shell: 'shellscript', zsh: 'shellscript', ps1: 'powershell', pwsh: 'powershell',
  htm: 'html', svg: 'xml', xaml: 'xml', csproj: 'xml', jsonc: 'json',
};

const THEME = 'rune';

/** Shiki 的类别 → 主题的代码颜色 token；使用方也可以用 --shiki-token-keyword 这类变量单独覆盖 */
const TOKEN_VARIABLES = {
  foreground: 'var(--rune-colors-text)',
  background: 'transparent',
  'token-comment': 'var(--rune-code-colors-comment)',
  'token-punctuation': 'var(--rune-code-colors-comment)',
  'token-keyword': 'var(--rune-code-colors-keyword)',
  'token-link': 'var(--rune-code-colors-keyword)',
  'token-string': 'var(--rune-code-colors-string)',
  'token-string-expression': 'var(--rune-code-colors-string)',
  'token-constant': 'var(--rune-code-colors-number)',
  'token-function': 'var(--rune-code-colors-function)',
  'token-parameter': 'var(--rune-colors-text)',
  'token-inserted': 'var(--rune-code-colors-string)',
  'token-deleted': 'var(--rune-tones-danger-text)',
  'token-changed': 'var(--rune-code-colors-number)',
};

// CSS 变量主题不区分类型、标签名、属性名：追加几条规则（同 vue-tsx-pro）
const EXTRA_TOKEN_COLORS = [
  {
    scope: ['entity.name.type', 'entity.name.class', 'support.type', 'support.class', 'entity.other.inherited-class'],
    settings: { foreground: 'var(--shiki-token-type, var(--rune-code-colors-type))' },
  },
  { scope: ['entity.name.tag', 'support.class.component'], settings: { foreground: 'var(--shiki-token-tag, var(--rune-code-colors-tag))' } },
  { scope: ['entity.other.attribute-name'], settings: { foreground: 'var(--shiki-token-attribute, var(--rune-code-colors-attribute))' } },
];

let core;
const loading = new Map();
const highlighted = new WeakSet();

function loadCore() {
  return (core ??= import(`${SHIKI}shiki.mjs`).then(({ createHighlighterCore, createCssVariablesTheme, createJavaScriptRegexEngine }) => {
    const base = createCssVariablesTheme({ name: THEME, variableDefaults: TOKEN_VARIABLES });
    return createHighlighterCore({
      themes: [{ ...base, tokenColors: [...(base.tokenColors ?? []), ...EXTRA_TOKEN_COLORS] }],
      langs: [],
      engine: createJavaScriptRegexEngine({ forgiving: true }),
    });
  }));
}

/** 加载语言；不认识的语言返回 null */
async function ensureLanguage(highlighter, lang) {
  const name = ALIASES[lang] ?? lang;
  if (!LANGUAGES.has(name)) return null;
  if (highlighter.getLoadedLanguages().includes(name)) return name;
  let pending = loading.get(name);
  if (!pending) {
    pending = import(`${SHIKI}langs/${name}.mjs`).then(
      async (module) => {
        await highlighter.loadLanguage(module.default);
        return name;
      },
      (error) => {
        // 下载失败（如断网）不缓存，下次再试
        loading.delete(name);
        throw error;
      },
    );
    loading.set(name, pending);
  }
  return pending;
}

/** 给一个 code 元素上色（插入时调用；同一个元素只做一次） */
export async function highlightCode(code) {
  if (highlighted.has(code)) return;
  const lang = code.getAttribute('data-rune-highlight')?.toLowerCase();
  if (!lang) return;
  highlighted.add(code);
  const lines = [...code.querySelectorAll('[data-rune-code-text]')];
  const source = lines.length > 0 ? lines.map((line) => line.textContent).join('\n') : code.textContent;
  try {
    const highlighter = await loadCore();
    const name = await ensureLanguage(highlighter, lang);
    // 加载期间被移出页面（换页、代码变了）：不用再做
    if (!name || !code.isConnected) return;
    // 只要行内的 span 与 br，外层的 pre、code 由 CodeBlock 渲染；代码里的 < 都已转义，按 <br> 拆是安全的
    const html = highlighter.codeToHtml(source, { lang: name, theme: THEME, structure: 'inline' });
    if (lines.length === 0) {
      code.innerHTML = html;
      return;
    }
    const parts = html.split('<br>');
    lines.forEach((line, index) => (line.innerHTML = parts[index] ?? ''));
  } catch (error) {
    console.error(`CodeBlock 高亮 ${lang} 代码失败，显示纯文本`, error);
  }
}
