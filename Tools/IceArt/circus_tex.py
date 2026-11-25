"""Tiling fairground floor for the Circus theme (Assets/Art/IceTheme/FairGround.png, repeats every 10 m): a trampled
summer field: lush grass worn through to earth along winding paths, sawdust and straw strewn over the paths, a few
dropped bits of coloured paper. numpy + Pillow:

    python Tools/IceArt/circus_tex.py
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

g = tile_noise(4, 1.2, 1) * 0.5 + tile_noise(32, 0.8, 2) * 0.5
grass = np.array([0.36, 0.55, 0.22]) + (np.array([0.52, 0.68, 0.30]) - np.array([0.36, 0.55, 0.22])) * g[..., None]
# worn paths: where a low noise is near its middle (winding bands), grass gives way to earth and sawdust
w = tile_noise(2, 1.3, 3)
path = np.clip(1 - np.abs(w - 0.5) / 0.08, 0, 1) ** 0.8
path = np.maximum(path, np.clip((tile_noise(3, 1.2, 4) - 0.72) * 4, 0, 1))           # and some worn patches
earth = np.array([0.62, 0.50, 0.34]) * (0.85 + 0.25 * tile_noise(24, 0.9, 5))[..., None]
saw = np.array([0.86, 0.74, 0.50])
dust = np.clip(tile_noise(14, 0.9, 6) * 1.6 - 0.5, 0, 1)                             # sawdust scattered on the paths
ground = earth * (1 - dust[..., None] * 0.6) + saw * (dust[..., None] * 0.6)
img = grass * (1 - path[..., None]) + ground * path[..., None]
a = np.clip(img, 0, 1) * 255
rnd = random.Random(7)
for k in range(900):   # straw: short light strokes, mostly on the paths
    px, py = rnd.randrange(N), rnd.randrange(N)
    if path[py, px] < 0.4 and rnd.random() < 0.85: continue
    ang = rnd.uniform(0, math.pi); L = rnd.randint(3, 7)
    for t in range(L):
        qx = int(px + math.cos(ang) * t) % N; qy = int(py + math.sin(ang) * t) % N
        a[qy, qx] = (232, 205, 130)
cols = [(214, 40, 40), (240, 200, 60), (50, 90, 190), (60, 170, 160), (245, 245, 235)]
for k in range(70):    # dropped bits of coloured paper (confetti), sparse
    px, py = rnd.randrange(N), rnd.randrange(N); c = rnd.choice(cols)
    for dy in range(2):
        for dx in range(3): a[(py + dy) % N, (px + dx) % N] = c
Image.fromarray(a.astype(np.uint8)).save(os.path.join(OUT, 'FairGround.png'))
print('ok')
