"""Tiling works-yard floor for the Factory theme (Assets/Art/IceTheme/YardGround.png, repeats every 10 m): an old yard
of the 1960s: packed earth and cinders with gravel, worn cart ruts, and patches of granite setts (cobbles laid in rows)
whose edges have broken up. Warm and dusty so the red brick sits on it. numpy + Pillow:

    python Tools/IceArt/factory_tex.py
"""
import numpy as np, math, os, random
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

# ---------------------------------------------------------------- packed earth and cinders
earth_t = tile_noise(4, 1.2, 1) * 0.6 + tile_noise(24, 0.9, 2) * 0.4
lo = np.array([0.46, 0.38, 0.29]); hi = np.array([0.66, 0.56, 0.42])
img = lo + (hi - lo) * earth_t[..., None]
cinder = np.clip((tile_noise(5, 1.1, 3) - 0.6) * 3.0, 0, 1)                      # darker cinder patches
img = img * (1 - 0.30 * cinder[..., None])
# cart ruts: two worn bands wandering across the tile (integer frequencies so they tile)
for k, (y0, amp, ph) in enumerate([(0.30, 0.03, 0.0), (0.36, 0.03, 0.0), (0.74, 0.025, 1.7), (0.80, 0.025, 1.7)]):
    yc = y0 + amp * np.sin(2 * math.pi * x * 2 + ph)
    d = np.abs(((y - yc + 0.5) % 1.0) - 0.5)
    rut = np.clip(1 - d / 0.012, 0, 1) * (0.6 + 0.4 * tile_noise(8, 1.0, 10 + k))
    img = img * (1 - 0.22 * rut[..., None])

# ---------------------------------------------------------------- granite setts in patches
SW, SH = 28, 44          # setts per tile across / down: 0.36 x 0.23 m, laid in rows across
row = np.floor(y * SH).astype(int)
gx2 = x * SW + (row % 2) * 0.5                                                      # stretcher bond
cx, cy = gx2 % 1.0, (y * SH) % 1.0
col = np.floor(gx2).astype(int) % SW
rs = np.random.RandomState(4)
tone = rs.uniform(-1, 1, (SH, SW))[row % SH, col]
stone = np.array([0.52, 0.49, 0.45]) + 0.05 * tone[..., None]
stone = stone * (0.92 + 0.10 * tile_noise(30, 0.8, 5))[..., None]
ex, ey = np.minimum(cx, 1 - cx), np.minimum(cy, 1 - cy)
inner = np.clip(np.minimum(ex / 0.14, ey / 0.20), 0, 1)                             # rounded tops
stone = stone * (0.78 + 0.22 * inner ** 0.6)[..., None]
joint = np.clip(1 - np.minimum(ex / 0.07, ey / 0.10), 0, 1)                         # joints filled with earth
stone = stone * (1 - joint[..., None]) + img * joint[..., None] * 0.85
# whole setts only: the patch is sampled at each sett's centre, so the ragged edge follows the stones
patch = tile_noise(3, 1.3, 6)
ccx = ((np.floor(gx2) + 0.5 - (row % 2) * 0.5) / SW * N).astype(int) % N
ccy = ((row + 0.5) / SH * N).astype(int) % N
missing = rs.uniform(0, 1, (SH, SW))[row % SH, col]
mask = ((patch[ccy, ccx] + 0.22 * (missing - 0.5)) > 0.56).astype(float)
img = img * (1 - mask[..., None]) + stone * mask[..., None]

a = np.clip(img, 0, 1) * 255
rnd = random.Random(9)
for k in range(450):   # gravel and cinders on the earth
    px, py = rnd.randrange(N), rnd.randrange(N)
    if mask[py, px] > 0.5: continue
    r = rnd.randint(1, 2); c = rnd.choice([(140, 128, 110), (92, 80, 66), (150, 138, 118)])
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r: a[(py + dy) % N, (px + dx) % N] = c
Image.fromarray(a.astype(np.uint8)).save(os.path.join(OUT, 'YardGround.png'))
print('ok')
