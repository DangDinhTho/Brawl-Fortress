"""Tiling snow ground for the Xmas theme (Assets/Art/IceTheme/SnowGround.png): soft white snow with gentle drift
shading, a few pale blue hollows and sparkles. Plain python + numpy + Pillow (no Blender):

    python Tools/IceArt/snow_tex.py
"""
import numpy as np, random, math, os
from PIL import Image
_HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.environ.get('ICE_OUT') or os.path.join(_HERE, '..', '..', 'Assets', 'Art', 'IceTheme')
PREVIEW = os.environ.get('ICE_PREVIEW') or os.path.join(_HERE, 'preview')
os.makedirs(OUT, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
N = 1024
rnd = random.Random(11)
y, x = np.mgrid[0:N, 0:N] / N

def tile_noise(kmax, amp_fall=1.0, seed=0):
    """sum of integer-frequency waves: tiles seamlessly"""
    r = np.random.RandomState(seed); f = np.zeros((N, N))
    for kx in range(-kmax, kmax + 1):
        for ky in range(-kmax, kmax + 1):
            if kx == 0 and ky == 0: continue
            a = r.normal() / (math.hypot(kx, ky) ** amp_fall)
            f += a * np.cos(2 * math.pi * (kx * x + ky * y) + r.uniform(0, 2 * math.pi))
    f -= f.min(); f /= f.max(); return f

drift = tile_noise(4, 1.3, 21)            # big soft drifts
ripple = tile_noise(12, 1.0, 22)          # small wind ripples
t = np.clip(drift * 0.75 + ripple * 0.25, 0, 1)
hi = np.array([0.97, 0.98, 1.00]); lo = np.array([0.83, 0.88, 0.97])
img = hi * (1 - t[..., None] * 0.65) + lo * (t[..., None] * 0.65)
# pale blue hollows where the drift field is lowest
hollow = np.clip((t - 0.78) / 0.12, 0, 1)[..., None]
img = img * (1 - hollow * 0.35) + np.array([0.70, 0.79, 0.95]) * hollow * 0.35
a = np.clip(img, 0, 1) * 255
# sparkles: tiny bright specks, a few with a faint blue shadow beside them
for k in range(2600):
    px, py = rnd.randrange(N), rnd.randrange(N)
    a[py, px] = 255
    if rnd.random() < 0.25: a[(py + 1) % N, (px + 1) % N] = a[(py + 1) % N, (px + 1) % N] * 0.85
Image.fromarray(a.astype(np.uint8)).save(os.path.join(OUT, 'SnowGround.png'))
t2 = Image.new('RGB', (1024, 1024)); s = Image.open(os.path.join(OUT, 'SnowGround.png')).resize((512, 512))
for i in range(2):
    for j in range(2): t2.paste(s, (i * 512, j * 512))
t2.save(os.path.join(PREVIEW, 'snow_tiled.png'))
print('ok')
