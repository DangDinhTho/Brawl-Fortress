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
`show_mrec_matching`, `show_inter_back_home` (bool, default true; name kept for old builds: since Nov 2026 it switches the
  end-of-match interstitial shown when the player closes the reward popup (`PopupReward`, Claim) with a plain claim; no
  interstitial after the x2 rewarded ad, none on the Home buttons of the scoreboard / MedalScene any more; the
  scoreboard no longer leaves on its own: the 30 s auto-quit `PopupScoreBoard.WaitAutoQuickGame` was removed, Nov 2026,
  the player taps Home),
  `show_inter_die`, `cart_payload_speed_scale` (number, default 1:
  multiplies the Payload cart speed on every map after it is fitted to the track), `bush_hp` (number, default 250:
  health of one grass clump; explosions / fire tear it at 0; rockets deal 80-100), `free_map` (default missing / null:
  a list of map enum names that become free on top of the default free maps, e.g. `["Map_Brawl_JingleSquare",
  "Map_Brawl_LanternEve"]` for Christmas; a JSON array or names separated by commas; unknown names are skipped; old
  builds do not read this key, so it may list new map ids; free maps show first in PopupSelectMap), `hidden_map` (same
  format as `free_map`, default missing: maps taken out of the game, e.g. one that crashes until a fixed build is out;
  `ConfigManager.hiddenMaps` / `IsHidden` / `IsPlayable`: PopupSelectMap drops their cards (and a selection on one moves
  to the mode's first visible map), `FirstMapOf` skips them, a room on one cannot be joined and a guest leaves when the
  host picks one (`UIRoomItem.UnavailableReason`, message `room_map_hidden`), a solo match that still asks for one plays
  the mode's first map (`ArenaManager.WaitLoadMap`, OfflineMode only). Old builds do not read it. Checked in Play mode
  with Map_Brawl_Payload + Map_Brawl_MoonRail hidden).
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
  (1.3; at 1.0 the HP number was unreadable from the match camera, Tho found 1.55 a little too big).
- `BrawlHudKit`: runtime rounded-rect sprites (drawn at 4x, `pixelsPerUnitMultiplier` 4), HUD text style (Lilita One,
  thick dark outline + drop shadow) through ONE shared material per font + outline (`HudMaterial`; do not use TMP's
  `outlineWidth` setter or `fontMaterial` for HUD text: they make a material per text, and a material instanced from
  another font keeps that font's atlas = garbled glyphs).
- Damage numbers (UltimateTextDamage, `TextDamageItem_default_stack_TMP`): restyled per pooled item in
  `UltimateTextDamageManager.AllocateOneInstance` -> `BrawlHudKit.StyleDamageNumber` (Lilita One, red vertical
  gradient, thick outline, 1.55x size).
- FootRing (Nov 2026, Tho: like Brawl Stars): a translucent team disc, strongest at its edge and fading towards the
  centre (`FillAlpha` 0.6 at the edge, `CenterFill` 0.15, `Falloff` 1.4) with a thin light rim (`Width` 0.05 m); NO dark
  outline. Radius = the hit collider's radius (players: CharacterController 0.30, bots / robots / zombies: CapsuleCollider
  0.35) x `RadiusScale` 1.9 (1.45 = Soldier 0.44 m was too small), so every character's circle is about the same size (the model's bounds made them very
  different); bosses keep the model measure. Drawn 0.07 m above the feet (`HeightOffset`: the bog / pond surfaces are at
  0.048 and hid it) in queue Transparent-4 (after the Ocean currents). The Engineer buildings' circle and the sentry's
  range ring (BuildableMarker) use the same shader. Colours come from OverheadBarStyle so ring and bar match; the reload
  arc is off (`ShowReloadArc`): the ammo bar shows the reload.
- **Character effects** (Nov 2026): `CharacterFx` on every character (added next to FootRing) + `BrawlFx` (one shared
  world-space ParticleSystem per kind, fed with Emit; shapes drawn by the shader BrawlFortress/FxParticle through
  Resources/Materials/FxPuff / FxPlus / FxSpark): a dust puff behind the feet every 1.05 m walked on the ground (none in a
  bush or in the air); in a Swamp bog puddle mud drops + bubbles; in a Savanna water hole water drops and green "+" when
  it heals; speed lines on a Japan torii path or with an Ocean current, bubbles against it; on a hit (HealthBase.
  OnTakeDamage, every client) the model flashes white (MaterialPropertyBlock `_Color` / `_ColorDim` x2.6 for 0.12 s,
  renderers that already have a block are left alone: the Spy cloak) and sparks fly. The movement is read from the
  position, so bots and remote players get it too, with no network message. Nothing shows for a character hidden from
  the viewer (`BushStealth.Hidden`, cloaked Spy) or dead. Checked in a solo match on GlowshroomBog (dust, mud, bubbles,
  flash, "+"; speed lines and sparks triggered by hand); torii paths, currents and water holes not walked in a match.
