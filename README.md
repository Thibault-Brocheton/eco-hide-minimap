# 🗺️ HideMinimap Eco Mod

Hides the minimap for everyone, except in the areas you choose. Perfect for exploration and roleplay servers: players only see the map of the places that are "known".

## What gets hidden

Outside revealed areas, the minimap shows a flat fog instead of the terrain. This also applies to:
- player, vehicle, object and tree icons,
- the web server map,
- in-game world map objects.

The world itself is unaffected: players still see far terrain normally when walking around.

## Revealing areas

You can reveal the map in several ways, and they combine:

- **Districts**: reveal a district (or a whole district map) with a command or a **Map Control** object. The revealed area follows the district when it's edited in civics.
- **Settlements**: reveal everything under the influence of towns, nations and/or federations.
- **Claims**: reveal every claimed plot.

Revealed areas are the same for every player, and update live as districts, influence and claims change.

### Map Control object

A craftable object (Carpentry Table, Carpentry level 1: 20 lumber, 10 paper). In its tab, enter the district map name and the district name (leave the district empty for the whole map), then press **Reveal map here** / **Hide map here**. Anyone with access to the object can use it.

## Commands (admin)

| Command | Effect |
| --- | --- |
| `/HideMinimap Hide [fogHeight],[fogColor]` | Hides the minimap. Optional fog height (0-255, around 40-180 looks best) and color as RRGGBB, e.g. `/HideMinimap Hide 71,1a3350`. |
| `/HideMinimap Restore` | Shows the normal minimap again. |
| `/HideMinimap RevealDistrict <map>,[district]` | Reveals a district, or a whole map. |
| `/HideMinimap ConcealDistrict <map>,[district]` | Hides it again. |
| `/HideMinimap ToggleTownInfluence` | Reveals or hides areas under town influence. |
| `/HideMinimap ToggleNationInfluence` | Reveals or hides areas under nation influence. |
| `/HideMinimap ToggleFederationInfluence` | Reveals or hides areas under federation influence. |
| `/HideMinimap ToggleClaims` | Reveals or hides all claimed plots. |
| `/HideMinimap Status` | Shows whether the minimap is hidden, the fog settings and every reveal setting. |
| `/HideMinimap ListDistricts` | Shows the current state, revealed districts and available district maps. |
| `/HideMinimap ShowLayers` / `HideLayers` | Shows or hides all world layers. |

## Configuration

Settings are saved in `Configs/HideMinimap.eco` and can be edited from the server UI (*Mods* category). Changes apply immediately.

| Setting | Description |
| --- | --- |
| `HideMinimap` | Whether the minimap is hidden. |
| `HiddenMapHeight` / `HiddenMapColor` | Fog height and color (-1 / empty = default). |
| `RevealedDistricts` | Revealed districts, as `"Map/District"` (`"Map/"` for a whole map). |
| `TownInfluenceIsUnhidden` | Reveal areas under town influence. |
| `NationInfluenceIsUnhidden` | Reveal areas under nation influence. |
| `FederationInfluenceIsUnhidden` | Reveal areas under federation influence. |
| `ClaimInfluenceIsUnhidden` | Reveal all claimed plots. |

## Installation

Requires Eco **14.1.1 or newer**.

Drop the mod folder into `Mods/UserCode/` and restart the server. The Map Control object has no custom model, so it shows as a placeholder in game (it works normally).
