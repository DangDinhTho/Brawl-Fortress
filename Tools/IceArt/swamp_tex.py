"""Tiling textures for the Swamp theme (Assets/Art/IceTheme), numpy + Pillow:

    python Tools/IceArt/swamp_tex.py

SwampGround.png  the floor (repeats every 10 m): dark peat with patches of moss, wet dark hollows, roots, leaf litter
AcidFlow.png     the acid lake beyond the banks (BrawlFortress/Lava, two drifting layers): bright toxic green with dark
                 scum plates and bubble rings
BogMud.png       the slowing bog puddles (BrawlFortress/Bog): olive-brown mud with lighter scum swirls
Each file is written only when its bytes change (Unity keeps imported textures mapped)."""
import numpy as np, math, os, random, io
from PIL import Image
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

def save(arr, name):
    im = Image.fromarray(np.clip(arr * 255, 0, 255).astype(np.uint8))
    b = io.BytesIO(); im.save(b, 'PNG'); data = b.getvalue()
    p = os.path.join(OUT, name)
    if os.path.exists(p) and open(p, 'rb').read() == data: return
    open(p, 'wb').write(data)

def smooth(a, lo, hi):
    t = np.clip((a - lo) / (hi - lo), 0, 1); return t * t * (3 - 2 * t)

def wrapd(cx, cy):
    dx = (x - cx + 0.5) % 1.0 - 0.5; dy = (y - cy + 0.5) % 1.0 - 0.5
    return dx, dy

# ---------------------------------------------------------------- floor
rnd = random.Random(5)
n1 = tile_noise(4, 1.2, 11); n2 = tile_noise(14, 0.9, 12); n3 = tile_noise(40, 0.7, 13)
peat = np.array([0.36, 0.27, 0.19]); peat_dark = np.array([0.22, 0.16, 0.12])
moss = np.array([0.33, 0.47, 0.20]); moss_dark = np.array([0.22, 0.34, 0.15])
wet = np.array([0.17, 0.15, 0.13])
v = 0.55 * n1 + 0.30 * n2 + 0.15 * n3
col = peat_dark[None, None] + (peat - peat_dark)[None, None] * smooth(v, 0.25, 0.75)[..., None]
m = smooth(tile_noise(5, 1.1, 21) * 0.75 + n3 * 0.25, 0.56, 0.66)            # moss patches
mt = smooth(n2, 0.3, 0.8)[..., None]
col = col * (1 - m[..., None]) + (moss_dark + (moss - moss_dark) * mt) * m[..., None]
w = smooth(tile_noise(3, 1.3, 31) * 0.8 + n2 * 0.2, 0.70, 0.78)              # wet dark hollows
col = col * (1 - 0.8 * w[..., None]) + wet * 0.8 * w[..., None]
for k in range(16):   # roots: dark winding lines
    cx, cy = rnd.random(), rnd.random(); a = rnd.uniform(0, 6.28); L = rnd.uniform(0.08, 0.2); th = rnd.uniform(0.004, 0.008)
    for s in range(150):
        t = s / 149
        px = cx + math.cos(a + 1.5 * math.sin(t * 3 + k)) * L * t; py = cy + math.sin(a + 1.5 * math.sin(t * 3 + k)) * L * t
        dx, dy = wrapd(px, py); d = np.sqrt(dx * dx + dy * dy)
        r = smooth(-d, -th * (1 - 0.6 * t), -th * 0.3 * (1 - 0.6 * t))
        col = col * (1 - 0.55 * r[..., None]) + np.array([0.20, 0.13, 0.09]) * 0.55 * r[..., None]
a = col.copy()
for k in range(1600):   # leaf litter and pebbles
    px, py = rnd.randrange(N), rnd.randrange(N)
    c = rnd.choice([(0.55, 0.40, 0.20), (0.45, 0.30, 0.16), (0.40, 0.40, 0.36), (0.50, 0.55, 0.25)])
    for (ox, oy) in [(0, 0), (1, 0), (0, 1)] if rnd.random() < 0.5 else [(0, 0)]:
        a[(py + oy) % N, (px + ox) % N] = c
save(a, 'SwampGround.png')

# ---------------------------------------------------------------- acid lake
g1 = tile_noise(6, 1.0, 41); g2 = tile_noise(18, 0.8, 42)
bright = np.array([0.50, 0.90, 0.30]); mid = np.array([0.16, 0.56, 0.18]); dark = np.array([0.05, 0.24, 0.10])
t = 0.7 * g1 + 0.3 * g2
acid = mid + (bright - mid) * smooth(t, 0.45, 0.85)[..., None]
scum = smooth(tile_noise(5, 1.2, 43) * 0.8 + g2 * 0.2, 0.57, 0.63)            # dark scum plates
acid = acid * (1 - scum[..., None]) + (dark + 0.06 * g2[..., None]) * scum[..., None]
for k in range(70):   # bubble rings
    cx, cy = rnd.random(), rnd.random(); R = rnd.uniform(0.006, 0.02)
    dx, dy = wrapd(cx, cy); d = np.sqrt(dx * dx + dy * dy)
    ring = np.exp(-((d - R) / (R * 0.28)) ** 2); inner = smooth(-d, -R * 0.9, -R * 0.5)
    acid = acid * (1 - 0.5 * inner[..., None]) + np.array([0.75, 1.0, 0.55]) * (0.5 * inner + 0.9 * ring)[..., None] * (1 - scum[..., None] * 0.8)
save(np.clip(acid, 0, 1), 'AcidFlow.png')

# ---------------------------------------------------------------- bog mud
b1 = tile_noise(5, 1.0, 51); b2 = tile_noise(16, 0.8, 52)
mud = np.array([0.27, 0.31, 0.14]); mud_dark = np.array([0.13, 0.16, 0.08]); scumc = np.array([0.52, 0.60, 0.22])
u = 0.65 * b1 + 0.35 * b2
bog = mud_dark + (mud - mud_dark) * smooth(u, 0.3, 0.8)[..., None]
sw = np.sin((b1 * 9 + b2 * 3) * math.pi)                                       # swirling scum streaks
s = smooth(sw, 0.55, 0.9) * smooth(b2, 0.35, 0.65)
bog = bog * (1 - 0.8 * s[..., None]) + scumc * 0.8 * s[..., None]
save(np.clip(bog, 0, 1), 'BogMud.png')
print('ok')
