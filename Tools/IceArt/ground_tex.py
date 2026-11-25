import numpy as np, random, math, os
_HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.environ.get('ICE_OUT') or os.path.join(_HERE, '..', '..', 'Assets', 'Art', 'IceTheme')
PREVIEW = os.environ.get('ICE_PREVIEW') or os.path.join(_HERE, 'preview')
os.makedirs(OUT, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
from PIL import Image, ImageDraw, ImageFilter
N = 1024
rnd = random.Random(7); np.random.seed(7)
y, x = np.mgrid[0:N, 0:N] / N

def tile_noise(kmax, amp_fall=1.0, seed=0):
    r = np.random.RandomState(seed); f = np.zeros((N, N))
    for kx in range(-kmax, kmax + 1):
        for ky in range(-kmax, kmax + 1):
            if kx == 0 and ky == 0: continue
            a = r.normal() / (math.hypot(kx, ky) ** amp_fall)
            f += a * np.cos(2 * math.pi * (kx * x + ky * y) + r.uniform(0, 2 * math.pi))
    f -= f.min(); f /= f.max(); return f

base = tile_noise(4, 1.2, 1)
detail = tile_noise(14, 1.0, 2)
ice_a = np.array([0.90, 0.95, 1.00]); ice_b = np.array([0.76, 0.87, 0.97])   # pale, snowy ice like the reference
t = np.clip(base * 0.8 + detail * 0.2, 0, 1)[..., None]
img = ice_a * (1 - t) + ice_b * t
# soft snow patches
snow = tile_noise(5, 1.1, 3) * 0.75 + tile_noise(16, 1.0, 4) * 0.25
m = np.clip((snow - 0.68) / 0.08, 0, 1)[..., None]
img = img * (1 - m) + np.array([0.95, 0.97, 1.0]) * m
img = (np.clip(img, 0, 1) * 255).astype(np.uint8)
im = Image.fromarray(img, 'RGB')

# cracks: branching random walks, drawn with wrap-around so the texture tiles
crack = Image.new('L', (N, N), 0); hi = Image.new('L', (N, N), 0)
dc, dh = ImageDraw.Draw(crack), ImageDraw.Draw(hi)
def seg(p, q, w):
    for ox in (-N, 0, N):
        for oy in (-N, 0, N):
            dc.line([(p[0] + ox, p[1] + oy), (q[0] + ox, q[1] + oy)], fill=255, width=w)
            dh.line([(p[0] + ox + 1, p[1] + oy + 1), (q[0] + ox + 1, q[1] + oy + 1)], fill=255, width=1)
def walk(p, ang, length, w, depth):
    # ice cracks: long straight runs with sudden kinks, few branches
    for i in range(length):
        if rnd.random() < 0.2: ang += rnd.choice([-1, 1]) * rnd.uniform(0.5, 1.0)   # sharp kink
        else: ang += rnd.uniform(-0.08, 0.08)
        q = (p[0] + math.cos(ang) * rnd.uniform(22, 44), p[1] + math.sin(ang) * rnd.uniform(22, 44))
        seg(p, q, w); p = q
        if depth < 1 and rnd.random() < 0.10: walk(p, ang + rnd.choice([-1, 1]) * rnd.uniform(0.9, 1.4), rnd.randint(2, 5), max(1, w - 1), depth + 1)
for k in range(6):
    walk((rnd.uniform(0, N), rnd.uniform(0, N)), rnd.uniform(0, 2 * math.pi), rnd.randint(8, 16), 2, 0)
crack = crack.filter(ImageFilter.GaussianBlur(0.6))
a = np.asarray(im).astype(float)
c = (np.asarray(crack) / 255.0)[..., None]; h = (np.asarray(hi) / 255.0)[..., None] * (1 - c)
a = a * (1 - c * 0.55) + np.array([120, 165, 215]) * c * 0.55      # light cracks
a = a * (1 - h * 0.6) + 255 * h * 0.6
# frost specks
for k in range(1400):
    px, py = rnd.randrange(N), rnd.randrange(N); a[py, px] = a[py, px] * 0.3 + 255 * 0.7
Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'IceGround.png'))
# 2 x 2 tiling check
t2 = Image.new('RGB', (1024, 1024)); s = Image.open(os.path.join(OUT, 'IceGround.png')).resize((512, 512))
for i in range(2):
    for j in range(2): t2.paste(s, (i * 512, j * 512))
t2.save(os.path.join(PREVIEW, 'ground_tiled.png'))
print('ok')
