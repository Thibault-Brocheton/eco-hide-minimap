# HideMinimap

A small Eco mod that hides or overrides the minimap to create more immersive or roleplay-oriented gameplay.

## Features
• Minimap Hiding

Replaces all minimap terrain data with:
- a fixed height (default: 160)
- a single block type (default: ShaleBlock, customizable)

This prevents players from using the minimap to navigate or scout terrain.

## Commands (Admin-only)

- /HideMinimap Hide [height] [block]
> Enables minimap hiding.

```
Examples:

/HideMinimap Hide
/HideMinimap Hide 200,GraniteBlock
```

- /HideMinimap Restore
> Restores normal minimap behavior.

No client reconnect required. The setting persists through a server restart.

## Installation

Drop the mod folder into: **Mods/UserCode/**  
A config file will be generated automatically in **Configs/HideMinimap.eco**
