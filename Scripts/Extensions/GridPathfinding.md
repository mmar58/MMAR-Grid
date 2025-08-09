# GridPathfinding (A\*)

A grid-based A\* pathfinding component built for MMAR Grid. Supports synchronous and coroutine-based (non-blocking) search, with optional node visualization and partial-path updates.

## Features

* 4-way movement (cardinals) with Manhattan heuristic
* Async coroutine search with partial updates via events
* Direct path check (Bresenham), and walkable-in-range queries
* Simple integration with GridManager

## Component

Script: `GridPathfinding.cs`

Inspector settings:

* visualizeNodes (bool)
* nodeVisualizationDuration (float)
* openNodeColor, closedNodeColor, pathColor

## Public API

* `Coroutine FindPathAsync(Vector3 startWorld, Vector3 targetWorld)`
  * Events: `OnPathFound(List<Vector3>)`, `OnPartialPathFound(List<Vector3>)`, `OnPathNotFound()`
* `List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)`
* `void UpdateNodeWalkability(Vector2Int gridPos, bool walkable)`
* `bool IsPositionWalkable(Vector2Int gridPos | Vector3 world)`
* `List<Vector3> GetWalkablePositionsInRange(Vector3 centerWorld, int range)`
* `bool HasDirectPath(Vector3 startWorld, Vector3 endWorld)`

## Usage

```csharp
var pathfinder = FindObjectOfType<GridPathfinding>();
var path = pathfinder.FindPath(unit.transform.position, target.position);
if (path != null && path.Count > 0) {
    // follow path world positions in order
}

// Async with updates
pathfinder.OnPartialPathFound += partial => Debug.Log($"Partial: {partial.Count}");
pathfinder.OnPathFound += final => Debug.Log($"Final: {final.Count}");
pathfinder.OnPathNotFound += () => Debug.Log("No path");
pathfinder.FindPathAsync(start, goal);
```

## Integration Tips

* Ensure `GridManager` is present and sized before calling `FindPath`.
* Keep walkability up to date: call `UpdateNodeWalkability` when tiles change, or modify the search to call `IsPositionWalkable` during neighbor expansion.
* For larger grids, consider a binary heap for the open set to reduce O(n) scans.
* If you enable 8-way movement in your game, extend `GetNeighbors()` and switch the heuristic to octile distance.

## Known Limits

* Uses a List for the open set (linear min lookup).
* Resets the entire node grid each query; optimize with a touched-nodes list or searchId stamping if needed.


