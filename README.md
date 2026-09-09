# 🗺️HideMinimap Eco Mod

An Eco mod that hides the minimap except inside chosen districts, for immersive or roleplay-oriented gameplay.

## Features

- **Minimap hiding** — outside revealed districts, the minimap renders as flat dark fog. Display-only: the mod sends a reveal mask (`MinimapManager.SetRevealMask`) and the client's minimap shader fogs hidden chunks, while the real chunk data keeps flowing — so **far terrain rendering is unaffected**. World layers and overlays only draw over revealed districts. Requires a client with reveal-mask support (Eco 14.2+).
- **District reveal** — the minimap is only rebuilt from the real world inside revealed districts (a chunk is revealed when any of its plots belongs to one). Districts are matched by *map name* and *district name*, so editing a district in civics keeps everything working; the revealed area follows the district as it grows or shrinks.
- **Icons** — player, vehicle, world object and tree icons are only shown when they stand inside a revealed district (masked client-side from the reveal mask, instant).
- **Map Control object** — a craftable object (Carpentry Table, Carpentry lvl 1). Configure `District Map Name` and `District Name` in its tab (leave the district empty to control the whole map), then press **Reveal map here / Hide map here**. Anyone with consumer access to the object can use it.
- **Palantir object** — a craftable seeing-stone (Glassworks, Glassworking lvl 1). The claim owner configures the district (`District Map Name` / `District Name`) and the orb `Color` (Cyan, Green, Purple, Red, Yellow); **anyone** can press **Activate / Deactivate**. While activated the orb glows in its color and the district's minimap is revealed; deactivated it turns grey and the district is hidden again (also on pickup). The client prefab lives in the Incroyaux client mod (`Assets/Mods/Incroyaux`), driven by the object enabled state plus one `Color<Name>` animated state per color.

- **Berlu statuette** — a small clay tabletop collectible made by painters (Paint Mixer, Painting lvl 1: 4 clay, 3 oil paint). Its color is rolled once, the first time the item is inspected or placed (Classic 24%, Grey 16%, Blue 14%, Green 14%, Orange 12%, Purple 8%, Black 6%, Pink 5.89%, Yellow 0.1%, Red 0.01%), shown in the item tooltip, and carried through placement and pickup via item persistent data. Client prefab in the Incroyaux client mod, driven by one `Color<Name>` animated state per variant.

Everything is global: a revealed district is revealed for every player.

The mask also covers the **web server map** (terrain, heightmap, preview and layer images regenerate fogged via `WorldLayerHistory.MapRevealedTest`) and the **in-world map objects** (framed world maps render fog on hidden chunks). Note: the web map's history timeline keeps frames recorded before hiding was enabled.

## Commands (Admin-only)

| Command | Effect |
| --- | --- |
| `/HideMinimap Hide [fogHeight],[fogColor]` | Enables hiding; optional fog height (0-255, -1 = default) and RRGGBB fog color (`/HideMinimap Hide 71,1a3350`). Keep the fog height in ~40-180: 255 lets the zoom camera clip through it, very low values show edge artifacts. |
| `/HideMinimap Restore` | Restores the vanilla minimap. |
| `/HideMinimap RevealDistrict <map>,[district]` | Reveals a district (or a whole map). |
| `/HideMinimap ConcealDistrict <map>,[district]` | Hides it again. |
| `/HideMinimap ListDistricts` | Shows revealed districts and the active district maps. |
| `/HideMinimap KillMap` | Disables the minimap network object entirely. |
| `/HideMinimap ShowLayers` / `HideLayers` | Toggles all world layers. |

Settings persist in `Configs/HideMinimap.eco` (`HideMinimap`, `RevealedDistricts` as `"Map/District"`, `HiddenMapHeight`, `HiddenMapColor`), so they survive a server restart. No client reconnect required.

## Installation

Drop the mod folder into **Mods/UserCode/**. The `MapControlObject` has no dedicated client prefab: without a mod bundle providing one, the client shows the default placeholder object (the UI tab and button still work).
