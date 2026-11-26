# Brawl Fortress — notes for Claude Code

Handoff from a long claude.ai session (Nov 2026). Unity 6000.3.10f1 mobile TPS, a third-person remake of Tho's FPS
project TF2_AI (E:\UnityProjects\TF2_AI). Photon PUN 2, PlayFab (title F08BD), AdMob, Cinemachine, Behavior Designer,
FlatKit (toon shading), DOTween. Windows, Rider.

## Working with Tho
- **Always answer in Vietnamese.** He is technical and direct.
- When asking him to change a setting, give its **exact label or config key** (e.g. Title Data key `max_death_time`,
  WeaponConfig field `Bullet Speed Scale`), never a paraphrase.
- Game setting is the **1960s-70s** (TF2-like): wood, painted/enamelled metal, canvas, incandescent lamps. Nothing modern.
  Tho removed props that looked modern: triangular hazard / warning signs, black-yellow hazard stripes. Check every new
  prop against the era before building it (no road signs, plastic, safety markings, modern equipment).
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
`show_mrec_matching`, `show_inter_back_home`, `show_inter_die`, `cart_payload_speed_scale` (number, default 1:
  multiplies the Payload cart speed on every map after it is fitted to the track), `free_map` (default missing / null:
  a list of map enum names that become free on top of the default free maps, e.g. `["Map_Brawl_JingleSquare",
  "Map_Brawl_LanternEve"]` for Christmas; a JSON array or names separated by commas; unknown names are skipped; old
  builds do not read this key, so it may list new map ids; free maps show first in PopupSelectMap).
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

## In-match HUD (Brawl Stars style, Nov 2026)
- Overhead bar = `OverheadBarView` (Assets/Scripts/UI), built at runtime by `HeaderUIForFun` over each character
  prefab's old bar on its world canvas (scale 0.002: 1 unit = 2 mm). Look in `Resources/UI/OverheadBarStyle`:
  chunky rounded bar (drop shadow, thick near-black border, dark back), saturated team fill (mine green / ally blue /
  enemy red) with a darker lower half and a light band, cut straight at the health ratio by a RectMask2D; ruler ticks
  every `BrawlHudKit.TickStep(max)` HP (25 for 125-200 HP, 50 for 300, 500 for a 5000 HP boss); white damage trail +
  white hit flash; fill pulses below 25%; big bold HP number on the bar; bold name above in a light team tint; the
  local player's orange ammo bar under it (one segment per bullet up to 8, else one bar with quarter ticks; the
  segment being reloaded fills in pale orange; frame blinks red when empty). The whole group is scaled by `scale`
  (1.55: at 1.0 the HP number was unreadable from the match camera).
