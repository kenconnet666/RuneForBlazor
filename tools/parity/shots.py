"""比较 artifacts/parity/<页>/<示例>-light-{vue,rune}.png：两张截图左上角对齐，取共同的区域（去掉底部的工具栏），
算有多少像素明显不同、高度差多少，按不同的比例从大到小列出，写到 artifacts/parity/shots.md。

部件配对（parity.mjs）的差异数会因两边结构不同而虚高；截图比较看的是画面，先修这里排在前面的。
用法：python tools/parity/shots.py [--top 60]
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2] / "artifacts" / "parity"
# 截的是效果区，没有工具栏
TOOLBAR = 0
# 一个像素的亮度相差超过这么多才算不同（抗锯齿、字体渲染的细小差别不算）
THRESHOLD = 40
# 允许整体平移的像素数
SHIFT = 2


def compare(vue_path: Path, rune_path: Path) -> tuple[float, int]:
    # 按亮度比：一边是灰度抗锯齿、一边是彩色的子像素抗锯齿（ClearType）时，字形边缘的颜色不同、亮度相近
    vue = np.asarray(Image.open(vue_path).convert("L"), dtype=np.int16)
    rune = np.asarray(Image.open(rune_path).convert("L"), dtype=np.int16)
    height = min(vue.shape[0], rune.shape[0]) - TOOLBAR
    width = min(vue.shape[1], rune.shape[1])
    if height <= 0 or width <= 0:
        return 1.0, rune.shape[0] - vue.shape[0]
    # 整块内容差一两个像素的平移（小数像素的取整）不算：试几个偏移，取最小的
    best = 1.0
    for dy in range(-SHIFT, SHIFT + 1):
        for dx in range(-SHIFT, SHIFT + 1):
            v = vue[max(dy, 0):height + min(dy, 0), max(dx, 0):width + min(dx, 0)]
            r = rune[max(-dy, 0):height + min(-dy, 0), max(-dx, 0):width + min(-dx, 0)]
            best = min(best, float((np.abs(v - r) > THRESHOLD).mean()))
    return best, rune.shape[0] - vue.shape[0]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--top", type=int, default=60)
    args = parser.parse_args()
    rows = []
    for vue_path in ROOT.glob("*/*-light-vue.png"):
        rune_path = vue_path.with_name(vue_path.name.replace("-vue.png", "-rune.png"))
        if not rune_path.exists():
            continue
        ratio, height = compare(vue_path, rune_path)
        example = vue_path.name.removesuffix("-light-vue.png")
        rows.append((ratio, abs(height), f"{vue_path.parent.name}/{example}", height))
    rows.sort(reverse=True)
    lines = ["| 示例 | 不同的像素 | 高度差（rune − vue） |", "| --- | --- | --- |"]
    lines += [f"| {name} | {ratio:.1%} | {height:+d}px |" for ratio, _, name, height in rows]
    (ROOT / "shots.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    for ratio, _, name, height in rows[: args.top]:
        print(f"{ratio:6.1%} {height:+5d}px  {name}")
    print(f"共 {len(rows)} 个示例，全表在 {ROOT / 'shots.md'}")


if __name__ == "__main__":
    main()
