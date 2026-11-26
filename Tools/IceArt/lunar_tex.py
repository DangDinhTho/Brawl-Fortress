"""Tiling moon floor for the Lunar theme (Assets/Art/IceTheme/RegolithGround.png, repeats every 10 m): pale grey
regolith with soft undulations, craters of many sizes (a lit rim on the sun side, a shaded bowl), scattered pebbles
and darker ejecta rays. numpy + Pillow:

    python Tools/IceArt/lunar_tex.py
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

base = 0.60 + 0.10 * tile_noise(4, 1.2, 1) + 0.05 * tile_noise(30, 0.8, 2)
shade = np.zeros((N, N))
rnd = random.Random(3)
for k in range(46):   # craters: bigger ones rare
    cx, cy = rnd.random(), rnd.random()
    R = 0.012 + 0.10 * rnd.random() ** 3
    dx = (x - cx + 0.5) % 1.0 - 0.5; dy = (y - cy + 0.5) % 1.0 - 0.5
    d = np.sqrt(dx * dx + dy * dy) / R
    bowl = np.clip(1 - d, 0, 1)
    rim = np.exp(-((d - 1.0) / 0.18) ** 2)
    light = (dx * -0.7 + dy * -0.7) / (R + 1e-6)          # the sun from the upper left: lit far wall, shaded near wall
    shade += -0.16 * bowl * np.clip(0.6 + 0.6 * light, 0, 1.2) + 0.10 * rim * np.clip(0.5 - 0.5 * light, 0, 1)
img = np.clip(base + shade, 0, 1)
col = np.stack([img * 0.98, img * 0.97, img * 0.95], -1)   # neutral, a breath warm
a = (col * 255)
for k in range(800):   # pebbles
    px, py = rnd.randrange(N), rnd.randrange(N); c = rnd.choice([0.45, 0.52, 0.80])
    a[py, px] = a[py, px] * 0 + c * 255
    a[(py + 1) % N, px] = a[(py + 1) % N, px] * 0.85
Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'RegolithGround.png'))
print('ok')
