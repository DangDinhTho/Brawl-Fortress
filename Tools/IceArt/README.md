# Ice / arctic prop generator (Blender, Python)

Procedural low-poly props in the game's style, exported as FBX into `Assets/Art/IceTheme`, all sharing the 128 px
palette `IcePalette.png` (8 x 8 cells of 16 px).

## Run
    "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python Tools/IceArt/build_all.py -- [--preview] [Name ...]
- no names: all props; names: only those (e.g. `-- --preview GlacierWallA IceSpire`)
- `--preview`: renders `Tools/IceArt/preview/<Name>.png` (Cycles CPU)
- environment overrides: `ICE_OUT` (export folder), `ICE_PREVIEW` (preview folder)
- ground texture (needs numpy + Pillow): `python Tools/IceArt/ground_tex.py` -> `Assets/Art/IceTheme/IceGround.png`
Then in Unity: `Tools/Maps/Ice Theme/Setup Assets`, then `Tools/Maps/Build Frost Peak (Ice)`.

## Files
- `common.py`: palette, helpers (`part`, `lathe`, `box`, `bevel_bm`, `lit`), export, preview render
- `props1.py` .. `props6.py`, `grass.py`: builders (latest version of each prop is wired in `build_all.py`)
- `ground_tex.py`: tiling ice ground texture (cracks, frost, snow patches)

## Style rules
- **Palette is append-only**: new colours go at the end of `PALETTE`, so existing UV cells keep their colour.
- Each part is built, bevelled and coloured on its own (`part(bm, build, colour, bevel=...)`) — bevel faces never
  inherit a wrong colour.
- Smoothing: everything is smoothed by angle (50°) except the `FACETED` list (crisp low-poly pieces).
  Curved, smoothed parts use **one base colour** and let FlatKit shade them (baked light/dark tones look blotchy).
- Origin at the base centre, metres, Y up in Unity. Keep the game's density (~300-3000 tris per prop).
- 1960s-70s setting for anything man-made. Ice theme = natural only.
