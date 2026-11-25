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
import props1, props2, props3, props4, props5, props6, props7, props8, props9, props10, props11, props12, grass

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
    # Xmas theme
    'XmasTree': props8.build_xmastree,
    'GiftStackA': lambda: props8.build_giftstack('A', 801, 'GiftStackA'),
    'GiftStackB': lambda: props8.build_giftstack('B', 802, 'GiftStackB'),
    'GiftBox': props8.build_giftbox,
    'LightFence': props8.build_lightfence,
    'XmasLamp': props8.build_xmaslamp,
    'CandyCane': props8.build_candycane,
    'Snowman2': props8.build_snowman2,
    'Sleigh': props8.build_sleigh,
    'BigBauble': props8.build_bigbauble,
    'PineGrass': props8.build_pinegrass,
    # Egypt theme
    'SandBlockA': lambda: props9.build_sandblock('A', 901, 'SandBlockA'),
    'SandBlockB': lambda: props9.build_sandblock('B', 902, 'SandBlockB'),
    'SandBlockC': lambda: props9.build_sandblock('C', 903, 'SandBlockC'),
    'RuinLow': props9.build_ruinlow,
    'Obelisk': props9.build_obelisk,
    'PalmTree': props9.build_palm,
    'CatStatue': props9.build_catstatue,
    'LotusColumn': props9.build_lotuscolumn,
    'DigCrates': props9.build_digcrates,
    'Amphorae': props9.build_amphorae,
    'Sarcophagus': props9.build_sarcophagus,
    'BrokenColumn': props9.build_brokencolumn,
    'Pyramid': props9.build_pyramid,
    'DigTent': props9.build_digtent,
    'PapyrusGrass': props9.build_papyrus,
    'SandCliffA': lambda: props9.build_sandcliff('A', 911, 'SandCliffA'),
    'SandCliffB': lambda: props9.build_sandcliff('B', 912, 'SandCliffB'),
    'SandCliffC': lambda: props9.build_sandcliff('C', 913, 'SandCliffC'),
    'TempleGate': props9.build_templegate,
    'IceGate': props10.build_icegate,
    'XmasGate': props10.build_xmasgate,
    # Volcano theme
    'BasaltBlockA': lambda: props11.build_basaltblock('A', 1101, 'BasaltBlockA'),
    'BasaltBlockB': lambda: props11.build_basaltblock('B', 1102, 'BasaltBlockB'),
    'BasaltBlockC': lambda: props11.build_basaltblock('C', 1103, 'BasaltBlockC'),
    'BasaltLow': props11.build_basaltlow,
    'ObsidianSpire': props11.build_obsidianspire,
    'FireCrystal': props11.build_firecrystal,
    'CharredTree': props11.build_charredtree,
    'LavaRock': props11.build_lavarock,
    'ObsidianChunk': props11.build_obsidianchunk,
    'SurveyCrates': props11.build_surveycrates,
    'LavaCrack': props11.build_lavacrack,
    'PumiceRocks': props11.build_pumicerocks,
    'BasaltRimA': lambda: props11.build_basaltrim('A', 1111, 'BasaltRimA'),
    'BasaltRimB': lambda: props11.build_basaltrim('B', 1112, 'BasaltRimB'),
    'BasaltRimC': lambda: props11.build_basaltrim('C', 1113, 'BasaltRimC'),
    'VolcanoGate': props11.build_volcanogate,
    'Volcano': props11.build_volcano,
    'LavaVent': props11.build_lavavent,
    'BasaltBouldersA': lambda: props12.build_boulders('A', 1301, 'BasaltBouldersA'),
    'BasaltBouldersB': lambda: props12.build_boulders('B', 1302, 'BasaltBouldersB'),
    'BasaltSlabsA': lambda: props12.build_slabs('A', 1303, 'BasaltSlabsA'),
    'BasaltSlabsB': lambda: props12.build_slabs('B', 1304, 'BasaltSlabsB'),
    'ObsidianWall': props12.build_obsidianwall,
    'TuffRockA': lambda: props12.build_tuff('A', 1305, 'TuffRockA'),
    'TuffRockB': lambda: props12.build_tuff('B', 1306, 'TuffRockB'),
    'MagmaPillar': props12.build_magmapillar,
    'BurntPine': props12.build_burntpine,
    'LavaBombs': props12.build_lavabombs,
    'MineCart': props12.build_minecart,
    'CooledFlow': props12.build_cooledflow,
    'ScorchGrassA': lambda: props12.build_scorchgrass('A'),
    'ScorchGrassB': lambda: props12.build_scorchgrass('B'),
    'EmberTuft': props12.build_embertuft,
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