- Checked in a solo match (camera renders, Wild West): bars / names / ammo readable at 1280x720. The overlay damage numbers could not be screenshotted from the editor (styling checked on the live
  items: Lilita One, HUD material, gradient). Preview image: Assets/Screenshots/hud_brawl_style.png.

## Match camera (Brawl Stars style, Nov 2026)
- `TopDownCam` in Arena.unity (CinemachineVirtualCamera + FramingTransposer, Follow = the character), set up by
  `CameraController.InitCam`: `pitch` 58 (was 45), `fieldOfView` 28, `cameraDistance` 27.06, `screenY` 0.532
  (FramingTransposer Screen Y, 0 top / 1 bottom; tracked offset y 1). Tho asked for FOV 28 with the same view as FOV 38 at
  19 m: distance and screen Y were solved so the ground in view is unchanged along the screen's centre line (11.06 m
  ahead of the character, 6.12 m behind, same ground area); the width at the character is 30.8 m instead of 30.1 at
  19.5:9 (the near edge a little wider, the far edge a little narrower: less perspective). Blue team: yaw 180.
  The camera is ~28 m from the character (was ~20), so `PlaceListener` moves the AudioListener off FreeCamera onto a child
  on the view line, `listenerDistance` 19 m from the character: the 3D sounds fade linearly to 0 at 35-60 m (custom
  rolloff), at 28 m they were ~27% quieter. Shadow distance on quality High is 40 m: the far top corners are now ~39 m
  from the camera (were ~33), so shadows fade out a little there on High (mobile default Medium has no shadows).
- `ArenaCameraClamp` (a CinemachineExtension added to it at runtime; Assets/Scripts/GameManager/ArenaCameraClamp.cs) stops
  the camera at the arena's edges instead of always centring the character: after the Body stage it shifts the camera so
  the view's ground footprint stays inside the arena + `endMargin` 3 m (bottom of the screen, own base), `topMargin` 8 m
  (top, enemy base: the view reaches ~11 m ahead and ~6 m behind, with 2 m a character at the enemy base sat under the
  top HUD; Tho then widened all margins) and `sideMargin` 5 m. `Follow` stays the character (death / respawn / spectate code compares it).
- `ArenaBounds`: the arena rectangle, measured once per match: end VitualWalls when present, else the farthest first hit
  of rays at 1 m from the spawns' centre (up to 32 m), never narrower than the spawns + a margin. Generated maps measure
  about x -18.5..17.8, z -22.2..22.1. (A percentile of the hits picked the inside obstacles: 11 m.)
- The steeper camera looks at the spawns / bomb holes from higher up, so the end-gate check in BrawlMapBuilderGates
  (built for the old 45 deg camera, CamUp 10.4 / CamBack 10.6) is on the safe side.
- Checked in a solo match on SunTemple: spawn, middle, side edge, enemy end. Not tried on a phone or in multiplayer.

