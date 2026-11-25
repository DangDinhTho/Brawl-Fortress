# Brawl Fortress — notes for Claude Code

Handoff from a long claude.ai session (Nov 2026). Unity 6000.3.10f1 mobile TPS, a third-person remake of Tho's FPS
project TF2_AI (E:\UnityProjects\TF2_AI). Photon PUN 2, PlayFab (title F08BD), AdMob, Cinemachine, Behavior Designer,
FlatKit (toon shading), DOTween. Windows, Rider.

## Working with Tho
- **Always answer in Vietnamese.** He is technical and direct.
- When asking him to change a setting, give its **exact label or config key** (e.g. Title Data key `max_death_time`,
  WeaponConfig field `Bullet Speed Scale`), never a paraphrase.
- Game setting is the **1960s-70s** (TF2-like): wood, painted/enamelled metal, canvas, incandescent lamps. Nothing modern.
- Art direction: Brawl Stars-like — chunky, bright, readable; square blocky UI (GUI PRO Kit frames), not glossy.
- Safe area: everything readable/usable stays inside the notch-safe area (landscape: left/right only); only decoration
  may reach the screen edges.

## Safety rules learned the hard way
- **Check you are in this project before any write.** Another Unity project (`E:\UnityProjects\Fish Sort`) is often open
  and MCP tools have jumped to it several times. Guard every editor command with
  `if (!Application.dataPath.Contains("Brawl Fortress")) return;`.
- **FlashScene** (`Assets/Scenes/FlashScene.unity`): Tho never leaves his own edits there unsaved. If it shows unsaved
  changes you did not make, close it without saving. To test in Play mode start from FlashScene via
  `EditorSceneManager.playModeStartScene` instead of opening it (opening it marks it dirty); reset it to null afterwards.
- Never save or discard another unsaved scene without asking.
- `Assets/SimpleLocalization/Resources/Localization/Main.csv` must keep **CRLF** line endings (the parser splits on
  `Environment.NewLine`; LF broke every string in the editor once). Columns: Key, English, Russian, Thai, Spanish, Vietnamese.
  No commas inside values.
- Write files with a retry (Unity locks them while compiling); normalise CRLF before matching text anchors.

## Live config (PlayFab Title Data) — players reopen the game to get new values
Balance and switches live in Title Data; the code has fallbacks. Keys in use (non-exhaustive):
`weapon_stats` (player weapon stats; interval etc. are ONLY here, no code fallback), `battle_map_times`,
`default_battle_time`, `base_death_time` / `max_death_time` (respawn wait, default 8 / 12 s), `mode_unlock_level`,
`multiplay`, `max_ticket_ads_per_day`, `admob_ids` (per platform: inter, rewarded, app_open, mrec, banner),
`show_mrec_matching`, `show_inter_back_home`, `show_inter_die`.
- **Do not add new mode / weapon / map ids to Title Data until a store build that parses Title Data leniently is out**
  (old builds choke on unknown enum names). This includes `Map_Brawl_FrostPeak` and the Yeti boss maps.
- Changes Tho still has to make on PlayFab: `weapon_stats` Sniperrifle `interval` 2.75 -> 1.75, Minigun `interval`
  0.1 -> 0.0833; `battle_map_times`: Payload maps 210 (Map_Brawl_Payload 240), MvM maps 360.

## Networking notes
- Connect only through `LobbyManager.ConnectTo(region)` (ConnectUsingSettings with a copied AppSettings +
  FixedRegion). `PhotonNetwork.ConnectToRegion` does not set the AppId -> InvalidAuthentication after every launch.
- Solo matches use Photon **OfflineMode**; while offline Photon reports IsConnected = true. `LobbyManager.LeaveOfflineMode()`
  drops it in Home (never while a solo match is starting).
- The Find a game button is always shown; when offline it offers Retry (PopupConfirm + PopupWaitingFocus spinner).

## Map builder (Assets/Editor/MapBuilder)
- `BrawlMapBuilder.cs`: maps defined as ASCII half-layouts (`Def`: half rows + center, mirrored for the other team).
  Tiles: `#` wall, `x` small obstacle, `f` fence, `t` tall prop, `b` bush (4 clumps per tile), `.` floor/decor.
  Tile size 1.5 m. `Theme`: Western, Bamboo, Cave, Moon, Mix, **Ice**. `Build(def)` places tiles, border, ground,
  lighting, bushes (Tools/Bush/Build Bush Areas logic), patrol points, NavMesh, map card, icon; it refuses to run with
  unsaved scenes open.
