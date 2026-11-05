// 评分（data-rune-rating）的悬停预览：指针移到某个分数（label[data-score]）上时，按这个分数给各颗星写行内的 --rune-rating-fill，
// 盖过按勾上的 radio 用 CSS 算出的填充；移出评分或点了之后去掉（同 vue-tsx-pro 的 hover）。没有往返，静态 SSR 也一样。

const FILL = '--rune-rating-fill';

const starsOf = (root) => root.querySelectorAll(':scope > span');

function clear(root) {
  starsOf(root).forEach((star) => star.style.removeProperty(FILL));
}

/** 指针移到某个分数上：预览 */
export function previewRating(event) {
  if (!(event.target instanceof Element)) return;
  const label = event.target.closest('[data-rune-rating] label[data-score]');
  const root = label?.closest('[data-rune-rating]');
  if (!root || root.hasAttribute('data-disabled')) return;
  const score = Number(label.dataset.score);
  starsOf(root).forEach((star, i) => {
    const index = i + 1;
    star.style.setProperty(FILL, score >= index ? '100%' : score >= index - 0.5 ? '50%' : '0%');
  });
}

/** 指针移出评分：去掉预览 */
export function endRatingPreview(event) {
  if (!(event.target instanceof Element)) return;
  const root = event.target.closest('[data-rune-rating]');
  if (root && !(event.relatedTarget instanceof Node && root.contains(event.relatedTarget))) clear(root);
}

/** 点了一个分数：去掉预览，显示勾上的（再点一次清空时也能看出变化） */
export function pickRating(event) {
  if (!(event.target instanceof Element)) return;
  const root = event.target.closest('[data-rune-rating]');
  if (root) clear(root);
}
