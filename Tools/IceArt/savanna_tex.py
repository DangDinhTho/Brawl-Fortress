"""Tiling floor texture for the Savanna theme (Assets/Art/IceTheme/SavannaGround.png, repeats every 10 m), numpy + Pillow:

    python Tools/IceArt/savanna_tex.py

Sun-baked red ochre earth, patches of short dry golden grass (stubble strokes), a few dried mud patches with cracks,
small pebbles. Written only when its bytes change (Unity keeps imported textures mapped)."""
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

rnd = random.Random(17)
n1 = tile_noise(4, 1.2, 201); n2 = tile_noise(12, 0.9, 202); n3 = tile_noise(30, 0.7, 203)
earth = np.array([0.82, 0.58, 0.38]); earth_d = np.array([0.70, 0.46, 0.30])
col = earth_d + (earth - earth_d) * smooth(0.6 * n1 + 0.4 * n2, 0.25, 0.8)[..., None]
col = col * (1 + 0.05 * (n3[..., None] - 0.5)) * (1 - 0.08 * smooth(tile_noise(9, 1.0, 206), 0.6, 0.8)[..., None])   # darker blotches
# dried mud patches: paler, flat, with dark polygon cracks (distance to the nearest of a tiling set of points)
mud = smooth(tile_noise(7, 1.15, 204), 0.76, 0.82) * 0.9
pts = np.array([[rnd.random(), rnd.random()] for _ in range(420)])
d1 = np.full((N, N), 9.0); d2 = np.full((N, N), 9.0)
for px, py in pts:
    dx = np.abs(x - px); dx = np.minimum(dx, 1 - dx); dy = np.abs(y - py); dy = np.minimum(dy, 1 - dy)
    d = np.sqrt(dx * dx + dy * dy)
    d2 = np.where(d < d1, d1, np.minimum(d2, d)); d1 = np.minimum(d1, d)
crack = 1 - smooth(d2 - d1, 0.0, 0.004)
col = col * (1 - mud[..., None]) + np.array([0.86, 0.68, 0.48]) * mud[..., None]
col = col * (1 - 0.22 * (crack * mud)[..., None])
a = col.copy()
# dry grass: short golden strokes clustered in patches (never on the mud)
grass = smooth(tile_noise(6, 1.1, 205) * 0.65 + n2 * 0.35, 0.52, 0.64) * (1 - mud)
straws = [(0.90, 0.80, 0.50), (0.82, 0.70, 0.40), (0.95, 0.88, 0.62), (0.70, 0.56, 0.32)]
for k in range(45000):
    px, py = rnd.randrange(N), rnd.randrange(N)
    if rnd.random() > grass[py, px]: continue
    c = np.array(rnd.choice(straws)); ang = rnd.uniform(-1.2, 1.2) - math.pi / 2; L = rnd.randint(3, 7)
    for t in range(L):
        qx = int(round(px + math.cos(ang) * t)) % N; qy = int(round(py + math.sin(ang) * t)) % N
        a[qy, qx] = a[qy, qx] * 0.25 + c * 0.75
    a[(py + 1) % N, px] = a[(py + 1) % N, px] * 0.7 + np.array([0.55, 0.40, 0.24]) * 0.3   # a shadow at the root
for k in range(700):   # small pebbles
    px, py = rnd.randrange(N), rnd.randrange(N)
    c = rnd.choice([(0.92, 0.88, 0.80), (0.62, 0.58, 0.55), (0.78, 0.70, 0.62), (0.55, 0.36, 0.24)])
    a[py, px] = c
    if rnd.random() < 0.5: a[py, (px + 1) % N] = c; a[(py + 1) % N, (px + 1) % N] = np.array(c) * 0.75
save(np.clip(a, 0, 1), 'SavannaGround.png')
print('ok')