- `BrawlHudKit`: runtime rounded-rect sprites (drawn at 4x, `pixelsPerUnitMultiplier` 4), HUD text style (Lilita One,
  thick dark outline + drop shadow) through ONE shared material per font + outline (`HudMaterial`; do not use TMP's
  `outlineWidth` setter or `fontMaterial` for HUD text: they make a material per text, and a material instanced from
  another font keeps that font's atlas = garbled glyphs).
- Damage numbers (UltimateTextDamage, `TextDamageItem_default_stack_TMP`): restyled per pooled item in
  `UltimateTextDamageManager.AllocateOneInstance` -> `BrawlHudKit.StyleDamageNumber` (Lilita One, red vertical
  gradient, thick outline, 1.55x size).
- FootRing: thicker band (0.15 m) with a dark outline on both edges (`_Outline`, `_OutlineColor` in the shader), lighter
  inner half, soft team disc; colours come from OverheadBarStyle so ring and bar match; the reload arc is off
  (`ShowReloadArc`): the ammo bar shows the reload.
- Checked in a solo match (camera renders, Wild West): bars / names / ammo readable at 1280x720, rings thick and
  outlined. The overlay damage numbers could not be screenshotted from the editor (styling checked on the live
  items: Lilita One, HUD material, gradient). Preview image: Assets/Screenshots/hud_brawl_style.png.

## Attack controls (Brawl Stars style, Nov 2026)
- The right-hand stick in a match (`Arena.unity`, Canvas/PlayerControllerGroup/Attack Joystick) is an `AttackJoystick`
  (Assets/Scripts/Player, subclass of the Joystick Pack's `Joystick`, whose `OnDrag` / `input` / `handle` / `canvas`
  were opened up for it). The drag is measured from the touch point, so a tap anywhere on the button is a tap.
  - Tap weapons (rocket, shotgun, sniper, pistol, grenades...): tap = auto-aim (`AutoAim.FindTarget`: nearest enemy
    within `range` + 0.5 m, not hidden in a bush, not a cloaked Spy or a Spy disguised as our team, characters before
    buildings, targets in the open before targets behind a wall; no lead, like Brawl Stars; none = straight ahead);
    drag + release = that way; drag back to the centre (< 0.2, where the indicator hides) + release = cancel. A tap up
    to `tapBuffer` 0.15 s before the cooldown ends still fires; on an empty clip the indicator flashes red instead.
  - Hold weapons (melee, minigun, flamethrower, SMG, syringe gun, medigun): fire from the moment the stick is pressed;
    not dragged = at the auto target, followed as it moves; dragged = the stick's way; back in the centre = pause. The
    Medigun auto-aims at the most hurt teammate in range.
  - The player snaps to the shot's direction (no turning delay). `AimIndicator` flashes toward an auto-aimed shot and
    draws a pulsing ring (red enemy / green teammate) under the target; its shader's stencil ref is `_StencilRef`
    (fan 77, ring 78). `PlayerFPSController.UpdateFireLegacy` keeps the old logic for a UI without AttackJoystick.
  - Tested in Play mode (solo, Soldier): auto-aimed rocket turned 90 deg onto the bot, shovel held = 8 swings tracking the
    bot until it died, cancel / manual aim / rim tap behave as above. Not yet tried on a phone.

## Map selection
- One map per art theme per mode. The old themes are Western (orange canyon), Bamboo (green), Cave (blue rock,
  crystals), Moon (purple) and Mix (orange desert floor with mixed cliffs = reads as a second desert). Hand-made old
  maps: BrawlScene "Wild West" / Map_Brawl_MvM = Western, BrawlScene_2 / Map_Brawl_Koth = Bamboo, ShowDown /
  Map_Brawl_CTF = Moon, Map_Brawl_Payload / ZombieYard = Cave. 24 older maps that repeated a theme are **retired**
  (the 5 Mix maps FrontierRuins, BorderSiege, CrossroadRuins, FrontierConvoy, LastStand since Nov 2026): listed in
  `BrawlMapBuilder.Retired`, cards removed from `PopupSelectMap` (menu `Tools/Maps/Remove Retired Map Cards`; AddCard
  never re-adds them). Their scenes stay in the build and their `Map` enum values stay: all clients share Photon
  AppVersion 1.0, so an old client's room on a retired map must still load, and the enum is sent as a byte.
- `DefaultTicketCosts` lists only the kept maps, priced in card order: in each mode the first 3 maps are free, the
  next 3 cost 1, 2, 3 tickets, all others 5. **Boss Battle and Zombie have no free map**: 1, 2, 3, then 5.
  Title Data `map_config` overrides a map; `free_map` makes more maps free. A tab switch selects the mode's first
  card even when it costs tickets (Boss / Zombie): `autoPicked`, so Done asks the price (`using_ticket_confirm`) or
  offers tickets (`TicketAds.Offer`) like tapping the card does.
- `PopupSelectMap`: no "All" tab (removed); it opens on the tab of the selected map's mode (`RevealTab` scrolls the
  tab bar to it). `SortCards`: free maps first (the `free_map` ones, then the default free ones), then the paid ones
  in card order. `FitCards` zooms the card grid (content `localScale`, content anchored top-left at viewport width /
  zoom) so `columns` (3) fill the list's width, clamped to `maxCardScale` 1.3, one column less below `minCardScale`
  0.85: 300 px cards at 16:9, ~343 px at 19.5:9 (were 286 px with an empty strip on the right; on 4:3 they ran off).

## Map builder (Assets/Editor/MapBuilder)
- `BrawlMapBuilder.cs`: maps defined as ASCII half-layouts (`Def`: half rows + center, mirrored for the other team).
  Tiles: `#` wall, `x` small obstacle, `f` fence, `t` tall prop, `b` bush (4 clumps per tile), `.` floor/decor.
  Tile size 1.5 m. `Theme`: Western, Bamboo, Cave, Moon, Mix, **Ice**. `Build(def)` places tiles, border, ground,
  lighting, bushes (Tools/Bush/Build Bush Areas logic), patrol points, NavMesh, map card, icon; it refuses to run with
  unsaved scenes open.
- Bush detection: renderer/object names must contain **"grass" or "bush"**. `GrassHeight` stretches bushes to ~1.95 m.
- **Ice theme = ice age / wild arctic** (natural pieces only). Defined in `BrawlMapBuilder.IceMaps` (now in `AllDefs`):
  `Map_Brawl_FrostPeak` (Competitive), `FrostVault` (CTF), `GlacierThrone` (KOTH), `FrozenRail` (Payload, PathE),
  `IcebreakerPass` (MvM, PathB), `YetiGlacier` (Boss Battle, **Yeti** via `ConfigManager.bossOfMap`),
  `FrostbiteHollow` (Zombie). Ticket costs in `DefaultTicketCosts`. Menus: `Tools/Maps/Build Frost Peak (Ice)`,
  `Tools/Maps/Build All Ice Maps` (one map per call if driven over MCP: a 7-map run times out).
  None of these ids may go into Title Data yet (see Live config).
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