## Destructible bushes (Brawl Stars style, Nov 2026)
- Each box of a `BushArea` is one grass clump. Its health `BushDamage.ClumpHp` = `ConfigManager.bushHp`, Title Data key
  `bush_hp` (number, default **250**; Tho: at 80 HP one rocket tore a bush, he wants at least 3). Rockets deal 80-100
  (`weapon_stats` RocketLauncher): 250 = 3 rockets (4 when three low rolls sum under 250, ~2%). A rocket blast deals the
  shot's damage (no falloff) to every clump within 1.8 + 0.45 m. With 250: grenades (70-85) take 3-4, sticky bombs
  (103-138) 2-3. Explosions (every `ExplosionDamage`:
  grenades, sticky bombs, sentry rockets...; rockets where they blow up: `BulletNormal.BushBlastRadius`, Rocket 1.8 m)
  and fire (`FlameDamage`, `FireFactor` 0.6 x ClumpHp / 80 of the flame's damage every 0.2 s over its trigger's bounds:
  fire burns as fast as at 80 HP) wear the clumps they reach down. At 0: `BushArea.Shatter` = leaves burst out (`BrawlFx.Leaves`, leaf shape 3 of FxParticle,
  Resources/Materials/FxLeaf), the clump renderer is hidden, `BushStubble` leaves 2-3 tufts of stalks cut off low (8-11
  stalks packed in an 11 cm radius per tuft, taller in the middle, leaning a little outwards; 5-sided prisms 8-22 cm,
  lighter flat cut tops, drawn with the clump's own material at the palette uvs of its leaf roots. Tho rejected the clump
  mesh squashed flat (a cabbage) and single stalks spread evenly over the clump: he wants tufts),
  and `BushArea.Contains` skips the clump (no more hiding there: BushStealth, bots' FieldOfView, the grass fade).
- Any hit on a bush shakes leaves out (local only): a blast too weak to tear it, a bullet flying into it
  (`BulletNormal.Move`), someone hiding in it being hit (`CharacterFx.OnHit`).
- Sync: projectiles fly on every client but only the shot's owner (`DamageObject.isOwner`: the shooter, the master for
  bots) counts the damage, tears the clumps down at once and sends them to the others: Photon event 41 (`BushEventCode`),
  int[] {bush sibling index under "Bushes", clump index, ...}, Receivers Others (not sent offline). A player who joins
  later does not get the bushes already torn.
- The map grass is statically batched at runtime (its MeshFilter holds the scene's combined mesh): the stubble uvs and
  the leaf colour come from `Resources/BushLeafColors.asset` by the clump's object name (`BushLeafColors.GrassName`:
  "PapyrusGrass", "Grass02 (52)" -> "Grass02"), baked by `Tools/Bush/Bake Leaf Colours` (Assets/Editor/BushLeafColorBaker.cs:
  the palette colour of the upper leaves; old themes read the World atlas). Run it after adding a grass model. The leaf
  colour is tinted by the clump material's `_Color` at runtime (night / desert / Egypt materials).
- Checked in a solo match on SunTemple: a scripted blast, a real rocket from the pool and the bots' own rockets / grenades
  tore clumps, leaves burst, stubble stayed. Fire and multiplayer sync not tried. At 240 HP real rockets from the pool
  (97, 81, 85 damage) left the bush whole after 2 and tore 8 clumps with the 3rd; 250 (now) not re-run in a match.

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
  Map_Brawl_CTF = Moon, ZombieYard = Cave. The hand-made Cave Payload map `Map_Brawl_Payload` crashed on a phone (cause
  not found, no device log) and is retired since 27 Nov 2026; `Map_Brawl_CrystalRail` "Crystal Rail" (generated, Cave,
  `PathF` S-bend track of 54 m along the left side, across, back across the middle; new layout, seed 3606) replaces it:
  first Payload card, free, first in `DefaultTicketCosts`; checked in a solo match (loads, bots pushed the cart past the
  middle, no errors), not on a phone. 24 older maps that repeated a theme are **retired** too
  (the 5 Mix maps FrontierRuins, BorderSiege, CrossroadRuins, FrontierConvoy, LastStand since Nov 2026): listed in
  `BrawlMapBuilder.Retired`, cards removed from `PopupSelectMap` (menu `Tools/Maps/Remove Retired Map Cards`; AddCard
  never re-adds them). Their `Map` enum values stay (the enum is sent as a byte; all clients share Photon AppVersion
  1.0). Since Nov 2026 their scenes are **out of the build** too (Tho: the build had grown a lot; menu `Tools/Maps/Remove
  Retired Maps From Build`, `AddBuildSettings` skips them; the scene files stay in the project): a room an older version
  opens on one cannot be joined (`UIRoomItem.JoinRoomClick` / `PopupInsideRoom.OnChangeMap` check
  `UIRoomItem.MapInBuild`, message `room_map_unavailable`, the guest leaves); anything else that still asks for one
  plays the mode's first map (`ArenaManager.WaitLoadMap`). Build list: 117 maps + FlashScene, Home, Arena, MedalScene.

