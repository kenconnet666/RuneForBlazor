// 导出（同 vue-tsx-pro 的 export.ts）：菜单项带 data-rune-chart-export，图表外层是它所在的 figure。
// 图片：复制绘图区的 SVG、写上算出来的样式，画进两倍大小的 canvas；数据：读屏的数据表写成 CSV（带 BOM）。

document.addEventListener('click', (event) => {
  const item = event.target instanceof Element ? event.target.closest('[data-rune-chart-export]') : null;
  const figure = item?.closest('[role="figure"]');
  if (!figure) return;
  const name = item.dataset.name || 'chart';
  if (item.dataset.runeChartExport === 'image') {
    const svg = figure.querySelector('[data-rune-chart] svg');
    if (svg) exportImage(svg, name).catch((error) => console.error('图表导出图片失败', error));
  } else {
    const table = figure.querySelector(':scope > table');
    if (table) download(`${name}.csv`, new Blob(['\uFEFF', tableToCsv(table)], { type: 'text/csv;charset=utf-8' }));
  }
});

// 写回到复制品上的样式：画图要用到的这些（主题的 CSS 变量在算出来的样式里已经解析）
const INLINED = ['fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-opacity', 'stroke-dasharray', 'stroke-linecap', 'stroke-linejoin',
  'opacity', 'visibility', 'display', 'font-family', 'font-size', 'font-weight', 'text-anchor', 'dominant-baseline', 'paint-order',
  // 镂空（环形、地图的洞）与像素对齐的线：不带上的话导出的图把洞填实、细线发虚
  'fill-rule', 'shape-rendering'];

/** SVG → 带内联样式的 SVG 文字（宽高写成像素）；悬停的十字线、浅底不在图里 */
function serializeSvg(svg) {
  const clone = svg.cloneNode(true);
  const sources = [svg, ...svg.querySelectorAll('*')];
  const targets = [clone, ...clone.querySelectorAll('*')];
  sources.forEach((source, index) => {
    const target = targets[index];
    if (!target?.style) return;
    const style = getComputedStyle(source);
    for (const property of INLINED) target.style.setProperty(property, style.getPropertyValue(property));
    // 进场动画的终态：复制品不播动画
    target.style.setProperty('stroke-dashoffset', '0');
    target.style.setProperty('scale', 'none');
  });
  clone.querySelectorAll('[data-part="crosshair"], [data-part="hoverBand"]').forEach((element) => element.remove());
  const { width, height } = svg.getBoundingClientRect();
  clone.setAttribute('width', String(width));
  clone.setAttribute('height', String(height));
  clone.removeAttribute('viewBox');
  clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
  return { text: new XMLSerializer().serializeToString(clone), width, height };
}

/** 往外找第一个不透明的背景色：图片的底色 */
function backgroundOf(element) {
  for (let node = element; node instanceof Element; node = node.parentElement) {
    const color = getComputedStyle(node).backgroundColor;
    if (color && color !== 'transparent' && !/rgba\(.*,\s*0\)$/.test(color)) return color;
  }
  return '#ffffff';
}

/** 绘图区的 SVG 存成 PNG（两倍分辨率），底色取外层的背景色 */
async function exportImage(svg, name) {
  const { text, width, height } = serializeSvg(svg);
  const image = new Image();
  image.src = URL.createObjectURL(new Blob([text], { type: 'image/svg+xml' }));
  try {
    await image.decode();
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(width * 2);
    canvas.height = Math.round(height * 2);
    const context = canvas.getContext('2d');
    context.scale(2, 2);
    context.fillStyle = backgroundOf(svg);
    context.fillRect(0, 0, width, height);
    context.drawImage(image, 0, 0, width, height);
    const blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/png'));
    if (blob) download(`${name}.png`, blob);
  } finally {
    URL.revokeObjectURL(image.src);
  }
}

/** 数据表 → CSV：每格按 RFC 4180 加引号，开头写 = + - @ 的格子前加 '（Excel 不当公式执行） */
function tableToCsv(table) {
  const cell = (text) => {
    const value = /^[=+\-@]/.test(text) && Number.isNaN(Number(text)) ? `'${text}` : text;
    return /[",\r\n]/.test(value) ? `"${value.replaceAll('"', '""')}"` : value;
  };
  return [...table.rows].map((row) => [...row.cells].map((c) => cell(c.textContent.trim())).join(',')).join('\r\n');
}

function download(fileName, blob) {
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = fileName;
  link.click();
  setTimeout(() => URL.revokeObjectURL(link.href), 1000);
}