- **Xmas theme** (`Theme.Xmas`, Assets/Editor/MapBuilder/BrawlMapBuilderXmas.cs, props in Tools/IceArt/props8.py, same
  folder/palette as Ice): `#` GiftStackA/B (+ GlacierBlockB), `x` GiftBox / Sleigh / IceChunkA, `f` LightFence, `t`
  XmasTree / XmasLamp / CandyCane / Snowman2, `b` PineGrass, decor BigBauble / small GiftBox, floor `SnowFloor`
  (`snow_tex.py` -> SnowGround.png). Glacier border and Ice lighting. C9 bulbs, lamp glass and the star are a second
  material slot -> `XmasGlow.mat` (unlit). Menu `Tools/Maps/Build Xmas Maps` (drive one or two maps per MCP call).
  Day maps: JingleSquare (Comp), PresentHeist (CTF), TreeTopHill (KOTH), SleighRide (Payload), ToyWorkshop (MvM),
  SnowGlobe (Boss, Yeti), CandyCaneAlley (Zombie). Night maps (`Def.night`): LanternEve, MidnightGifts, StarlightHill,
  NightExpress, CandleWorks, YetiEve (Yeti), SilentNight. Layouts reuse retired maps' layouts or mirrored ones.
  **Night**: FlatKit has `_LightContribution 0` (scene light barely matters, Built-in RP), so `XmasNight()` swaps
  materials: IcePropsNight / SnowGroundNight (dark blue `_Color` / `_ColorDim`), XmasGlowNight (IcePaletteWarm.png =
  amber glow), adds additive `WarmPool` quads under lamps / trees / light fences, navy fog + camera background. Night
  `t` tiles favour lamps; `NightLamp` adds up to 6 lamps per half on free tiles next to walls, >= 4.5 m apart.
  The old faceted SnowDrift looks like a white log on the snow: not used in Xmas.

- **Egypt theme** (`Theme.Egypt`, BrawlMapBuilderEgypt.cs, props in Tools/IceArt/props9.py, same folder/palette): a 1960s
  dig in painted sandstone ruins. `#` SandBlockA/B/C (sand_grad ramp + painted glyph band), `x` DigCrates / Amphorae /
  Sarcophagus, `f` RuinLow, `t` Obelisk / PalmTree / CatStatue / LotusColumn, `b` PapyrusGrass, decor BrokenColumn /
  small Amphorae / DigCrates, backdrop Pyramid / palms / DigTent, floor `SandFloor` (`sand_tex.py`). Border:
  `SandCliffA/B/C` (layered sandstone, B with a carved temple front in a recess) replace the desert cliffs; they stand on
  the ground and are stretched to the old cliff's top (the old cliffs are sunk 0.3-3 m, which buried the temple front);
  collider = rock body only (not the rubble in front). `TempleGate` at z +-22.5 closes the gap behind each spawn where
  the base scene's Gate arch stood (Western keeps that Gate; the gap is walled by the invisible VitualWall).
  **Lighting** (Tho: sunny, NOT yellow): Western scene lighting + `EgyptLook` (the Western ambient is dark grey and FlatKit adds it:
  bright near-neutral trilight ambient, light haze, sun) + `EgyptSun` after the mode objects: IceProps / SandGround swapped
  for `IcePropsDesert` / `SandGroundDesert` (`_Color` ~1.05, a light neutral `_ColorDim` instead of the blue-grey one).
  Ice and Xmas close that gap the same way (`BrawlMapBuilder.PlaceGates`): `IceGate` (glacier arch, natural ice) and
  `XmasGate` (timber arch, pine garland, C9 bulbs, wreath, lanterns), both in Tools/IceArt/props10.py. Maps: SunTemple, ScarabVault, ObeliskHill, NileExpress, DuneFoundry, PharaohsCourt (Hale), MummyValley.
- Unity keeps a scene / texture file memory-mapped after it was open or imported (IOException "user-mapped section",
  Win32 1224). `Build` falls back to saving the base scene over the map through Unity; a locked map icon is kept
  (`WriteMapped`, warning in the build log; `AssetDatabase.ReleaseCachedFileHandles` does not free it): restart Unity
  and rebuild that map, or run `Tools/Maps/Refresh Recent Map Icons` (icons only: Payload, Egypt, Factory, Circus).
- **Payload cart & pace** (Nov 2026): `BombCartController` raises its `speeds` (metres per physics step, now a floor)
  so a whole uncontested push from the cart to the target takes `pushShare` of the match time (0.40 / 0.30 / 0.24 with
  1 / 2 / 3+ pushers; match time = `ConfigManager.BattleTimeOf`, like the timer; length measured along the coaster's
  Bezier samples). Then x Title Data `cart_payload_speed_scale` (default 1, read with a dot decimal, must be > 0). Before, one fixed speed (0.4 / 0.6 / 0.75 m/s) made the 65 m tracks take 77% of a match with one
  pusher: blue lost. Tracks run 22.6-65 m. The cart is 1.25 x (`CartScale`, still on the 0.95 m gauge) with a 2.5 m
  push zone (`CartPushRadius`, EntryFieldOfView.radius): `SizeCart` in Build(), menu `Tools/Maps/Apply Payload Cart
  Size` for the existing scenes.
- **Map_Brawl_Payload** (hand-made; the generated maps clone its cart systems) had its `NavMeshSurface` switched off
  since the first commit (no NavMesh: bots stood still) and a cart path up to 2.4 m off its hand-laid rail pieces. Menu
  `Tools/Maps/Fix Payload Source Map` rebuilds the fragments through the rail pieces (under `rail`), switches the
  surface on and rebakes. `BakeNavMesh` accepts both `NavmeshSurface` (generated) and `NavMeshSurface` (hand-made).
  A scan of all 139 build maps (NavMesh, spawns on it, red-to-blue path, rail vs cart path) found no other broken map.
