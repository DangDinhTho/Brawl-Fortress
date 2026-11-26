"""Tiling textures for the Greek theme (Assets/Art/IceTheme), numpy + Pillow:

    python Tools/IceArt/greek_tex.py

GreekGround.png  the floor (repeats every 10 m): sun-baked pale earth, patches of worn and broken ancient marble
                 flagstones (irregular, cracked, chipped: not modern paving), dry grass, pebbles
SeaFlow.png      the sea beyond the cliffs (BrawlFortress/Lava, two drifting layers): Mediterranean blue with lighter wave
                 streaks and white glints
Each file is written only when its bytes change (Unity keeps imported textures mapped)."""
import numpy as np, math, os, random, io
from PIL import Image, ImageDraw
_HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.environ.get('ICE_OUT') or os.path.join(_HERE, '..', '..', 'Assets', 'Art', 'IceTheme')
N = 512
y, x = np.mgrid[0:N, 0:N] / N

def tile_noise(kmax, amp_fall=1.0, seed=0):
    r = np.random.RandomState(seed); f = np.zeros((N, N))
    for kx in range(-kmax, kmax + 1):
        for ky in range(-kmax, kmax + 1):
            if kx == 0 and ky == 0: continue
            a = r.normal() / (math.hypot(kx, ky) ** amp_fall)
            f += a * np.cos(2 * math.pi * (kx * x + ky * y) + r.uniform(0, 2 * math.pi))
    f -= f.min(); f /= f.max(); return f

def smooth(a, lo, hi):
    t = np.clip((a - lo) / (hi - lo), 0, 1); return t * t * (3 - 2 * t)

def save(arr, name):
    im = Image.fromarray(np.clip(arr * 255, 0, 255).astype(np.uint8))
    b = io.BytesIO(); im.save(b, 'PNG'); data = b.getvalue()
    p = os.path.join(OUT, name)
    if os.path.exists(p) and open(p, 'rb').read() == data: return
    open(p, 'wb').write(data)

