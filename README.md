# 🗺️HideMinimap Eco Mod

An Eco mod that hides the minimap except inside chosen districts, for immersive or roleplay-oriented gameplay.

## Features

- **Minimap hiding** — outside revealed districts, the minimap renders as flat dark fog. Display-only: the mod sends a reveal mask (`MinimapManager.SetRevealMask`) and the client's minimap shader fogs hidden chunks, while the real chunk data keeps flowing — so **far terrain rendering is unaffected**. World layers and overlays only draw over revealed districts.
- **District reveal** — the minimap is only rebuilt from the real world inside revealed districts (a chunk is revealed when any of its plots belongs to one). Districts are matched by *map name* and *district name* (case-insensitive), so editing a district in civics keeps everything working; the revealed area follows the district as it grows or shrinks.
- **Icons** — player, vehicle, world object and tree icons are only shown when they stand inside a revealed district (masked client-side from the reveal mask, instant).
- **Map Control object** — a craftable 1x2 object (Carpentry Table, Carpentry lvl 1: 20 lumber, 10 paper), listed in the Ecopedia under *Housing Objects > Cultural*. Configure `District Map Name` and `District Name` in its tab (leave the district empty to control the whole map), then press **Reveal map here / Hide map here**. The tab shows the current status of the target district; the button is disabled while the minimap is not hidden or the district cannot be found. Anyone with consumer access to the object can use it.

Everything is global: a revealed district is revealed for every player.

The mask also covers the **web server map** (terrain, heightmap, preview and layer images regenerate fogged via `WorldLayerHistory.MapRevealedTest`) and the **in-world map objects** (framed world maps render fog on hidden chunks). Note: the web map's history timeline keeps frames recorded before hiding was enabled.

## Commands (Admin-only)

| Command | Effect |
| --- | --- |
| `/HideMinimap Hide [fogHeight],[fogColor]` | Enables hiding; optional fog height (0-255, -1 = default) and RRGGBB fog color (`/HideMinimap Hide 71,1a3350`). Keep the fog height in ~40-180: 255 lets the zoom camera clip through it, very low values show edge artifacts. |
| `/HideMinimap Restore` | Restores the vanilla minimap (also re-enables it after `KillMap`). |
| `/HideMinimap RevealDistrict <map>,[district]` | Reveals a district (or a whole map). |
| `/HideMinimap ConcealDistrict <map>,[district]` | Hides it again. |
| `/HideMinimap ListDistricts` | Shows whether hiding is active, the revealed districts and the active district maps. |
| `/HideMinimap KillMap` | Disables the minimap network object entirely. |
| `/HideMinimap ShowLayers` / `HideLayers` | Toggles all world layers. |

Settings persist in `Configs/HideMinimap.eco` (`HideMinimap`, `KillMinimap`, `RevealedDistricts` as `"Map/District"`, `HiddenMapHeight`, `HiddenMapColor`), so they survive a server restart. They can also be edited from the server UI (*Mods* category), where the plugin status shows whether the minimap is hidden and how many chunks are revealed. No client reconnect required.

## Requirements

Eco **14.1.1 or newer** on both server and client: the reveal mask (`MinimapManager.SetRevealMask`, `WorldLayerHistory.MapRevealedTest`) and the client-side fog shader shipped with that version.

## Installation

Drop the mod folder into **Mods/UserCode/**; the server compiles the sources at startup, so no build step is needed (`HideMinimap.csproj` is only there for IDE support and expects the Eco server projects two levels up). The `MapControlObject` has no dedicated client prefab: without a mod bundle providing one, the client shows the default placeholder object (the UI tab and button still work).
