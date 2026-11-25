import bpy, bmesh, math, random, os, sys
from mathutils import Vector, Matrix, noise

# Tools/IceArt/ -> project root -> Assets/Art/IceTheme (override with the ICE_OUT environment variable)
_HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(_HERE, '..', '..'))
OUT = os.environ.get('ICE_OUT') or os.path.join(PROJECT, 'Assets', 'Art', 'IceTheme')
PREVIEW = os.environ.get('ICE_PREVIEW') or os.path.join(_HERE, 'preview')
os.makedirs(OUT, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
# ---------------------------------------------------------------- palette (8 x 8 cells of 16 px = 128 px, like the game's packs)
PALETTE = {
    'snow':        (0.96, 0.98, 1.00), 'snow_shade':  (0.78, 0.86, 0.96), 'snow_deep': (0.62, 0.73, 0.90),
    'ice_light':   (0.70, 0.93, 1.00), 'ice':         (0.38, 0.78, 0.98), 'ice_deep':  (0.18, 0.52, 0.86), 'ice_dark': (0.12, 0.33, 0.66),
    'pine':        (0.16, 0.45, 0.40), 'pine_dark':   (0.09, 0.30, 0.30), 'pine_light': (0.26, 0.60, 0.48),
    'wood':        (0.62, 0.40, 0.24), 'wood_dark':   (0.42, 0.25, 0.15), 'wood_light': (0.78, 0.55, 0.33),
    'stone':       (0.40, 0.44, 0.53), 'stone_dark':  (0.27, 0.30, 0.38), 'stone_light': (0.55, 0.60, 0.69),
    'red':         (0.86, 0.22, 0.20), 'orange':      (0.98, 0.56, 0.16), 'yellow':    (1.00, 0.84, 0.28),
    'black':       (0.10, 0.10, 0.14), 'metal':       (0.55, 0.58, 0.62),
    # 1960s-70s pieces (appended: the cells of the colours above do not move)
    'enamel':      (0.18, 0.40, 0.34), 'khaki':       (0.74, 0.66, 0.46), 'olive':     (0.44, 0.47, 0.29), 'ceramic': (0.62, 0.84, 0.76),
    # frost grass (appended)
    'frost_white': (0.93, 0.95, 1.00), 'frost':       (0.70, 0.76, 0.98), 'frost_deep': (0.46, 0.56, 0.92),
    # ice age (appended)
    'glacier':     (0.84, 0.93, 1.00), 'glacier_deep': (0.58, 0.78, 0.96), 'bone':      (0.94, 0.89, 0.76), 'bone_dark': (0.74, 0.66, 0.52),
    # gradient cells (appended): drawn as a vertical ramp, bottom -> top; sample them with grad_uv(name, t)
    'frost_grad':  (0.62, 0.68, 0.96), 'ice_grad':    (0.40, 0.72, 0.96),
    # Xmas (appended): paper, ribbons, painted iron, glowing C9 bulbs, a pine gradient for the PineGrass bush
    'xmas_red':    (0.86, 0.13, 0.15), 'xmas_red_dark': (0.56, 0.06, 0.10), 'xmas_green': (0.13, 0.52, 0.30), 'xmas_green_dark': (0.06, 0.30, 0.19),
    'gold':        (1.00, 0.78, 0.26), 'gold_dark':   (0.76, 0.50, 0.12), 'cream':     (0.98, 0.95, 0.88),
    'bulb_red':    (1.00, 0.28, 0.22), 'bulb_green':  (0.36, 1.00, 0.45), 'bulb_blue': (0.36, 0.62, 1.00), 'bulb_yellow': (1.00, 0.90, 0.36),
    'bulb_orange': (1.00, 0.58, 0.18), 'lamp_warm':   (1.00, 0.92, 0.62),
    'pine_grad':   (0.20, 0.50, 0.32),
}
GRADIENTS = {
    'frost_grad': [(0.0, (0.24, 0.30, 0.72)), (0.30, (0.47, 0.55, 0.93)), (0.66, (0.71, 0.76, 0.99)), (1.0, (0.93, 0.95, 1.00))],
    'pine_grad':  [(0.0, (0.05, 0.24, 0.16)), (0.35, (0.10, 0.42, 0.26)), (0.72, (0.22, 0.60, 0.36)), (0.86, (0.70, 0.86, 0.80)), (1.0, (0.96, 0.98, 1.00))],   # snow on the tips
    'ice_grad':   [(0.0, (0.56, 0.86, 1.00)), (0.25, (0.46, 0.80, 0.99)), (0.65, (0.62, 0.89, 1.00)), (1.0, (0.84, 0.97, 1.00))],   # pale, lighter at the foot: reads as clear ice
}

def _ramp(stops, t):
    for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
        if t <= t1:
            k = (t - t0) / (t1 - t0) if t1 > t0 else 0.0
            return tuple(a + (b - a) * k for a, b in zip(c0, c1))
    return stops[-1][1]
NAMES = list(PALETTE.keys())
CELL, GRID = 16, 8

def cell_uv(name):
    i = NAMES.index(name); cx, cy = i % GRID, i // GRID
    return ((cx + 0.5) / GRID, 1.0 - (cy + 0.5) / GRID)

def grad_uv(name, t):
    """uv inside a gradient cell: t 0 = bottom colour, 1 = top colour (kept on pixel centres)"""
    i = NAMES.index(name); cx, cy = i % GRID, i // GRID
    t = min(1.0, max(0.0, t))
    return ((cx + 0.5) / GRID, (GRID - 1 - cy) / GRID + (0.5 + t * (CELL - 1)) / (CELL * GRID))

_glow = None
def glow_material():
    """second material slot for faces that should read as lit (bulbs, lamp glass, the star): same palette, set up
    unlit in Unity by IceThemeSetup (any renderer submesh 1 of a mesh with two submeshes)"""
    global _glow
    if _glow: return _glow
    m = bpy.data.materials.new('XmasGlow'); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes['Principled BSDF']
    tex = nt.nodes.new('ShaderNodeTexImage'); tex.image = palette_material().node_tree.nodes['Image Texture'].image; tex.interpolation = 'Closest'
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color']); nt.links.new(tex.outputs['Color'], bsdf.inputs['Emission Color'])
    bsdf.inputs['Emission Strength'].default_value = 2.0
    _glow = m; return m

def apply_glow(ob, attr='glow'):
    """faces flagged in the int face attribute go to material slot 1 (XmasGlow)"""
    me = ob.data
    if attr not in me.attributes: return ob
    g = me.attributes[attr].data
    if any(g[p.index].value for p in me.polygons):
        me.materials.append(glow_material())
        for p in me.polygons:
            if g[p.index].value: p.material_index = 1
    me.attributes.remove(me.attributes[attr])
    return ob

def write_palette(path):
    """the 128 px palette (8 x 8 cells), written with Blender's image API (no Pillow needed)"""
    size = GRID * CELL
    px = [0.0] * (size * size * 4)
    for i, n in enumerate(NAMES):
        cx, cy = i % GRID, i // GRID
        for y in range(cy * CELL, (cy + 1) * CELL):
            row = size - 1 - y                      # Blender images start at the bottom row
            if n in GRADIENTS: r, g, b = _ramp(GRADIENTS[n], 1.0 - (y - cy * CELL + 0.5) / CELL)   # top of the cell = t 1
            else: r, g, b = PALETTE[n]
            for x in range(cx * CELL, (cx + 1) * CELL):
                k = (row * size + x) * 4
                px[k:k + 4] = [r, g, b, 1.0]
    img = bpy.data.images.new('IcePalette_tmp', size, size, alpha=False)
    img.colorspace_settings.name = 'sRGB'
    img.pixels = px
    img.filepath_raw = path; img.file_format = 'PNG'; img.save()
    bpy.data.images.remove(img)

_mat = None
def palette_material():
    global _mat
    if _mat: return _mat
    p = os.path.join(OUT, 'IcePalette.png'); write_palette(p)
    m = bpy.data.materials.new('IcePalette'); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes['Principled BSDF']
    tex = nt.nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(p); tex.interpolation = 'Closest'
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = 0.75
    _mat = m; return m

# ---------------------------------------------------------------- building helpers
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    global _mat, _glow; _mat = None; _glow = None

# faceted on purpose (crisp low-poly look); every other prop is smoothed by angle
FACETED = ('IcePine', 'SnowRock', 'IceCrystals', 'IceShards', 'GlacierWallA', 'GlacierWallB', 'GlacierWallC', 'GlacierBlock', 'IceSpire', 'PressureRidge', 'IceBoulder')
SMOOTH_ANGLE = 50.0

def bm_to_obj(bm, name, colors):
    """colors: list of palette names per material index stored in face.material_index"""
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    ob.data.materials.append(palette_material())
    uv = me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        u = cell_uv(colors[poly.material_index])
        for li in poly.loop_indices: uv.data[li].uv = u
        poly.material_index = 0
        poly.use_smooth = name not in FACETED
    if name not in FACETED:
        # curved surfaces smooth, hard edges (box corners, crate frames) kept sharp
        me.set_sharp_from_angle(angle=math.radians(SMOOTH_ANGLE))
    return ob

def paint(bm, faces, idx):
    for f in faces: f.material_index = idx

def add_cone(bm, segs, r1, r2, depth, mat=None):
    res = bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r1, radius2=r2, depth=depth, matrix=mat or Matrix())
    return res['verts']

