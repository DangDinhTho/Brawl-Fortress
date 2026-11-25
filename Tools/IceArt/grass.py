import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
import common

H = 1.5   # clump height: x1.3 in the map builder = ~1.95 m, so GrassHeight does not have to stretch it

def build_frostgrass(seed=71):
    """Brawl-style frost bush: a dense clump of almond-shaped leaves (soft point, convex sides) in two staggered rings
    around a solid core. Every leaf runs a smooth gradient from deep blue at its root through lavender to near white at
    its tip (the 'frost_grad' palette cell), so overlapping leaves separate by the dark root of the front leaf against
    the pale tip of the one behind. Dome normals keep the cel shading soft."""
    rnd = random.Random(seed)
    bm = bmesh.new(); C = ['frost_grad', 'frost_deep']
    gt = bm.verts.layers.float.new('gt')          # gradient position per vertex (0 root .. 1 tip)
    # (angle, base radius, height, outward lean at the top, half width)
    leaves = []
    for k in range(4):   # inner ring: tall, upright
        leaves.append((k * math.pi / 2 + rnd.uniform(-0.2, 0.2), 0.10, H * rnd.uniform(0.95, 1.0), rnd.uniform(0.03, 0.06), rnd.uniform(0.22, 0.24)))
    for k in range(7):   # outer ring: shorter, leaning out, staggered against the inner ring
        leaves.append((k * 2 * math.pi / 7 + 0.3 + rnd.uniform(-0.12, 0.12), 0.27, H * rnd.uniform(0.76, 0.88), rnd.uniform(0.08, 0.12), rnd.uniform(0.22, 0.25)))
    TS = (0.0, 0.22, 0.42, 0.58, 0.72, 0.84, 0.93)
    def width(t, w):
        if t <= 0.42: return w * (0.70 + 0.30 * math.sin(t / 0.42 * math.pi / 2))
        x = (t - 0.42) / 0.58
        return w * max(0.0, 1.0 - x ** 2.2) ** 0.6           # full, convex sides closing to a soft point
    def make(bm):
        # solid core closing in a dome (no flat cap: seen from above between the leaves it showed as dark blue blots);
        # it takes the same ramp by height, so what shows through is the leaves' lavender
        for f in lathe(bm, [(0.26, 0.0), (0.26, H * 0.5), (0.20, H * 0.62), (0.10, H * 0.69), (0.0, H * 0.71)], 8):
            for v in f.verts: v[gt] = min(1.0, v.co.z / (H * 0.71)) * 0.72
        for (a, r0, h, lean, w) in leaves:
            out = Vector((math.cos(a), math.sin(a), 0)); side = Vector((-out.y, out.x, 0))
            base = out * r0
            rings = []
            for t in TS:
                wv = max(width(t, w), 0.025); th = 0.03 + wv * 0.22
                c = base + out * (lean * h * t * t) + Vector((0, 0, h * t))
                ring = [bm.verts.new(c + side * wv - out * wv * 0.12), bm.verts.new(c + out * th),
                        bm.verts.new(c - side * wv - out * wv * 0.12), bm.verts.new(c - out * th)]   # slightly convex face
                for v in ring: v[gt] = t
                rings.append(ring)
            tip = bm.verts.new(base + out * (lean * h) + Vector((0, 0, h))); tip[gt] = 1.0
            for i in range(len(rings) - 1):
                for j in range(4):
                    bm.faces.new((rings[i][j], rings[i][(j + 1) % 4], rings[i + 1][(j + 1) % 4], rings[i + 1][j]))
            for j in range(4): bm.faces.new((rings[-1][j], rings[-1][(j + 1) % 4], tip))
            bm.faces.new(list(reversed(rings[0])))
    part(bm, make, lambda f: 0)
    ob = bm_to_obj(bm, 'FrostGrass', C)
    me = ob.data
    # gradient uvs: each vertex samples the ramp at its own position along its leaf
    g = me.attributes['gt'].data; uv = me.uv_layers['UVMap'].data
    for li, lp in enumerate(me.loops): uv[li].uv = grad_uv('frost_grad', g[lp.vertex_index].value)
    me.attributes.remove(me.attributes['gt'])
    dome_normals(me, H)
    return ob

def dome_normals(me, h):
    """custom normals: outward from the clump axis on the sides, bending up to straight up at the top"""
    for p in me.polygons: p.use_smooth = True
    if 'sharp_edge' in me.attributes: me.attributes.remove(me.attributes['sharp_edge'])
    if 'sharp_face' in me.attributes: me.attributes.remove(me.attributes['sharp_face'])
    ns = []
    for v in me.vertices:
        u = min(1.0, max(0.0, (v.co.z / h - 0.45) / 0.55))
        r = Vector((v.co.x, v.co.y, 0.0))
        r = r.normalized() if r.length > 1e-3 else Vector((0, 0, 0))
        ns.append((r * (1.0 - u * u) + Vector((0, 0, 0.3 + 1.6 * u))).normalized())
    me.normals_split_custom_set_from_vertices(ns)

if __name__ == '__main__':
    common.FACETED = tuple(x for x in common.FACETED if x != 'FrostGrass')   # smooth by angle like the other organic props
    reset(); ob = build_frostgrass(); tris = finish(ob, 'FrostGrass'); export_fbx(ob, 'FrostGrass')
    render_preview([ob], f'{PREVIEW}/FrostGrass.png', res=320)
    # a bush tile: 2 x 2 clumps 0.74 m apart, as the map builder places them
    me = ob.data; obs = []
    for i, (dx, dy, rz) in enumerate([(-0.37, -0.37, 0.3), (0.37, -0.37, 1.9), (-0.37, 0.37, 3.4), (0.37, 0.37, 5.0), (1.11, -0.37, 2.5), (1.11, 0.37, 4.2), (-1.11, 0.0, 0.9)]):
        o = bpy.data.objects.new('g%d' % i, me); bpy.context.collection.objects.link(o)
        o.location = (dx, dy, 0); o.rotation_euler = (0, 0, rz); o.scale = (1.0, 1.0, 1.0 + 0.1 * (i % 3)); obs.append(o)
    ob.hide_render = True
    bpy.ops.object.select_all(action='DESELECT')
    render_preview(obs, f'{PREVIEW}/FrostGrass_tile.png', res=420, lift=0.75)
    for o in bpy.data.objects:
        if o.type == 'MESH' and o.name.startswith('Plane'):
            m = o.data.materials[0]; m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.36, 0.55, 0.78, 1)
    bpy.context.scene.render.filepath = f'{PREVIEW}/FrostGrass_tile.png'; bpy.ops.render.render(write_still=True)
    print('BUILT FrostGrass', tris, tuple(round(d, 2) for d in ob.dimensions))
