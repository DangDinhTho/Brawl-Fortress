"""Tiling textures for the Candy theme (Assets/Art/IceTheme), numpy + Pillow:

    python Tools/IceArt/candy_tex.py

CandyGround.png  the floor (repeats every 10 m): soft mint icing with gentle swirls, coloured sprinkles, and patches of
                 square biscuit tiles (docked with little holes) laid as a path
ChocoFlow.png    the chocolate lake beyond the cake banks (BrawlFortress/Lava, two drifting layers): dark and milk
                 chocolate swirls with ribbons of cream
Each file is written only when its bytes change (Unity keeps imported textures mapped)."""
import numpy as np, math, os, random, io
from PIL import Image, ImageDraw
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

rnd = random.Random(11)
# ---------------------------------------------------------------- floor
n1 = tile_noise(4, 1.2, 81); n2 = tile_noise(14, 0.9, 82)
mint = np.array([0.66, 0.88, 0.72]); mint_d = np.array([0.55, 0.80, 0.64])
col = mint_d + (mint - mint_d) * smooth(0.6 * n1 + 0.4 * n2, 0.25, 0.8)[..., None]
sw = np.sin((n1 * 8 + n2 * 2) * math.pi)                                            # soft icing swirls
col = col * (1 + 0.05 * smooth(sw, 0.6, 0.95)[..., None])
# biscuit path patches: square crackers on a jittered grid where a low-frequency mask says so
mask = tile_noise(3, 1.3, 83)
img = Image.new('RGB', (N * 2, N * 2), (0, 0, 0)); dr = ImageDraw.Draw(img)
alpha = Image.new('L', (N * 2, N * 2), 0); da = ImageDraw.Draw(alpha)
S = 2 * N // 8                                                                       # ~1.25 m biscuits
for gy in range(8):
    for gx in range(8):
        x0 = gx * S + rnd.randint(-3, 3); y0 = gy * S + rnd.randint(-3, 3)
        cx, cy = (x0 + S / 2) / (2 * N), (y0 + S / 2) / (2 * N)
        if mask[int(cy * N) % N, int(cx * N) % N] < 0.55: continue
        w = S * 0.9; base = (int(232 * rnd.uniform(0.95, 1.02)), int(178 * rnd.uniform(0.95, 1.02)), 108)
        for ox in (-2 * N, 0, 2 * N):
            for oy in (-2 * N, 0, 2 * N):
                bx, by = x0 + ox + S * 0.05, y0 + oy + S * 0.05
                dr.rounded_rectangle([bx, by, bx + w, by + w], radius=int(S * 0.08), fill=base)
                da.rounded_rectangle([bx, by, bx + w, by + w], radius=int(S * 0.08), fill=255)
                dr.rounded_rectangle([bx + 6, by + 6, bx + w - 6, by + w - 6], radius=int(S * 0.06), outline=(204, 148, 84), width=3)
                for i in range(3):
                    for j in range(3):
                        hx, hy = bx + w * (0.25 + 0.25 * i), by + w * (0.25 + 0.25 * j)
                        dr.ellipse([hx - 5, hy - 5, hx + 5, hy + 5], fill=(186, 128, 70))
bis = np.asarray(img.resize((N, N), Image.BILINEAR)) / 255.0
al = np.asarray(alpha.resize((N, N), Image.BILINEAR)) / 255.0
col = col * (1 - al[..., None]) + bis * al[..., None]
a = col.copy()
cols = [(0.95, 0.30, 0.30), (1.0, 0.85, 0.30), (0.40, 0.70, 1.0), (1.0, 0.60, 0.80), (0.70, 0.50, 0.95), (1.0, 1.0, 1.0)]
for k in range(700):   # sprinkles: short coloured dashes
    px, py = rnd.randrange(N), rnd.randrange(N)
    if al[py, px] > 0.5: continue
    c = rnd.choice(cols); dx, dy = rnd.choice([(1, 0), (0, 1), (1, 1), (1, -1)])
    for t in range(4): a[(py + dy * t) % N, (px + dx * t) % N] = c
save(a, 'CandyGround.png')

# ---------------------------------------------------------------- chocolate
c1 = tile_noise(5, 1.0, 91); c2 = tile_noise(16, 0.8, 92)
dark = np.array([0.30, 0.16, 0.09]); milk = np.array([0.52, 0.32, 0.18]); cream = np.array([0.92, 0.82, 0.66])
t = 0.7 * c1 + 0.3 * c2
choc = dark + (milk - dark) * smooth(t, 0.3, 0.75)[..., None]
rib = smooth(np.sin((c1 * 9 + c2 * 2.5) * math.pi), 0.88, 0.99) * smooth(c2, 0.45, 0.7)
choc = choc * (1 - rib[..., None] * 0.85) + cream * rib[..., None] * 0.85
gl = smooth(np.sin((c1 * 5 - c2 * 3) * math.pi), 0.93, 1.0) * 0.25                   # glossy highlights
choc = choc + gl[..., None] * np.array([0.9, 0.75, 0.6])
save(np.clip(choc, 0, 1), 'ChocoFlow.png')
print('ok')
