"""Tiling floor texture for the Ocean theme (Assets/Art/IceTheme/SeabedGround.png, repeats every 10 m), numpy + Pillow:

    python Tools/IceArt/ocean_tex.py

Pale sea sand with wavy ripple marks, darker hollows, tiny shells and pebbles (flat sea-grass patches read as spilt paint). The moving light
on it (caustics) is the BrawlFortress/Caustics shader on a quad over the arena, not baked here.
Written only when its bytes change (Unity keeps imported textures mapped)."""
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

rnd = random.Random(13)
n1 = tile_noise(4, 1.2, 101); n2 = tile_noise(12, 0.9, 102); n3 = tile_noise(30, 0.7, 103)
sand = np.array([0.90, 0.82, 0.62]); sand_d = np.array([0.76, 0.68, 0.50])
col = sand_d + (sand - sand_d) * smooth(0.6 * n1 + 0.4 * n2, 0.25, 0.8)[..., None]
# ripple marks: wavy parallel ridges (a tiling direction 10 cycles over the tile, bent by noise)
ph = 2 * math.pi * (10 * y + 2 * x) + 3.0 * (n1 - 0.5) * 2 + 1.2 * (n2 - 0.5)
rip = np.sin(ph)
col = col * (1 + 0.07 * rip[..., None])
col = col + 0.06 * smooth(rip, 0.82, 1.0)[..., None] - 0.10 * smooth(-rip, 0.80, 1.0)[..., None] * np.array([0.8, 0.8, 0.6])   # crisp crests, shaded troughs
hollow = smooth(tile_noise(3, 1.3, 104), 0.72, 0.82)                                  # darker hollows
col = col * (1 - 0.18 * hollow[..., None])
a = col.copy()
for k in range(900):   # tiny shells and pebbles
    px, py = rnd.randrange(N), rnd.randrange(N)
    c = rnd.choice([(0.98, 0.95, 0.90), (0.95, 0.75, 0.62), (0.60, 0.58, 0.55), (0.82, 0.86, 0.92)])
    a[py, px] = c
    if rnd.random() < 0.4: a[py, (px + 1) % N] = c
save(np.clip(a, 0, 1), 'SeabedGround.png')
print('ok')
