"""
Builds the arctic / ice-age props (and the snow props kept for the Xmas theme) and exports them as FBX straight into
Assets/Art/IceTheme (the 128 px palette IcePalette.png is written there too).

    blender --background --python Tools/IceArt/build_all.py -- [--preview] [Name ...]

No names: every prop. --preview also renders Tools/IceArt/preview/<Name>.png (Cycles, CPU, a few seconds each).
After a build, in Unity: Tools/Maps/Ice Theme/Setup Assets (prefabs, materials, colliders), then
Tools/Maps/Build Frost Peak (Ice) to rebuild the map. Ground texture: python Tools/IceArt/ground_tex.py (numpy + Pillow).
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common
from common import reset, finish, export_fbx, render_preview, OUT, PREVIEW
import props1, props2, props3, props4, props5, props6, props7, grass

# name -> builder (the latest version of each piece)
BUILDERS = {
    # ice age theme (Frost Peak)
    'GlacierWallA': lambda: props6.build_glacier(401, 'GlacierWallA'),
    'GlacierWallB': lambda: props6.build_glacier(402, 'GlacierWallB'),
    'GlacierWallC': lambda: props6.build_glacier(403, 'GlacierWallC'),
    'GlacierBlock': props6.build_glacierblock,            # old single version (fallback)
    'GlacierBlockA': lambda: props6.build_glacierblock_v('cols', 351, 'GlacierBlockA'),
    'GlacierBlockB': lambda: props6.build_glacierblock_v('block', 352, 'GlacierBlockB'),
    'GlacierBlockC': lambda: props6.build_glacierblock_v('stack', 353, 'GlacierBlockC'),
    'GlacierBlockD': lambda: props6.build_glacierblock_v('step', 354, 'GlacierBlockD'),
    'IceChunkA': lambda: props7.chunk('chunk', 361, 'IceChunkA'),
    'IceChunkB': lambda: props7.chunk('chunk2', 362, 'IceChunkB'),
    'FrozenLog': props7.build_frozenlog,
    'IceShardCluster': props7.build_shardcluster,
    'RibCage': props7.build_ribcage,
    'BeastSkull': props7.build_beastskull,
    'BonePile': props7.build_bonepile,
    'IceHole': props7.build_icehole,
    'IceSpire': props6.build_spire,
    'PressureRidge': props6.build_ridge,
    'IceBoulder': props6.build_boulder,
    'MammothBones': props6.build_bones,
    'IceCrystals': props1.build_crystals,
    'SnowRock': props2.build_rock,
    'IceSlab': props5.build_iceslab,
    'SnowDrift': props5.build_snowdrift,
    'IceShards': props5.build_iceshards,
    'FrostGrass': grass.build_frostgrass,
    # earlier ice pieces (still used as fallbacks)
    'IceCliffA': lambda: props5.build_icecliff(201, 'IceCliffA'),
    'IceCliffB': lambda: props5.build_icecliff(202, 'IceCliffB'),
    'IceCliffC': lambda: props5.build_icecliff(203, 'IceCliffC'),
    'IceBlock': props3.build_iceblock,
    'IceWall': props2.build_wall,
    # snow / man-made pieces kept for the Xmas theme
    'IcePine': props1.build_pine,
    'SnowCrate': props2.build_crate,
    'Igloo': props2.build_igloo,
    'FrostBush': props2.build_bush,
    'WoodFence': props3.build_fence,
    'SnowTuft': props3.build_tuft,
    'Snowman': props3.build_snowman,
    'LampPost': props4.build_lamp,
    'Sled': props3.build_sled,
    'Flag': props3.build_flag,
    'Tent': props4.build_tent,
    'SnowMound': props3.build_mound,
}

def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    preview = '--preview' in args
    names = [a for a in args if not a.startswith('--')] or list(BUILDERS)
    unknown = [n for n in names if n not in BUILDERS]
    if unknown: print('UNKNOWN', unknown, '- known:', ', '.join(BUILDERS)); return
    common.GROUND_RGBA = (0.42, 0.55, 0.72, 1); common.SUN_ENERGY = 2.0
    print('OUT', OUT)
    for n in names:
        reset(); ob = BUILDERS[n](); tris = finish(ob, n); export_fbx(ob, n)
        if preview: render_preview([ob], os.path.join(PREVIEW, n + '.png'), res=320)
        d = ob.dimensions
        print('BUILT %-14s %5d tris  %.2f x %.2f x %.2f m' % (n, tris, d.x, d.y, d.z))

main()