## Build size: static batching at map load (Nov 2026)
- Every prop of the generated maps is Batching Static (`SetStaticEditorFlags(int.MaxValue)`). Build-time static batching
  stored a combined copy of every prop instance per map: 5-36 MB per map uncompressed (Japan maps ~33 MB, old themes
  5-15), ~1.9 GB for 139 maps, while the props' own meshes are shared by a theme's maps. That was the bulk of the build
  growth. Now: Player Settings static batching is **off for Android and iPhone** (Standalone keeps it), and
  `RuntimeStaticBatchProcessor` (Assets/Editor, IProcessSceneWithReport, also runs for scenes loaded in Play mode)
  clears Batching Static on each processed scene's static MeshRenderers and lists them on a new `RuntimeStaticBatch`
  object (Assets/Scripts/Map), which runs `StaticBatchingUtility.Combine` in Start and drops the combined meshes' CPU
  copy (`UploadMeshData(true)`). Same batches as before. PagodaHill (the heaviest, 646k static verts): 393 of 412
  renderers in 11 combined meshes, 11 ms in the editor (more on a phone, during the map load).
- Runtime combining needs **Read/Write enabled** meshes: the 328 prop FBX used by static renderers were switched on
  (Assets/Art/IceTheme + 8 in Brawl Arena/Models; the old Battle Arena models and the generated .asset meshes already
  were); `IceThemeSetup` sets `isReadable` on the theme FBX. In a player build the processor logs a
  `[RuntimeStaticBatch]` warning listing static meshes that are not readable (those props are drawn unbatched).
- Not measured with a real build yet (the editor log had no Brawl Fortress build report). Textures in the build are
  ~220 MB (estimate, ASTC 4x4), nothing unusual. Map_Brawl_Cavern's lightmaps (139 MB EXR) and Map_Brawl_Payload's
  LightingData (48 MB) are not referenced by their scenes: not in the build, only in the repo.
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

## Map icons and map info (Nov 2026)
- New-theme icons (Ice and later, 92 maps) come from `MapIconStudio` (Assets/Editor/MapBuilder), not the old fixed
  grading (that made desert / savanna yellow and dusk / lava / night maps dark): a "hero" shot from behind one end
  (camera height 19, back 23, looking at z 3.5, fov 46: the far gate and its landmark show at the top: torii, temple
  gate, big top, lava lake, Atlantis temple; little grey sky), cached raw in `Library/MapIconRaw/<icon>.png`, then
  graded per image, GENTLY (Tho: the first strong grading had heavy contrast and black spots, worst on ice / snow):
  white-point stretch only (`blackStretch` 0: a black-point stretch crushed shadows into black specks), gamma to a
  bright target (0.6+ for day maps), shadow lift + `floor` 0.05 (no pure black), contrast ~0.04, NO clarity (local
  contrast drew dark halos), mild saturation / vibrance, yellow-orange band control (desert, savanna), highlight
  shoulder (snow), bloom only on night / lava / glowing maps, vignette 0.05. The raw shot is the reference: a grade
  should only brighten and freshen it. Settings: `Assets/Editor/MapBuilder/MapIconGrades.json` = one
  entry per theme (`XmasNight` for night maps) + per-map overrides (`"set": "bloomThreshold=0.85 shoulder=0.6"`,
  `cam.height=18` for the camera). Edit the JSON and run `MapIconStudio.RegradeAll(theme)` / menu `Tools/Maps/Icons/
  Regrade New Theme Icons` (no scene opened, ~3.5 s per icon: one theme per MCP call); camera changes need
  `RenderRawBatch(start, count)` (opens the maps; ~25 per call). `BrawlMapBuilder.RenderIcon` uses the studio for new
  themes; the old themes keep the old formula. Preview grades into a scratch folder first (`GradeIcon(icon, g, path)`).
  Once Unity has loaded an icon its file stays memory-mapped and a normal write fails (1224); `WriteMapped` then
  overwrites it in place without truncating, the new PNG padded to the old length with an ancillary `paDd` chunk
  (decoders skip it; checked: Unity imports and shows them, pixels identical to the previews). Write with
  `RegradeAll(theme, null, false)` (one or two themes per MCP call: locked files are slow) + one `AssetDatabase.Refresh()`.
  (`EditorApplication.OpenProject` from a delayCall did not restart the editor.)