- **Rail track** (Payload, `PlaceRails` in BrawlMapBuilderObjective.cs): one procedural mesh on the shared palette
  material (IceProps; Xmas night: IcePropsNight; Egypt: swapped to the desert one): rails with a head / web / foot
  profile (dark worn head, rust below), uneven rough-sawn sleepers (length, width, a few degrees askew, chamfered; 85%
  wood_dark, 15% wood, no light planks: Tho found the contrast too strong),
  tie plates and spikes, fishplates every 9 sleepers, a lumpy neutral grey (`metal`) ballast bed with ragged edges
  (basalt on the volcano).
  Palette cells are hard-coded (`PWood` etc. = index in `PALETTE` of common.py). Quads: clockwise = front in Unity.
  Write meshes into an existing asset with the Mesh API (`Clear` + `Set*`): `EditorUtility.CopySerialized` left the
  editor drawing the old vertex data (no UVs: the track rendered flat green).
- **Lunar theme** (`Theme.Lunar`, NOT the old asset-pack `Theme.Moon`; BrawlMapBuilderLunar.cs, props in
  Tools/IceArt/props15.py, floor `lunar_tex.py` -> RegolithGround.png): a 1960s-70s Space Age moon base, original
  retro designs (no copies of real or fictional craft). `#` one family per wall: MoonBoulderA/B, CargoStack (canister
  rack), FuelTanks; `x` MoonBuggy / SupplyPods / RockSampler, `f` RockRidge, `t` RadioDish / MiniRocket / FloodMast /
  Beacon, `b` StarGrass (alien fronds, frost ramp), decor SmallCrater / MoonPebbles / Footprints, floor `MoonFloor`.
  Border CraterWallA/B/C (crater rim; B with an outpost dome, C with a landing pad; grounded, not stretched),
  `AirlockGate`; backdrop MoonDome, Lander, LaunchRocket, `Earth` (one, high on the left side). Sky: `StarSky.mat`
  (BrawlFortress/StarSky, procedural stars), dark-blue fog; ambient cool neutral (`LunarLook`), desert materials.
  Moon rock uses `ash` / `metal` / `stone` (the stone_* cells read bluish). Maps, one per mode: CraterBase (Comp, new layout), MoonRockRaid (CTF), TranquilityHill
  (KOTH), LunarExpress (Payload), OrbitalWorks (MvM), StardustArena (Boss, Hale), DarkSideCrater (Zombie). Menu
  `Tools/Maps/Build Lunar Maps` (2 maps per MCP call).
- **Ocean theme** (`Theme.Ocean`, BrawlMapBuilderOcean.cs + OceanThemeSetup.cs, props in Tools/IceArt/props19.py,
  floor `ocean_tex.py` -> SeabedGround.png): the sea floor and a sunken city (original Atlantis-like ruins of pale jade
  stone, gold bands, glowing runes / orbs / crystals), an old wooden wreck. `#` one family per wall: CoralReefA/B,
  AtlantisWall, ShipHull, BarnacleRock; `x` GiantClam / TreasureChest / AnchorRock / DivingHelmet (brass) / CannonWreck,
  `f` CoralRidge, `t` AtlantisColumn / SeaFan / TridentStatue (original sea-king merman) / GlowCrystal / BrokenMast,
  `b` KelpGrass (papyrus ramp) swaying (`KelpSway.cs`, one driver object; kelp clumps made not static), decor Starfish
  / SeaShells / SeaUrchins / CoralBits. Border ReefWallA/B/C (low reef banks, B with a wreck's bow, C with a ruined
  arch; collider 6 m), `AtlantisGate`; the 100 m sea floor goes on beyond (no lake), backdrop AtlantisTemple (domed
  rotunda, off each end), SunkenShip, GiantScallop. Glow slot = `OceanGlow.mat`. `Caustics` quad (BrawlFortress/Caustics,
  procedural additive light web) over the arena. Lighting `OceanLook` / `OceanWater` (IceProps -> IcePropsOcean) /
  `OceanSky`, blue-green fog. **Currents** (`Assets/Scripts/Map/OceanCurrent.cs`, placed by `PlaceCurrents`): per half
  the 2 best straight runs of open tiles along z (2 tiles wide, >= 5 long, rows 3-12, >= 5 m apart, clear of a path),
  mirrored, all flowing to the middle; x(1 + 0.35 cos) of the angle between the move and the flow (BrawlFortress/Current:
  chevrons; the quad's local Y is the flow). **Not yet tested in a real match.** Maps, one per mode, all new layouts (each with
  4 lanes, checked before building with a copy of the lane picker): CoralKingdom (Comp), PearlHeist (CTF), TridentCrown
  (KOTH), TidalExpress (Payload, own path (0,16.5)/(0,10)/(3,4)), AbyssWorks (MvM, path E), AtlantisArena (Boss, Hale),
  GhostReef (Zombie). Menu `Tools/Maps/Build Ocean Maps` (2 maps per MCP call). The MvM base scene has a red spawn at
  (0, -15.4): keep tile (row 3, col 10) free on MvM layouts.