def faces_of(bm, verts):
    vs = set(verts); return [f for f in bm.faces if all(v in vs for v in f.verts)]

def jitter(verts, amt, seed):
    rnd = random.Random(seed)
    for v in verts: v.co += Vector((rnd.uniform(-amt, amt), rnd.uniform(-amt, amt), rnd.uniform(-amt * 0.5, amt * 0.5)))

def bevel_object(ob, width=0.03, segs=1):
    m = ob.modifiers.new('Bevel', 'BEVEL'); m.width = width; m.segments = segs; m.limit_method = 'ANGLE'; m.angle_limit = math.radians(35)
    m.harden_normals = False

def finish(ob, name):
    """apply modifiers, origin at the base centre, triangle count"""
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    for m in list(ob.modifiers): bpy.ops.object.modifier_apply(modifier=m.name)
    me = ob.data
    minz = min(v.co.z for v in me.vertices)
    cx = sum(v.co.x for v in me.vertices) / len(me.vertices); cy = sum(v.co.y for v in me.vertices) / len(me.vertices)
    for v in me.vertices: v.co -= Vector((cx, cy, minz))
    me.update(); ob.name = name; me.name = name
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    return tris

def export_fbx(ob, name):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    path = os.path.join(OUT, name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE', path_mode='STRIP', bake_space_transform=True, add_leaf_bones=False)
    return path

GROUND_RGBA = (0.86, 0.91, 0.98, 1)
SUN_ENERGY = 3.2
# ---------------------------------------------------------------- preview render (Cycles CPU, 3/4 view like the game camera)
def render_preview(obs, path, res=520, dist=None, lift=0.55):
    scn = bpy.context.scene
    scn.render.engine = 'CYCLES'; scn.cycles.samples = 24; scn.cycles.use_denoising = True
    try: scn.cycles.denoiser = 'OPENIMAGEDENOISE'
    except Exception: pass
    scn.render.resolution_x = scn.render.resolution_y = res; scn.render.film_transparent = False
    scn.view_settings.view_transform = 'Standard'
    # bounds
    pts = [ob.matrix_world @ Vector(c) for ob in obs for c in ob.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))); mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    ctr = (mn + mx) / 2; size = (mx - mn).length
    world = bpy.data.worlds.new('w'); scn.world = world; world.use_nodes = True
    world.node_tree.nodes['Background'].inputs[0].default_value = (0.55, 0.66, 0.82, 1); world.node_tree.nodes['Background'].inputs[1].default_value = 0.9
    sun = bpy.data.lights.new('sun', 'SUN'); sun.energy = SUN_ENERGY; sun.angle = math.radians(8)
    so = bpy.data.objects.new('sun', sun); scn.collection.objects.link(so); so.rotation_euler = (math.radians(48), 0, math.radians(35))
    # ground
    bpy.ops.mesh.primitive_plane_add(size=size * 4, location=(ctr.x, ctr.y, mn.z))
    g = bpy.context.active_object; gm = bpy.data.materials.new('g'); gm.use_nodes = True
    gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = GROUND_RGBA; g.data.materials.append(gm)
    cam = bpy.data.cameras.new('cam'); cam.lens = 50; co = bpy.data.objects.new('cam', cam); scn.collection.objects.link(co)
    d = dist or size * 1.9
    direction = Vector((0.75, -1.0, lift)).normalized()
    co.location = ctr + direction * d
    co.rotation_euler = (ctr - co.location).to_track_quat('-Z', 'Y').to_euler()
    scn.camera = co; scn.render.filepath = path
    bpy.ops.render.render(write_still=True)

