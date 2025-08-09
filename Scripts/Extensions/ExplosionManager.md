# ExplosionManager

Spawns grid-aligned explosion visuals and supports cardinal-only or diagonal-inclusive rays. Stops at blocking objects and may include destructibles at the impact tile.

## Features
- Cardinal rays (up, down, left, right)
- Optional diagonals (NE, NW, SE, SW)
- Stops at blocking tiles; includes destructible on impact
- Optional ring-spread timing for visuals

## Component
Script: `ExplosionManager.cs`

Inspector settings:
- explosionEffectPrefab (GameObject)
- explosionEffectDuration (float)
- explotionHeight (float)
- gridManager (GridManager)
- DebugMode (bool)

## API
- `void TriggerExplosion(Vector2Int center, int range, bool allowDiagonal)`
  - Computes affected tiles and spawns effects per tile

## Usage
```csharp
// From Bomb on explode
ExplosionManager.Instance.TriggerExplosion(bombGridPos, range, allowDiagonal);
Destroy(gameObject); // remove bomb after triggering
```

## Blocking & Destructibles
`IsBlocked`/`IsDestructible` checks consult `gridManager.groundGridObjects[gridPos]`:
- Blocking examples: Walls, Indestructible
- Destructible examples: Crates (included at impact and then stop)

Customize tags/logic within `ExplosionManager` to fit your game’s rules.