- Map info: each card in PopupSelectMap gets an "i" button (UIMapIcon.CreateInfoButton, top-left of the picture under
  the mode label, opposite the ticket badge; the Boss Battle boss badge sits under it; on top of the lock overlay; sprite `PopupSelectMap.infoIcon` = GUI PRO Kit Icon_ImageIcon_Info) opening
  `PopupMapInfo` (Resources/Popups, made from the ticket popup's frame; content built at runtime): picture in a frame
  of the theme colour, price badge (FREE / FREE TODAY for `free_map` / xN), name, mode, theme name + description, mode
  objective, features (Volcano vents, Swamp bog, Ocean currents, Savanna water holes, Japan torii paths, Xmas night,
  boss name, track for Payload / MvM, tall grass). Data: `MapInfo.cs` + generated `MapInfoTable.cs` (map -> theme;
  regenerate with `Tools/Maps/Generate Map Info Table` after adding maps; hand-made maps are listed in
  MapInfoTableGen; the Boss / Zombie maps of the old themes (`BrawlMapBuilder.BossZombie`) are not in `AllDefs()`, the
  generator adds them itself: without that a regenerate dropped ZombieYard, HaleJungle, RedRockPit...). Texts: Main.csv keys `theme_*`, `theme_desc_*`, `mode_title_*`, `mode_desc_*`, `feat_*`,
  `map_info_*`, `boss_name_*` (5 languages). A new theme needs its row in MapIconGrades.json, `theme_<Theme>` /
  `theme_desc_<Theme>` strings, a colour in `MapInfo.ThemeColor` and, if it has special terrain, a `feat_` line.

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
  collider = rock body only (not the rubble in front). `TempleGate` closes the gap behind each spawn where
  the base scene's Gate arch stood (Western keeps that Gate; the gap is walled by the invisible VitualWall; see End gates).
  **Lighting** (Tho: sunny, NOT yellow): Western scene lighting + `EgyptLook` (the Western ambient is dark grey and FlatKit adds it:
  bright near-neutral trilight ambient, light haze, sun) + `EgyptSun(true)` after the mode objects. FlatKit ignores the
  light colour (`_LightContribution 0`): the colour is the material's. **White sun** (Nov 2026: Tho still found Egypt too
  yellow): props get `IcePropsEgypt` (palette `IcePaletteEgypt.png` = IcePalette with the sand family (khaki, bone,
  sandstone, sand_grad cells) toned 30-40% towards grey, `_Color` 1.07/1.03/1.00), the floor `SandGroundDesert`
  (`_Color` 1.00/1.02/1.18: golden yellow -> warm cream; Egypt only). Tho then found the neutral white (props
  1.04/1.04/1.07, floor 0.97/1.03/1.28) too white and asked for a little orange: keep it subtle (1.09/1.03/0.96 with floor
  1.02/1.02/1.12 already reads golden again). Icon grade Egypt: `wb` 0.14, `yellowSat` 0.86 (stronger values cancel the warmth). Made by `IceThemeSetup.EgyptWhiteSun` (menu
  `Tools/Maps/Egypt/Make White Sun Materials`, also run by Setup Assets); existing scenes: `Tools/Maps/Egypt/Apply White
  Sun To Egypt Maps`. Factory / Circus / Lunar / Savanna keep `IcePropsDesert` (`EgyptSun()`).
  Ice and Xmas close that gap the same way (`BrawlMapBuilder.PlaceGates`): `IceGate` (glacier arch, natural ice) and
  `XmasGate` (timber arch, pine garland, C9 bulbs, wreath, lanterns), both in Tools/IceArt/props10.py. Maps: SunTemple, ScarabVault, ObeliskHill, NileExpress, DuneFoundry, PharaohsCourt (Hale), MummyValley.
- Unity keeps a scene / texture file memory-mapped after it was open or imported (IOException "user-mapped section",
  Win32 1224). `Build` falls back to saving the base scene over the map through Unity; a locked map icon is kept
  (`WriteMapped`, warning in the build log; `AssetDatabase.ReleaseCachedFileHandles` does not free it): restart Unity
  and rebuild that map, or run `Tools/Maps/Refresh Recent Map Icons` (icons only: Payload, Egypt, Factory, Circus).
