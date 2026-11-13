# Bush hiding (Brawl-Stars style)

## How it works
- `BushArea` (Assets/Scripts/Bush/BushArea.cs): one bush = list of world-space boxes. Lives under a root object `Bushes` in the **map scene**.
- `BushStealth` (Assets/Scripts/Bush/BushStealth.cs): added to every `TF2Player` in `TF2Player.CreateCharacter()`.
  - me / teammates in a bush -> see-through (`GhostAlpha` = 0.6, shader `FlatKit/Stylized Surface Ghost`, material `Resources/Materials/BushGhost.mat`)
  - enemy in a bush -> not rendered (model, weapon, header UI) unless the local player is in the same bush and within `SameBushRange` (6 m), or within `RevealDistance` (0 = off)
  - leaving a bush uses a 0.35 m margin (`ExitMargin`) to avoid flicker
- `FieldOfView` (bots, sentries): skips targets for which `BushStealth.IsHiddenFrom(...)` is true.
- **Nothing is generated at runtime.** A map only has bushes that are baked into its scene.

## Adding bushes to a (new) map
1. Open the map scene (e.g. `Assets/Scenes/Map_xxx.unity`).
2. `Tools > Bush > Bush Area Tool` -> press **Build** next to the scene -> **Save**.
   (or `Tools > Bush > Build Bush Areas (Active Scene)` then save the scene)
3. Check the green boxes in the Scene view by selecting `Bushes/Bush_n` objects.

Detection rules (`BushAreaGenerator.cs`, editor-only):
- mesh renderer whose object name or mesh name contains `grass` or `bush` (case-insensitive)
- height >= `MinHeight` (0.5 m) -> flat grass decals are ignored
- each mesh footprint is at least `MinFootprint` (0.9 m); meshes closer than `LinkGap` (0.35 m) form one bush
- bushes smaller than `MinClusterArea` (1.5 m2) are dropped (lone decorative tufts)
- Rebuilding replaces the whole `Bushes` object (manual edits inside it are lost).

## Maps with baked bushes
| Map scene | Bushes |
|---|---|
| BrawlScene | 15 |
| BrawlScene_2 | 10 |
| Map_Brawl_MvM | 14 |
| Map_Brawl_Payload | 8 |
| Map_Brawl_ShowDown (Moonlight Arena) | 12 |
| Map_Brawl_CTF (Moonlight Arena, Capture The Flag) | 12 |
| Map_Brawl_Koth (Bamboo Forest, King Of The Hill) | 10 |
| Map_Brawl_Oasis (Sunset Oasis) | 10 |
| Map_Brawl_Cavern (Crystal Cavern) | 6 |
| Map_Brawl_MoonMaze (Moonlit Maze) | 18 |
| Map_Brawl_BambooTemple (Bamboo Temple) | 16 |
| Map_Brawl_DustyCanyon (Dusty Canyon) | 10 |
| Map_Brawl_FrontierRuins (Frontier Ruins) | 12 |

## Builder maps for CTF / KOTH / Payload / MvM (2026-11-13)
| Map scene | Mode | Theme | Bushes |
|---|---|---|---|
| Map_Brawl_GemHeist | CTF | Cave | 10 |
| Map_Brawl_LunarVault | CTF | Moon | 14 |
| Map_Brawl_BambooRaid | CTF | Bamboo | 15 |
| Map_Brawl_GoldRush | CTF | Western | 8 |
| Map_Brawl_BorderSiege | CTF | Mix | 14 |
| Map_Brawl_CrystalCrown | KOTH | Cave | 12 |
| Map_Brawl_MoonAltar | KOTH | Moon | 14 |
| Map_Brawl_BambooShrine | KOTH | Bamboo | 12 |
| Map_Brawl_SheriffSquare | KOTH | Western | 12 |
| Map_Brawl_CrossroadRuins | KOTH | Mix | 12 |
| Map_Brawl_MineExpress | Payload | Cave | 14 |
| Map_Brawl_MoonRail | Payload | Moon | 12 |
| Map_Brawl_BambooLine | Payload | Bamboo | 18 |
| Map_Brawl_DynamiteTrail | Payload | Western | 12 |
| Map_Brawl_FrontierConvoy | Payload | Mix | 10 |
| Map_Brawl_RobotRidge | MvM | Western | 6 |
| Map_Brawl_IronGrove | MvM | Bamboo | 14 |
| Map_Brawl_CrystalFactory | MvM | Cave | 8 |
| Map_Brawl_MoonbaseSiege | MvM | Moon | 12 |
| Map_Brawl_LastStand | MvM | Mix | 18 |

## Bot patrol points in bushes (2026-11-13)
- `Assets/Editor/BushPatrolPoints.cs` bakes points inside every BushArea into root object `BushPoints` and assigns them to `MapManager.BushPoints` (1 point per bush, 2 for bushes of 9 m2 or more), snapped onto the scene's NavMesh.
- Runs automatically after `Tools/Bush/Build Bush Areas (Active Scene)`; alone: `Tools/Bush/Build Bush Patrol Points (Active Scene)`. Save the scene afterwards.
- Bots: `MapManager.GetPatrolPoint()` returns a bush point with probability `bushPatrolChance` (default 0.45), otherwise a PathPoint. Used by FindRandomTarget, FindFlag, CheckFlagState, KothMoveFree.
- Map builder NavMesh ignores bush renderers (NavMeshBuildMarkup ignoreFromBuild), so bushes are walkable for bots. Existing builder maps + Oasis were rebaked with `BrawlMapBuilder.RebakeNavMesh(id)`.

## Hiding needs most of the body inside (2026-11-13)
- Before: a character was hidden as soon as its centre point touched a bush box (plus 0.35 m exit margin).
- Now `BushStealth` samples the character footprint (disc of `BodyRadius` 0.5 m, 17 points) and becomes hidden when `EnterCoverage` = 80% of it is inside one bush; revealed again when less than `ExitCoverage` = 80% is inside (about 20% of the body out; Tho, 2026-11-13). `CurrentBush` = bush covering most of the footprint. Bots (FieldOfView) use the same rule.

## Brawl Stars reveal zone (2026-11-13, replaces the ghost / same-bush rules)
- Me / teammates in a bush: the character is drawn normally (no ghost material any more). Instead `BushGrassFade` (created automatically) fades the grass tiles within `BushStealth.RevealRadius` (2.4 m) of every character of the local viewer's team that is in a bush (`FadedAlpha` 0.35, `FadeTime` 0.15 s, shader `FlatKit/Stylized Surface Ghost` via `_GhostAlpha` property block).
- An enemy in a bush is hidden from a team unless it is inside the reveal zone (RevealRadius) of a character of that team that is itself in a bush. `SameBushRange` was removed. `RevealDistance` (0 = off) still reveals enemies close to the observer regardless of bushes.
- The fade needs `BushArea.renderers` (grass renderers of each bush), filled by `Tools/Bush/Build Bush Areas`. All 33 bush maps were rebaked on 2026-11-13 (bush counts unchanged).
