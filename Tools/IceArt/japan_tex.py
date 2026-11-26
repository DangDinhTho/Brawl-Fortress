"""Tiling floor texture for the Japan theme (Assets/Art/IceTheme/ShrineGround.png, repeats every 10 m), numpy + Pillow:

    python Tools/IceArt/japan_tex.py

The gravel of a shrine yard in spring: pale warm-grey gravel speckled light and dark, a few patches of moss, fallen
cherry petals here and there. Written only when its bytes change (Unity keeps imported textures mapped)."""
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

def smooth(a, lo, hi):
    t = np.clip((a - lo) / (hi - lo), 0, 1); return t * t * (3 - 2 * t)

def save(arr, name):
    im = Image.fromarray(np.clip(arr * 255, 0, 255).astype(np.uint8))
    b = io.BytesIO(); im.save(b, 'PNG'); data = b.getvalue()
    p = os.path.join(OUT, name)
    if os.path.exists(p) and open(p, 'rb').read() == data: return
    open(p, 'wb').write(data)

rnd = random.Random(23)
n1 = tile_noise(4, 1.2, 301); n2 = tile_noise(12, 0.9, 302)
grav = np.array([0.87, 0.83, 0.75]); grav_d = np.array([0.75, 0.71, 0.64])
col = grav_d + (grav - grav_d) * smooth(0.6 * n1 + 0.4 * n2, 0.25, 0.8)[..., None]
speck = np.random.RandomState(303).rand(N, N)
col = col * (1 + 0.10 * (speck[..., None] - 0.5))                       # gravel grain
col = col - 0.10 * (speck > 0.985)[..., None] + 0.08 * (speck < 0.012)[..., None]
moss = smooth(tile_noise(10, 1.0, 304) * 0.7 + tile_noise(5, 1.2, 305) * 0.3, 0.70, 0.78) * 0.75                     # moss patches with soft edges
mcol = np.array([0.52, 0.62, 0.36]) * (0.85 + 0.3 * n2[..., None])
col = col * (1 - moss[..., None] * 0.85) + mcol * (moss[..., None] * 0.85)
a = col.copy()
for k in range(2600):   # fallen petals: little pink ovals
    px, py = rnd.randrange(N), rnd.randrange(N)
    if moss[py, px] > 0.6 and rnd.random() < 0.6: continue
    c = np.array(rnd.choice([(0.98, 0.78, 0.84), (0.96, 0.70, 0.78), (1.0, 0.86, 0.90)]))
    for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1), (2, 0)):
        if dx == 2 and rnd.random() < 0.5: continue
        a[(py + dy) % N, (px + dx) % N] = c
save(np.clip(a, 0, 1), 'ShrineGround.png')
print('ok')
