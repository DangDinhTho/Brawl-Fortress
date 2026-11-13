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
