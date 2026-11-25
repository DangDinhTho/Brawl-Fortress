"""Tiling textures for the Volcano theme (numpy + Pillow):

    python Tools/IceArt/volcano_tex.py

BasaltGround.png  the island floor (repeats every 10 m): the tops of basalt columns seen from above (irregular polygon
                  cells with dark joints), patches of pale ash, and a few thin glowing cracks along some joints.
LavaFlow.png      molten lava for the lava lake and the eruption fountain: dark cooling crust plates floating on bright
                  orange-yellow melt (the BrawlFortress/Lava shader scrolls two layers of it).
"""
import numpy as np, math, os
from PIL import Image
_HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.environ.get('ICE_OUT') or os.path.join(_HERE, '..', '..', 'Assets', 'Art', 'IceTheme')
os.makedirs(OUT, exist_ok=True)
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

def voronoi(npts, seed, jitter=0.0):
    """wrapping Voronoi: (index of the nearest point, distance to the nearest, distance to the second nearest)"""
    r = np.random.RandomState(seed)
    pts = r.rand(npts, 2)
    wx = x + (tile_noise(4, 1.2, seed + 1) - 0.5) * jitter; wy = y + (tile_noise(4, 1.2, seed + 2) - 0.5) * jitter
    d1 = np.full((N, N), 9.0); d2 = np.full((N, N), 9.0); idx = np.zeros((N, N), int)
    for i, (px, py) in enumerate(pts):
        dx = np.abs(wx - px); dx = np.minimum(dx, 1 - dx)
        dy = np.abs(wy - py); dy = np.minimum(dy, 1 - dy)
        d = np.sqrt(dx * dx + dy * dy)
        closer = d < d1
        d2 = np.where(closer, d1, np.minimum(d2, d)); idx = np.where(closer, i, idx); d1 = np.where(closer, d, d1)
    return idx, d1, d2

def save(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).save(os.path.join(OUT, name))

# ---------------------------------------------------------------- basalt floor
idx, d1, d2 = voronoi(70, 11, jitter=0.03)
edge = d2 - d1                                         # 0 on a joint
rs = np.random.RandomState(12)
shade = rs.uniform(0.0, 1.0, 70)[idx]                  # each column top a slightly different tone
lo = np.array([0.22, 0.19, 0.24]); hi = np.array([0.36, 0.31, 0.37])
img = lo + (hi - lo) * (0.35 + 0.45 * shade)[..., None]
img *= (0.92 + 0.12 * tile_noise(24, 1.0, 13))[..., None]          # grain
ash = np.clip((tile_noise(3, 1.3, 14) - 0.55) * 3.5, 0, 1) * np.clip(tile_noise(12, 1.0, 15) * 1.4, 0, 1)
img = img * (1 - ash[..., None] * 0.75) + np.array([0.62, 0.58, 0.56]) * (ash[..., None] * 0.75)   # pale ash drifts
joint = np.clip(1 - edge / 0.010, 0, 1)
img = img * (1 - joint[..., None] * 0.55)                           # dark joints between the columns
# a few glowing cracks: the joints where a low noise is high, fading in and out along their length
glow_mask = np.clip((tile_noise(2, 1.2, 16) - 0.56) * 5, 0, 1) * (1 - ash)
crack = np.clip(1 - edge / 0.006, 0, 1) * glow_mask
halo = np.clip(1 - edge / 0.016, 0, 1) * glow_mask
img = img * (1 - halo[..., None] * 0.35) + np.array([0.75, 0.18, 0.05]) * (halo[..., None] * 0.35)
img = img * (1 - crack[..., None]) + np.array([1.0, 0.55, 0.12]) * crack[..., None]
save(img, 'BasaltGround.png')

# ---------------------------------------------------------------- lava
idx, d1, d2 = voronoi(26, 21, jitter=0.08)
edge = d2 - d1
crust_t = np.clip((edge - 0.012) / 0.03, 0, 1)         # 0 = molten channel, 1 = middle of a crust plate
rs = np.random.RandomState(22)
plate = rs.uniform(0.0, 1.0, 26)[idx]
melt_heat = np.clip(1 - edge / 0.03, 0, 1) ** 0.7       # hottest in the middle of a channel
melt = np.array([0.90, 0.22, 0.04]) + (np.array([1.0, 0.90, 0.42]) - np.array([0.90, 0.22, 0.04])) * melt_heat[..., None]
melt = melt * (0.85 + 0.25 * tile_noise(10, 1.0, 23))[..., None]
crust = np.array([0.30, 0.09, 0.06]) + np.array([0.18, 0.06, 0.04]) * plate[..., None]
crust = crust * (0.8 + 0.4 * tile_noise(20, 1.0, 24))[..., None]
glowing_plates = np.clip((tile_noise(3, 1.2, 25) - 0.5) * 2.5, 0, 1)   # some plates still glow dull red
crust = crust + np.array([0.35, 0.06, 0.0]) * glowing_plates[..., None] * 0.6
t = crust_t[..., None]
save(melt * (1 - t) + crust * t, 'LavaFlow.png')
print('ok')