- Bush detection: renderer/object names must contain **"grass" or "bush"**. `GrassHeight` stretches bushes to ~1.95 m.
- **Ice theme = ice age / wild arctic** (natural pieces only). Defined in `BrawlMapBuilder.IceMaps`
  (`Map_Brawl_FrostPeak`, Competitive, Cavern layout + ridges). Menu: `Tools/Maps/Build Frost Peak (Ice)`.
  - border: `GlacierWallA/B/C` replace the old cliff pieces (scaled to their length, detailed face turned to the arena)
  - `#` GlacierBlockA-D (split / block / stack / icicles: rounded boxes with a pale `ice_grad` ramp (lighter at the foot =
    fake clear ice; real transparency was avoided: see-through walls, sorting, mobile cost), white zigzag cracks, thick rounded
    snow slab with flat drips; collider clamped to 1.45 m; builder: random pick, 90° turns +-3°, height x0.96-1.04;
    old GlacierBlock = fallback). IcePalette is **Bilinear** (flat cells sampled at their centre, gradient cells smooth).
    bmesh gotcha: `create_icosphere` invalidates every python BMVert reference; `v.tag` is used by ops (track with a layer), `x` IceBoulder / SnowRock, `f` PressureRidge, `t` IceSpire / IceCrystals,
    `b` FrostGrass (1.5 m clump of 11 broad almond leaves in 2 rings around a core, each leaf a smooth deep-blue ->
    lavender -> white ramp from the `frost_grad` gradient cell of IcePalette (`GRADIENTS` / `grad_uv` in common.py),
    **dome custom normals** for soft cel shading; builder scale 1.3 = ~1.95 m so GrassHeight does not stretch it),
  - More props (Tools/IceArt/props7.py): `x` also IceChunkA/B (small blocks, GlacierBlock style) and FrozenLog,
    `t` also IceShardCluster; walk-through decor BonePile, IceHole (decor + floor scatter), RibCage / BeastSkull (up to 4
    on open ground away from walls, spawns and bushes; RibCage also in the backdrop).
    Per-tile `Random`s have consecutive seeds -> correlated picks (some list entries never came up): choices go through
    `MixRnd(rnd)` (murmur finalizer), still per tile so both halves stay mirrored.
  - In-editor look check: render a `HideAndDontSave` temp camera to PNG (does not dirty the scene) and read the image.
    decor IceSlab / IceShards / SnowDrift / SnowRock / IceCrystals / MammothBones, floor `IceFloor` (tiling `IceGround.png`),
    backdrop glaciers / spires / boulders, `ScatterIceFloor` adds ice plates on the open floor.
  - GlacierWall front (detailed face, Blender -Y) is local **+Z** in Unity; border and backdrop turn +Z to the arena.
  - Snow / man-made props (Snowman, WoodFence, LampPost, Tent, Sled, Flag, SnowCrate, Igloo, IcePine, FrostBush,
    SnowTuft, SnowMound, IceBlock, IceWall, IceCliffA/B/C) stay in the project for a **coming Xmas theme**.
- `IceThemeSetup.cs` (`Tools/Maps/Ice Theme/Setup Assets`): import settings, materials (`IceProps` FlatKit copied from
  the matte character material, `IceGround`, `IceCliff`), one prefab per FBX in `Assets/Art/IceTheme/Prefabs` with
  colliders, and the `IceFloor` mesh/prefab. Run it after any new/changed FBX.

## Blender pipeline (Tools/IceArt) — see Tools/IceArt/README.md
Props are generated by Python (bmesh) and exported as FBX straight into `Assets/Art/IceTheme`:
`blender --background --python Tools/IceArt/build_all.py -- [--preview] [Name ...]`. Blender 5.2 is installed
(C:\Program Files\Blender Foundation\Blender 5.2\blender.exe). Then run Setup Assets and rebuild the map in Unity.

## State at handoff
- `Assets/Art/IceTheme` must contain the ice-age pieces (GlacierWall*, GlacierBlock, IceSpire, PressureRidge,
  IceBoulder, MammothBones) and the new pale IceGround.png / IcePalette.png — shipped in this handoff zip.
- Next: Setup Assets -> Build Frost Peak (Ice) -> check the glaciers face the arena, grass ~1.9 m, bushes work, then
  a solo match on Frost Peak. Tho reviews visuals himself (editor screenshots could not be read by the assistant).
- Then: an **Xmas theme** using the snow props.
- Not yet verified in play / on device: iOS Photon connection fix, solo-room bot fill on device, end-of-match freeze +
  ad on the Home button, bot weapon reach changes, Pyro flamethrower look, Spy cloak foot ring, Yeti boss combat.
- Open: Spy Revolver `ShotPoint` is ~0.86 m off the barrel in its prefab (the bone above it has scale 150 — check by eye).