- **Savanna theme** (`Theme.Savanna`, BrawlMapBuilderSavanna.cs + SavannaThemeSetup.cs, props in Tools/IceArt/props20.py,
  floor `savanna_tex.py` -> SavannaGround.png): wild Africa, a 1960s-70s game reserve with a safari camp (original designs).
  `#` one family per wall: KopjeRockA (balanced granite boulders) / KopjeRockB (split boulder), ThornBoma (palisade of
  stakes, thorn brush heaped against it), TermiteMound (red earth, fused spires), SafariStack (expedition crates, tarp,
  jerry cans, hurricane lantern); `x` SavannaBoulder / DeadLog / ElephantSkull / WaterBarrels / SafariCrate (pith
  helmet), `f` ThornHedge, `t` BaobabTree / AcaciaTree (umbrella) / TermiteSpire / Euphorbia (candelabra; the first
  try read as an American saguaro), `b` SavannaGrass (sand ramp, golden), decor StrawTuft (grass clump at 0.32 x) /
  AloePlant / HornSkull / SavannaStones, PondReeds at the water. Border SavannaRidgeA/B/C (low red-earth banks crowned
  with boulders; B with a baobab, C with an acacia and a ranger's lookout; grounded, collider 6 m), `SafariGate`
  (granite pillars, log lintel, carved elephant board, lanterns); the 100 m floor goes on beyond, backdrop BaobabGiant,
  Giraffe (Voronoi coat), Elephant, SafariCamp (ridge tents, an original boxy expedition truck), acacias / kopjes.
  **Baobab** (the theme's landmark; Tho wanted it like the Avenue of the Baobabs, pinkish-brown bark, not low-poly):
  `baobab()` in props20.py: a smooth 28-sided column (shallow grooves, flared foot), a crown of curved Bezier branches
  forking at the ends, lumpy foliage clumps on the twig ends (branches show between them), flat top. Bark faces go to
  slot 1 on the sand ramp and slot 1 is `BaobabBark.mat` (props material tinted, `SavannaThemeSetup.Bark`, like the
  candy pink) for BaobabTree, BaobabGiant, SavannaRidgeB (`SavannaThemeSetup.BarkProps`); Blender preview: `bark_preview`.
  Smooth look lessons: colour organic shapes per vertex on a ramp (the foliage clumps: papyrus ramp by height), not per
  face by facing (that gave a triangle mosaic that read as low-poly); noise lumps, not per-vertex random jitter; a tube
  needs >= 8 sides or its edges pass the 50 deg smoothing angle (`SMOOTH_ANGLE`) and show as facets. BaobabTree ~6.6k tris.
  Lighting = Egypt's (`EgyptLook` + `EgyptSun`) + `SavannaSky` (SkyGradient, warm pale horizon).
  **Water holes** (`Assets/Scripts/Map/WaterHole.cs`, placed by `PlaceWaterHoles`, BrawlFortress/Pond: depth from vertex
  alpha, drop rings, wet mud edge, twinkling pale-green "+" glints = heals): 2 per half on open floor (+1 in the middle on
  Competitive), mirrored, radius 1.35-2 m; heal `healPerSecond` 0.06 x max HP (ticks of 0.5 s through
  `HealthBase.Buff`, by the character's owner, only while `gameState == Started`); not bosses, not MvM robots.
  **Not yet tested in a real match.** Holes stay 4 m off a payload / robot path (a hole is up to 2 m wide). Maps, one per
  mode, all new layouts (checked before building with a copy of the hole picker: two clear 3x3 pockets per half):
  BaobabGrove (Comp), SafariRaid (CTF), TermiteHill (KOTH), SavannaExpress (Payload, path B), RangerStation (MvM,
  path A), LionsDen (Boss, Hale), BonePlains (Zombie). Menu `Tools/Maps/Build Savanna Maps` (2 maps per MCP call).
- **Japan theme** (`Theme.Japan`, NOT the old asset-pack `Theme.Bamboo`; BrawlMapBuilderJapan.cs + JapanThemeSetup.cs,
  props in Tools/IceArt/props21.py, floor `japan_tex.py` -> ShrineGround.png): a 1960s-70s Shinto shrine and temple
  garden in spring. **The landmark is the torii** (Tho's ask): `ToriiGate` (myojin torii: smooth vermilion pillars on
  black sleeves and stone footings, nuki with wedges, vermilion shimaki and black kasagi curving up at the ends
  (`sweep_box` along `curved()`), strut and a black plaque in a gold frame (no lettering), a shimenawa rope with shide,
  big stone lanterns) behind each spawn, and a smaller detailed `ToriiSmall` at the start of each `PathSlab` stone
  path in the arena: one per path end, **never in rows** (Tho rejected a senbon-torii tunnel: clutter from the top-down
  camera). `#` one family per wall: IshigakiStones, BambooFenceWall, SakeBarrels,
  PlasterWall (namako tiles, grey tile roof: `hip_roof`), MossRock; `x` StoneLanternLow / WaterBasin / OfferingBox /
  TaruStack, `f` BambooFence, `t` SakuraTree / BlackPine / KasugaLantern / Komainu (original chunky guardian), `b`
  BambooGrass, decor SteppingStones / MossTuft / PetalScatter / ZenStones. Border ShrineWallA/B/C (earthen walls
  under tile roofs on a stone base; B with a cherry tree, C with a belfry; grounded, collider 6 m); backdrop Pagoda,
  ShrineHall (with a torii before it), SnowPeak far off each end, cherry trees and pines around.
  **Pink**: blossom faces sit on the sand ramp in slot 1 = `SakuraPink.mat` (`JapanThemeSetup.Pink`, like the baobab
  bark) for SakuraTree, PetalScatter, ShrineWallB (`JapanThemeSetup.PinkProps`); Blender preview `props21.pink_preview`.
  Lighting: `GreekLook` + `GreekSun` + `JapanSky` (soft blue, pale pink-gold horizon).
  **Torii paths** (`Assets/Scripts/Map/ToriiPath.cs`, placed by `PlaceToriiPaths`): per half the 2 best straight open
  runs along z (2 tiles wide, >= 5 long, rows 3-12, no bushes, >= 5 m apart, clear of a path), mirrored; a run that
  reaches the middle row over open tiles goes on to z = 0, so mirrored runs join into one path (slabs every 0.66 m, nearly
  touching: wider gaps read as a ladder from above; laid out from the middle outwards so they line up; a torii at the outer end, and at the inner end when a run does not join). Speed x1.3 both ways through
  `TerrainSpeed.FactorAt` (player and NavMesh agents); `ToriiSmall` collides on its two pillars only.
  A path joins the other half's only when its mirror partner (a path at -x in the same half) exists, the middle
  tiles are open and the longer box clears a payload / robot path; otherwise it ends before the middle with a torii
  at both ends. **Not yet tested in a real match.** Maps, one per mode, all new layouts (checked before building with
  a copy of the path picker): SakuraShrine (Comp), ShrineHeist (CTF), PagodaHill (KOTH), SakuraExpress (Payload,
  path C), LanternWorks (MvM, path D, half paths), KomainuCourt (Boss, Hale), HauntedTemple (Zombie, half paths).
  Menu `Tools/Maps/Build Japan Maps` (2-3 maps per MCP call).
  Builder gotcha: tiles are moved after they are made, so their colliders sit at the origin until
  `Physics.SyncTransforms()`: a placement pass that tests the physics scene (CheckBox / CheckSphere) must sync first
  (PlaceBogPuddles never got its middle puddle for this reason).
- **Candy theme** (`Theme.Candy`, BrawlMapBuilderCandy.cs + CandyThemeSetup.cs, props in Tools/IceArt/props18.py,
  textures `candy_tex.py` -> CandyGround.png / ChocoFlow.png): a candy kingdom (1960s-70s sweet shop spirit). `#` one
  family per wall: WaferBlock, CakeBlock, ChocoBlock, GumdropStack, MarshmallowStack; `x` Cupcake / GumballMachine /
  WrappedSweets / DonutStack, `f` StickFence (peppermint sticks), `t` LollipopA (pink) / LollipopB (rainbow) /
  IceCreamCone / GumdropTree / GummyBear, `b` SugarGrass, decor Sprinkles / WrappedCandy / ChocoChips. Border
  CandyWallA/B/C (low layer-cake banks, B with a gingerbread cottage, C with lollipops; collider 6 m) with `ChocoLake`
  quads behind (BrawlFortress/Lava + ChocoFlow), `CandyGate`; backdrop CandyCastle, GingerbreadHouse, LayerCake.
  **Pink**: the palette is full and has no pink, so candy props put pink faces in material slot 1 (`CKit`: colours may
  be `(cell, True)`; `PINK` = snow) and that slot is `CandyPink.mat` = the lit props material tinted (_Color 0.92 / 0.48
  / 0.68), not a glow (`CandyThemeSetup.IsCandy`). SugarGrass = frost ramp leaves in the pink slot (purple -> pink).
  Lighting: `GreekLook` + `GreekSun` + `CandySky` (pastel SkyGradient). Maps, one per mode, all new layouts:
  CandyPlaza (Comp), CookieHeist (CTF), FrostingHill (KOTH), CaramelExpress (Payload, path A), ToffeeFactory (MvM,
  path D), GumdropArena (Boss, Hale), RottenSweets (Zombie). Menu `Tools/Maps/Build Candy Maps` (2 maps per MCP call).
- **Submesh order gotcha**: Unity numbers an FBX's submeshes in the order their materials first appear in the face
  list, not by Blender slot. A prop whose first face was a glow face got its two materials swapped (Volcano
  BasaltBouldersA/B, CooledFlow, LavaBombs, LavaRock had rock and lava swapped until Nov 2026; the candy cake banks).
  `Kit.obj` (props8.py) now sorts glow faces last; check with a Blender re-import (first polygon's material_index == 0).
- **Greek theme** (`Theme.Greek`, BrawlMapBuilderGreek.cs + GreekThemeSetup.cs, props in Tools/IceArt/props17.py,
  textures `greek_tex.py` -> GreekGround.png / SeaFlow.png): a Mediterranean acropolis by the sea. `#` one family per
  wall: MarbleBlocksA/B, BrokenColumns, WhiteWall (whitewashed house, blue door), StoneWall (dry stone, vine); `x`
  FallenDrums / ColumnCapital / Pithoi / OlivePress, `f` MarbleLow, `t` DoricColumn / MarbleStatue (original headless
  robed figure) / OliveTree / GreekCypress / BronzeBrazier, `b` MaquisGrass (papyrus ramp: the palette has no purple ramp,
  frost_grad lavender read as ice), decor MarbleRubble / PotteryShards / GeraniumPot. Border GreekCliffA/B/C (low
  limestone cliffs: ruined colonnade / blue-domed chapel / olives and cypress; grounded, collider 6 m) with `SeaWater`
  quads behind (BrawlFortress/Lava + SeaFlow), `GreekGate` (propylon); backdrop GreekTemple (original Doric temple on its
  rock, off each end), WhiteVillage, Windmill (on islets), Caique boats. Marble colour by facing + height (darker
  weathered foot), never by the light. Lighting: `GreekLook` (softer than EgyptLook: Tho found the desert light
  glaring on marble / whitewash) + `GreekSun` swapping IceProps -> `IcePropsGreek` (_Color 0.98, dim 0.68: 0.86 was too dark, the softer ambient did most of it) + `GreekSky`
  (SkyGradient, warm horizon); GreekGround _Color 0.98, sea _Boost 0.9. Maps, one per mode, all new layouts: MarbleAgora (Comp), AmphoraHeist
  (CTF), OracleHill (KOTH), HarborLine (Payload, path E), BronzeForge (MvM, path B), TitanTerrace (Boss, Hale),
  NecropolisSteps (Zombie). Menu `Tools/Maps/Build Greek Maps` (2 maps per MCP call). New Payload/MvM layouts: check
  the build log's `skipped=` (tiles cut by the path); BronzeForge was redrawn around its path (Catmull-Rom of
  pathHalf + its point mirror, 1.7 m clear) until skipped=0.
- **Swamp theme** (`Theme.Swamp`, BrawlMapBuilderSwamp.cs + SwampThemeSetup.cs, props in Tools/IceArt/props16.py,
  textures `swamp_tex.py` -> SwampGround.png / AcidFlow.png / BogMud.png): a toxic bog and glowing mushroom wood at dusk,
  1960s-70s backwoods. `#` one family per wall: MossBoulderA/B, ShroomCluster, RootTangle, LogStack; `x` Stump / RowBoat /
  RustyDrums / MoonshineStill, `f` FallenLog, `t` GiantShroomA (blue) / GiantShroomB (purple) / SwampCypress /
  SwampLantern, `b` BogGrass (reeds, papyrus ramp), decor ShroomPatch / MossClump / Cattails / SwampStones. Border
  SwampBankA/B/C (low mossy banks, B with a stilt shack, C with a cypress; grounded, not stretched, collider 6 m) with
  `AcidLake` quads behind (BrawlFortress/Lava + AcidFlow), `SwampGate`; backdrop BigCypress, StiltShack, giant mushrooms.
  Props kit `SKit` (per-vertex gradient faces: glowing frost_grad / ice_grad caps). Swamp props' glow slot =
  `SwampGlow.mat` (LavaGlow shader, slow breathing). After the mode objects, `SwampTwilight` swaps IceProps ->
  `IcePropsSwamp` and adds GlowPool (cyan) / WarmPool (lanterns) quads; sky `SwampSky.mat` (SkyGradient), green fog.
  **Bog puddles** (`Assets/Scripts/Map/BogPuddle.cs` + `MudSlowAgent.cs`, placed by `PlaceBogPuddles`): 2 per half on
  open floor (+1 in the middle on Competitive when free), mirrored, radius 1.35-2.1 m, slow x0.6. Applied through
  `TerrainSpeed.FactorAt(pos, moveDir)` (TerrainSpeedAgent.cs; also the Ocean currents): the local player in
  PlayerFPSController, NavMesh agents via a runtime `TerrainSpeedAgent` (keeps other scripts' agent.speed as the base). No network message. Mesh made in SwampThemeSetup (vertex alpha = wet edge band, BrawlFortress/Bog shader).
  **Not yet tested in a real match.** Maps, one per mode, all new layouts: GlowshroomBog (Comp),
  ToadstoolRaid (CTF), MireCrown (KOTH), BayouExpress (Payload, path D), SwampMill (MvM, path C), RotwoodArena (Boss,
  Hale), GhoulBayou (Zombie). Menu `Tools/Maps/Build Swamp Maps` (2 maps per MCP call).
- **Circus theme** (`Theme.Circus`, BrawlMapBuilderCircus.cs, props in Tools/IceArt/props14.py, floor `circus_tex.py`
  -> FairGround.png): a 1960s-70s travelling circus and funfair on a summer field. `#` one family per wall: show wagons
  CircusWagonA (red/gold) / B (blue/yellow) along the wall, TrunkStack, HayStack; `x` StarTubs / PopcornCart /
  CircusCannon, `f` RopeBarrier, `t` HighStriker / TicketBooth / CarnivalLamp / BalloonPost, `b` FairGrass (papyrus
  ramp), decor HayPile / JugglingPins / PopcornSpill, floor `FairFloor`. Border CircusWallA/B/C (striped canvas sidewall
  of the big top with poles, pennants, C9 bulb string; B with sideshow banners, C with a wagon; grounded, not stretched),
  `CircusGate` (striped towers, arched board with glowing bulbs, star); backdrop BigTop, FerrisWheel, Carousel.
  Lighting = Egypt's. Maps, one per mode: CircusRing (Comp, new layout), BigTopRaid (CTF), RingmasterHill (KOTH),
  CircusTrain (Payload), ClockworkFair (MvM), StrongmanArena (Boss, Hale), HauntedFair (Zombie). Factory: one map only
  (Tho's call).
  Blender previews draw back faces, Unity culls them: a flat sheet facing away showed fine in the preview and was
  invisible in the game (the striped sidewall). Check winding / give thin sheets their own back faces.
- **Factory theme** (`Theme.Factory`, BrawlMapBuilderFactory.cs, props in Tools/IceArt/props13.py, floor `factory_tex.py`
  -> YardGround.png, same folder/palette): a 1960s-70s engineering works / building site. `#` one family per wall
  (`PrepareVolcanoWalls`): BrickWallA/B (0.6 m thick, stretched 2.1 x in depth, long face along the wall), CrateStack,
  GirderStack (red-oxide), PipeStack (concrete); `x` OilDrums / CableReel / CementMixer / BrickPallet, `f` IronFence
  (corrugated iron), `t` GirderColumn / Scaffold / FactoryLamp / Boiler, `b` YardGrass (papyrus ramp), decor
  BrickRubble / ScrapPile / Cogwheel / PlankBundle / Wheelbarrow, floor `YardFloor` (packed earth, cart ruts, broken
  patches of granite setts; the first try, clean concrete slabs, read as modern paving: Tho rejected it). Border FactoryWallA/B/C (brick
  workshop with windows / corrugated shed / brick wall with pipes and a tank; grounded and stretched like SandCliff),
  `FactoryGate` (brick piers, riveted girder, open iron gates); backdrop TowerCrane, Chimney (no smoke), WaterTower,
  FactoryShed. Lighting = Egypt's (`EgyptLook` + `EgyptSun` desert materials). Test map: `Map_Brawl_SteelYard` (Comp,
  new layout). Brick and large single-colour surfaces must not bake a dark "back" colour (`brick()`: same red on every
  side): pieces get turned any way and the dark side showed as a brown block.
- **Volcano theme** (`Theme.Volcano`, BrawlMapBuilderVolcano.cs, props in Tools/IceArt/props11.py + props12.py,
  textures `volcano_tex.py` -> BasaltGround.png / LavaFlow.png, same folder/palette): a 1960s lava island.
  `#` one rock family per wall (`PrepareVolcanoWalls`: touching '#' tiles share it, mirrored walls too): basalt columns
  BasaltBlockA/B/C, BasaltBouldersA/B, platy BasaltSlabsA/B (glowing seams), ObsidianWall, red TuffRockA/B.
  `x` LavaRock / ObsidianChunk / SurveyCrates / LavaBombs / MineCart, `f` BasaltLow, `t` ObsidianSpire / FireCrystal /
  CharredTree / MagmaPillar / BurntPine, `b` ScorchGrassA/B (burnt grass, palette ramp `char_grad`: black leaves, ember
  tips), decor LavaCrack / PumiceRocks / EmberTuft (the grass clump at 0.33 x, no "grass" in its name so not a bush) /
  CooledFlow, floor `BasaltFloor`. SurveyCrates = crate, drum seismograph, olive army-surplus oil drum.
  **Tho: no smoke / cloud / dust-cloud objects** (the sulfur-smoke bush and the volcano's smoke plume were removed).
  Border: low `BasaltRimA/B/C` (~4 m, grounded, not stretched; collider 6 m tall) with `LavaLake` quads all round
  (they start at the outermost rim's centre line, ends behind the gates at |z| > 24.2), `VolcanoGate`, far `Volcano`
  cones at z +-80, gradient sky `VolcanoSky.mat` (BrawlFortress/SkyGradient: the map scenes have no camera, the sky
  must be the skybox). Glowing faces (palette `lava_grad`) use `VolcanoGlow.mat` (BrawlFortress/LavaGlow, unlit,
  two-sided, shimmer) instead of XmasGlow; lava uses BrawlFortress/Lava (world-UV, two scrolling layers).
  `VolcanoThemeSetup` (called from IceThemeSetup.Setup) makes those materials, the floor, LavaLake and EruptionVent prefabs.
  Maps: MagmaRift (Comp), ObsidianKeep (CTF), CalderaCrown (KOTH), LavaLine (Payload), CinderWorks (MvM), EmberThrone
  (Boss, Hale), AshenHollow (Zombie). Menu `Tools/Maps/Build Volcano Maps` (2-3 maps per MCP call).
  - **Eruption vents** (`Assets/Scripts/Map/EruptionVent.cs`, placed by `PlaceVents`): 2 per half on open floor (no bush
    neighbour, <= 2 solid neighbours, >= 5 m from spawns, 4.5 m from the middle, 3 m from a payload path), mirrored with
    the same phase; the two pairs alternate (period 10 s, warning 2.2 s: cracks brighten + red disc fills; eruption
    1.3 s: lava fountain + embers). Clock = PhotonNetwork.Time (no RPC). Damage 35% max HP once per eruption, dealt by
    the owner of each character (photonView.IsMine), only while `gameState == Started`, bosses immune, ownerId -1 like
    the DeathWall. Vents are not static, not on the wall layer, and ignored by the NavMesh bake (as are the lava lakes).
    **Not yet tested in a real match** (damage, kill feed for ownerId -1, bots walking into vents: bots do not avoid them).
- MCP gotcha: `AssetDatabase.Refresh()` does not report compile errors; if a builder change "does nothing", check the
  console for CS errors (Unity keeps running the last good assembly). Generated textures are written only when changed
  (`IceThemeSetup.WriteIfChanged`, `common.write_palette`): Unity maps imported textures and rewriting them fails (1224).

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