rnd = random.Random(7)
# ---------------------------------------------------------------- floor
n1 = tile_noise(4, 1.2, 61); n2 = tile_noise(16, 0.9, 62); n3 = tile_noise(40, 0.7, 63)
earth = np.array([0.80, 0.68, 0.50]); earth_d = np.array([0.66, 0.54, 0.38])
col = earth_d + (earth - earth_d) * smooth(0.6 * n1 + 0.4 * n2, 0.25, 0.8)[..., None]
grass = smooth(tile_noise(6, 1.0, 64) * 0.7 + n3 * 0.3, 0.62, 0.70)               # dry grass patches
gcol = np.array([0.62, 0.60, 0.34]) + 0.08 * n3[..., None]
col = col * (1 - grass[..., None] * 0.8) + gcol * grass[..., None] * 0.8
# flagstone patches: a jittered grid of slabs drawn where a low-frequency mask says paving survives
mask = tile_noise(3, 1.3, 65)
slab = Image.new('L', (N * 2, N * 2), 0); sd = ImageDraw.Draw(slab)           # drawn at 2x, wrapped
shade = Image.new('L', (N * 2, N * 2), 128); hd = ImageDraw.Draw(shade)
S = 2 * N // 7                                                                    # ~1.4 m slabs
for gy in range(7):
    off = (S // 2) if gy % 2 else 0
    for gx in range(8):
        x0 = gx * S - off + rnd.randint(-4, 4); y0 = gy * S + rnd.randint(-4, 4)
        w = S * rnd.uniform(0.93, 0.98); h = S * rnd.uniform(0.93, 0.98)
        cx, cy = (x0 + w / 2) / (2 * N) % 1.0, (y0 + h / 2) / (2 * N) % 1.0
        if mask[int(cy * N) % N, int(cx * N) % N] < 0.52 or rnd.random() < 0.15: continue   # gone
        pts = [(x0 + rnd.randint(0, 8), y0 + rnd.randint(0, 8)), (x0 + w * rnd.uniform(0.4, 0.6), y0 + rnd.randint(-4, 6)),
               (x0 + w - rnd.randint(0, 8), y0 + rnd.randint(0, 8)), (x0 + w + rnd.randint(-6, 4), y0 + h * rnd.uniform(0.4, 0.6)),
               (x0 + w - rnd.randint(0, 8), y0 + h - rnd.randint(0, 8)), (x0 + w * rnd.uniform(0.4, 0.6), y0 + h + rnd.randint(-6, 4)),
               (x0 + rnd.randint(0, 8), y0 + h - rnd.randint(0, 8)), (x0 + rnd.randint(-4, 6), y0 + h * rnd.uniform(0.4, 0.6))]
        if rnd.random() < 0.35:   # a chipped corner
            i = rnd.randrange(0, 8, 2); cxp, cyp = x0 + w / 2, y0 + h / 2
            pts[i] = (pts[i][0] + (cxp - pts[i][0]) * 0.35, pts[i][1] + (cyp - pts[i][1]) * 0.35)
        for ox in (-2 * N, 0, 2 * N):
            for oy in (-2 * N, 0, 2 * N):
                pp = [(px + ox, py + oy) for (px, py) in pts]
                sd.polygon(pp, fill=255); hd.polygon(pp, fill=rnd.randint(110, 150))
                if rnd.random() < 0.4:   # a crack
                    a = pp[rnd.randrange(8)]; b = pp[rnd.randrange(8)]
                    hd.line([a, ((a[0] + b[0]) / 2 + rnd.randint(-12, 12), (a[1] + b[1]) / 2 + rnd.randint(-12, 12)), b], fill=70, width=2)
slab = np.asarray(slab.resize((N, N), Image.BILINEAR)) / 255.0
sh = np.asarray(shade.resize((N, N), Image.BILINEAR)) / 255.0
marble = np.array([0.88, 0.82, 0.68]) * (0.86 + 0.35 * (sh - 0.5) + 0.08 * n3)[..., None]
col = col * (1 - slab[..., None]) + marble * slab[..., None]
edge = np.clip(slab * (1 - slab) * 4, 0, 1)                                         # dark joint at the slab edges
col = col * (1 - 0.35 * edge[..., None])
a = col.copy()
for k in range(1400):   # pebbles and chips
    px, py = rnd.randrange(N), rnd.randrange(N)
    c = rnd.choice([(0.92, 0.88, 0.80), (0.58, 0.50, 0.40), (0.70, 0.62, 0.48)])
    a[py, px] = c
save(a, 'GreekGround.png')

# ---------------------------------------------------------------- sea
s1 = tile_noise(6, 1.0, 71); s2 = tile_noise(20, 0.8, 72)
deep = np.array([0.06, 0.30, 0.58]); mid = np.array([0.12, 0.48, 0.74]); light = np.array([0.40, 0.72, 0.88])
t = 0.7 * s1 + 0.3 * s2
sea = deep + (mid - deep) * smooth(t, 0.2, 0.7)[..., None]
waves = smooth(np.sin((s1 * 7 + s2 * 2.5) * math.pi), 0.75, 0.97)
sea = sea * (1 - waves[..., None] * 0.6) + light * waves[..., None] * 0.6
for k in range(90):   # glints
    cx, cy = rnd.random(), rnd.random(); R = rnd.uniform(0.0015, 0.004)
    dx = (x - cx + 0.5) % 1.0 - 0.5; dy = (y - cy + 0.5) % 1.0 - 0.5
    g = np.exp(-((dx / (R * 2.5)) ** 2 + (dy / R) ** 2))
    sea = sea + g[..., None] * np.array([0.9, 0.95, 1.0]) * 0.8
save(np.clip(sea, 0, 1), 'SeaFlow.png')
print('ok')