# ---------------------------------------------------------------- shared geometry tools (v2)
def bevel_bm(bm, width=0.03, angle_deg=30, segments=1):
    """bevel the sharp edges in the bmesh itself (new faces keep a neighbour's material index; colouring runs afterwards)"""
    bm.normal_update()
    edges = [e for e in bm.edges if len(e.link_faces) == 2 and e.calc_face_angle(0) > math.radians(angle_deg)]
    if edges:
        bmesh.ops.bevel(bm, geom=edges, offset=width, offset_type='OFFSET', segments=segments, profile=0.5, affect='EDGES', clamp_overlap=True)
    bm.normal_update()

def lathe(bm, profile, segs, rim=None, rot=0.0, matrix=None):
    """revolve [(r, z), ...] (bottom to top; last r may be 0 for a point). rim(i, k, r, z) -> (r, z) lets each ring vertex be shaped.
    returns the new faces"""
    rings = []
    for k, (r, z) in enumerate(profile):
        if r <= 1e-5:
            rings.append([bm.verts.new((0, 0, z))]); continue
        ring = []
        for i in range(segs):
            a = rot + 2 * math.pi * i / segs
            rr, zz = rim(i, k, r, z) if rim else (r, z)
            ring.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, zz)))
        rings.append(ring)
    faces = []
    for k in range(len(rings) - 1):
        a, b = rings[k], rings[k + 1]
        for i in range(segs):
            if len(b) == 1: faces.append(bm.faces.new((a[i], a[(i + 1) % segs], b[0])))
            elif len(a) == 1: faces.append(bm.faces.new((a[0], b[(i + 1) % segs], b[i])))
            else: faces.append(bm.faces.new((a[i], a[(i + 1) % segs], b[(i + 1) % segs], b[i])))
    if len(rings[0]) > 1: faces.append(bm.faces.new(list(reversed(rings[0]))))
    if len(rings[-1]) > 1: faces.append(bm.faces.new(rings[-1]))
    if matrix is not None:
        vs = {v for f in faces for v in f.verts}
        bmesh.ops.transform(bm, matrix=matrix, verts=list(vs))
    return faces

def part(bm, build, colour, bevel=0.0, angle=30):
    """build(bm) adds geometry; its edges are bevelled on their own, then every face of this part (bevel faces too)
    gets colour(face) -> palette index. Earlier parts are never touched."""
    before = set(bm.faces)
    build(bm)
    new = [f for f in bm.faces if f not in before]
    if bevel > 0:
        bm.normal_update()
        edges = {e for f in new for e in f.edges if len(e.link_faces) == 2 and e.calc_face_angle(0) > math.radians(angle)}
        if edges: bmesh.ops.bevel(bm, geom=list(edges), offset=bevel, offset_type='OFFSET', segments=1, profile=0.5, affect='EDGES', clamp_overlap=True)
        new = [f for f in bm.faces if f not in before]
    bm.normal_update()
    for f in new: f.material_index = colour(f)

def lit(n):  # how much a face looks at the light (sun from the upper right front)
    return n.x * 0.5 - n.y * 0.55 + n.z * 0.65

def box(bm, size, loc=(0, 0, 0), rot=None):
    m = Matrix.Translation(loc) @ (rot or Matrix()) @ Matrix.Diagonal((size[0], size[1], size[2], 1))
    return bmesh.ops.create_cube(bm, size=1.0, matrix=m)['verts']