- **End gates and the match camera** (Nov 2026, BrawlMapBuilderGates.cs). The match camera (`FreeCamera`, fov 40) hangs
  11.6 m up and 10.6 m behind the player, towards the player's own end. The themed gates (IceGate ... ToriiGate) stood at
  |z| 22.5, right behind the back spawn row (19.6) and 8-9.7 m tall: they hid the players at their spawn and, on Payload /
  MvM, the bomb hole (16.5). Now `PlaceGates` puts them at |z| `GateZ` 27 (in the alcove between the end border pieces)
  scaled by `GateScale` (largest scale <= 0.98 that stays 30 cm under the camera's line of sight to a player 1 m behind
  the back spawns: Ice 0.73, Xmas 0.76, Temple 0.77, Greek 0.80, Volcano / Torii 0.84, Candy 0.87, Swamp 0.91, Atlantis
  0.94, Circus 0.97, Factory / Airlock / Safari 0.98). The end lakes (`EndLakes`: Volcano, Swamp, Greek, Candy) leave a
  floor inlet under the gate (|x| < 6.5 up to |z| 30.5). `ClearEndCamera` (in Build, after the lighting) casts the
  camera's lines of sight to every spawn (and 1 m behind it) and to the bomb hole: a backdrop piece beyond the end wall
  that cuts one is pushed outwards, a border piece becomes a variant of its family that does not (the Savanna ridge B with
  its 9.9 m baobab -> A / C at the ends); CircusWallA has no lower variant (2 of 72 lines, a corner spawn). Old themes
  keep the base scene's open Gate arch (it never blocked). Existing scenes: menu `Tools/Maps/Fix End Gates (all maps)` /
  `FixEndGatesBatch(start, count)` (~1 s per map, ~30 per MCP call; also splits the end lakes, lifts the decals below and
  rebakes the NavMesh: the gates' old footprint was cut out of it). Checked: no gate in the way on all 92 new-theme maps;
  a solo match on NileExpress shows the whole team at the spawn. The new-theme icons (except Egypt, re-shot) still show
  the gates at the old spot.
- **Z-fighting**: the grass clumps (`grass.build_frostgrass`: FrostGrass, PineGrass, PapyrusGrass, ScorchGrass, YardGrass,
  FairGrass, StarGrass, BogGrass, MaquisGrass, SugarGrass, KelpGrass, SavannaGrass, StrawTuft, EmberTuft, PondReeds) had a
  cap at the root of each leaf facing up at y = 0, flush with the floor: the foot of every bush flickered. The caps are gone
  (634 tris). Floor decals with faces flush with the floor (`FlushDecals`: LavaCrack, CooledFlow) are lifted 1.2 cm.
  Faces of a new prop must never lie flush with the floor facing up.
- **Payload cart & pace** (Nov 2026): `BombCartController` raises its `speeds` (metres per physics step, now a floor)
  so a whole uncontested push from the cart to the target takes `pushShare` of the match time (0.40 / 0.30 / 0.24 with
  1 / 2 / 3+ pushers; match time = `ConfigManager.BattleTimeOf`, like the timer; length measured along the coaster's
  Bezier samples). Then x Title Data `cart_payload_speed_scale` (default 1, read with a dot decimal, must be > 0). Before, one fixed speed (0.4 / 0.6 / 0.75 m/s) made the 65 m tracks take 77% of a match with one
  pusher: blue lost. Tracks run 22.6-65 m. The cart is 1.25 x (`CartScale`, still on the 0.95 m gauge) with a 2.5 m
  push zone (`CartPushRadius`, EntryFieldOfView.radius): `SizeCart` in Build(), menu `Tools/Maps/Apply Payload Cart
  Size` for the existing scenes.
- **Map_Brawl_Payload** (hand-made; retired from the build 27 Nov 2026, but the scene stays: the generated Payload maps
  clone its cart systems and the Cave maps take its lighting) had its `NavMeshSurface` switched off
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
    The warning disc (BrawlFortress/VentWarn) was pink on phones until Nov 2026: written with `half`, its `step()`
    comparisons did not compile for Metal / GLES3 / Vulkan (fine in the D3D11 editor); now `float`. The disc renders in
    the editor (solo match on MagmaRift); not yet seen on a phone.
- **Mobile shader check**: the Windows editor compiles D3D11 only, so a shader can be fine there and pink on a phone.
  After writing / changing a custom shader, compile it for the phones: `ShaderUtil.GetShaderData(shader).GetSubshader(0)
  .GetPass(p).CompileVariant(ShaderType.Fragment, new string[0], ShaderCompilerPlatform.Metal, BuildTarget.iOS)` (also
  GLES3x / Vulkan with BuildTarget.Android) and read `Success` / `Messages`. Avoid `half` with `step()` / comparisons.
  Nov 2026: all 13 BrawlFortress/* shaders pass (FlatKit surface shaders cannot be checked this way: "did not find
  kernel", a false alarm).
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
