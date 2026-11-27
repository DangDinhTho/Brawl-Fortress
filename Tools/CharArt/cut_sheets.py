"""Cut the FRONT / BACK character sheets into transparent PNGs, one per view.
usage: python cut_sheets.py <folder with the sheets> <output folder>"""
import sys, os, glob
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
# sheets in sorted file order
NAMES = ["Engineer", "Heavy", "Medic", "Demoman", "Sniper", "Scout", "Pyro", "Spy", "Soldier"]
SIZE = 1536


def runs(v, min_len, min_gap):
    r, start = [], None
    for i, on in enumerate(list(v) + [False]):
        if on and start is None: start = i
        if not on and start is not None:
            r.append([start, i]); start = None
    merged = []
    for s, e in r:
        if merged and s - merged[-1][1] < min_gap: merged[-1][1] = e
        else: merged.append([s, e])
    return [x for x in merged if x[1] - x[0] >= min_len]


def flood(m, seeds):
    for s in seeds:
        if m.getpixel(s) == 255:
            ImageDraw.floodfill(m, s, 128)


checks = []
files = sorted(glob.glob(os.path.join(src, "*.png")))
assert len(files) == len(NAMES), files
for f, name in zip(files, NAMES):
    im = Image.open(f).convert("RGBA")
    white = Image.new("RGBA", im.size, (255, 255, 255, 255)); white.alpha_composite(im)
    im = white.convert("RGB")
    a = np.asarray(im).astype(np.int16)
    H, W = a.shape[:2]
    mx, mn = a.max(2), a.min(2)
    sat = mx - mn
    cand = (sat < 16) & (mn > 180)                       # white paper, pale guide lines
    m = Image.fromarray((cand * 255).astype(np.uint8)).convert("L").copy(); m.load()
    flood(m, [(x, y) for x in range(0, W, 4) for y in (0, H - 1)] + [(x, y) for y in range(0, H, 4) for x in (0, W - 1)])
    fg = np.array(m) != 128
    # the characters' band = the tallest run of rows (the title and FRONT / BACK labels are separate runs)
    rr = runs(fg.sum(1) > 3, 40, 6)
    y0, y1 = max(rr, key=lambda r: r[1] - r[0])
    # second pass inside the band: ground shadow (darker grey) and pockets between the legs
    fy = y0 + int((y1 - y0) * 0.80)
    cand[fy:y1 + 40] |= (sat[fy:y1 + 40] < 14) & (mn[fy:y1 + 40] > 120)
    m = Image.fromarray((cand * 255).astype(np.uint8)).convert("L").copy(); m.load()
    flood(m, [(x, y) for x in range(0, W, 4) for y in (0, H - 1)] + [(x, y) for y in range(0, H, 4) for x in (0, W - 1)])
    py = y0 + int((y1 - y0) * 0.72)
    pure = (sat < 5) & (mn >= 249)
    ys_, xs_ = np.nonzero(pure[py:y1])
    flood(m, [(int(x), int(y + py)) for y, x in zip(ys_[::11], xs_[::11])])
    fg = np.array(m) != 128
    fg[:y0] = False
    # enclosed pockets of pure paper white anywhere (headset gap, between arm and body)
    m2 = Image.fromarray((((sat < 6) & (mn >= 244) & fg) * 255).astype(np.uint8)).convert("L").copy(); m2.load()
    ys_, xs_ = np.nonzero(((sat < 4) & (mn >= 252) & fg)[y0:y1])
    flood(m2, [(int(x), int(y + y0)) for y, x in zip(ys_[::5], xs_[::5])])
    pocket = np.array(m2) == 128
    print("   pocket px", int(pocket.sum()))
    fg &= ~pocket
    # guide lines: drop anything less than 6 px tall (vertical opening)
    er = fg.copy()
    for d in range(1, 6):
        er[d:] &= fg[:-d]
    op = er.copy()
    for d in range(1, 6):
        op[:-d] |= er[d:]
    fg = op
    alpha = Image.fromarray((fg * 255).astype(np.uint8)).convert("L")
    alpha = alpha.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.9))
    rgba = im.convert("RGBA"); rgba.putalpha(alpha)
    cols = runs(fg[y0:y1].sum(0) > 3, 150, 12)
    print(name, os.path.basename(f), "band", (y0, y1), "cols", cols)
    if len(cols) != 2:
        print("   !! expected 2 figures"); continue
    for (x0, x1), view in zip(cols, ["Front", "Back"]):
        rw = runs(fg[y0:y1, x0:x1].sum(1) > 3, 40, 10)
        ya, yb = y0 + rw[0][0], y0 + rw[-1][1]
        crop = rgba.crop((x0, ya, x1, yb))
        w, h = crop.size
        side = int(max(w, h) * 1.10)
        sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        sq.paste(crop, ((side - w) // 2, (side - h) // 2))
        if side > SIZE: sq = sq.resize((SIZE, SIZE), Image.LANCZOS)
        sq.save(os.path.join(out, "%s_%s.png" % (name, view)))
        chk = Image.new("RGBA", sq.size, (40, 60, 110, 255)); chk.alpha_composite(sq)
        checks.append((name, view, chk.convert("RGB").resize((420, 420), Image.LANCZOS), (w, h)))
        print("   ", view, (w, h))

for part, names in enumerate([NAMES[:5], NAMES[5:]]):
    sheet = Image.new("RGB", (420 * 2 * 3, 420 * 2), (40, 60, 110))
    i = 0
    for n, v, t, _ in checks:
        if n in names:
            sheet.paste(t, ((i % 6) * 420, (i // 6) * 420)); i += 1
    sheet.save(os.path.join(out, "_check%d.jpg" % (part + 1)), quality=88)
