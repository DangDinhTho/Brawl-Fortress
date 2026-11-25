"""Tiling desert sand for the Egypt theme (Assets/Art/IceTheme/SandGround.png): warm sand with wind ripples, darker
hollows, a few pebbles. Plain python + numpy + Pillow:

    python Tools/IceArt/sand_tex.py
"""
import numpy as np, random, math, os
from PIL import Image
_HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.environ.get('ICE_OUT') or os.path.join(_HERE, '..', '..', 'Assets', 'Art', 'IceTheme')
PREVIEW = os.environ.get('ICE_PREVIEW') or os.path.join(_HERE, 'preview')
os.makedirs(OUT, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
N = 1024
rnd = random.Random(5)
y, x = np.mgrid[0:N, 0:N] / N

def tile_noise(kmax, amp_fall=1.0, seed=0):
    r = np.random.RandomState(seed); f = np.zeros((N, N))
    for kx in range(-kmax, kmax + 1):
        for ky in range(-kmax, kmax + 1):
            if kx == 0 and ky == 0: continue
            a = r.normal() / (math.hypot(kx, ky) ** amp_fall)
            f += a * np.cos(2 * math.pi * (kx * x + ky * y) + r.uniform(0, 2 * math.pi))
    f -= f.min(); f /= f.max(); return f

dunes = tile_noise(3, 1.4, 31)
warp = tile_noise(5, 1.2, 32)
# wind ripples: stripes along one direction, bent by a low noise (integer frequency so it tiles)
ripple = 0.5 + 0.5 * np.sin(2 * math.pi * (14 * (x + y * 0.35) + warp * 2.0))
t = np.clip(dunes * 0.7 + ripple * 0.18 + tile_noise(16, 1.0, 33) * 0.12, 0, 1)[..., None]
hi = np.array([0.96, 0.86, 0.64]); lo = np.array([0.80, 0.64, 0.42])
img = hi * (1 - t * 0.7) + lo * (t * 0.7)
a = np.clip(img, 0, 1) * 255
for k in range(900):   # pebbles: small darker dots with a light top
    px, py = rnd.randrange(N), rnd.randrange(N); r = rnd.randint(1, 2)
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r: a[(py + dy) % N, (px + dx) % N] *= 0.78
    a[(py - r) % N, px % N] = np.minimum(255, a[(py - r) % N, px % N] * 1.25)
Image.fromarray(a.astype(np.uint8)).save(os.path.join(OUT, 'SandGround.png'))
print('ok')
