# AsyncGridPathfinderGrid

A Unity C# script implementing **A\*** grid-based pathfinding with:

* **Grid-first API** using `Vector2Int` coordinates
* **Asynchronous scheduler** with a frame-time budget
* **Multiple concurrent pathfinding jobs**
* **Multi-point pathfinding** (A → B → C → …)
* **K-shortest alternative paths** (lightweight Yen's algorithm)
* **Walkable cell discovery** within a range (optional paths to each)
* 4-way or 8-way movement with optional diagonal corner-cut blocking
* Integer-based movement costs for speed (10 straight, 14 diagonal)


---

## ✨ Features

### 1. Asynchronous Pathfinding

* Runs across multiple frames to prevent frame drops.
* Configurable frame budget (`frameBudgetMs`) and node expansions per job (`stepNodesPerJob`).

### 2. Multiple Jobs at Once

* Supports multiple path requests running concurrently.
* Each job is independent thanks to per-search stamping.

### 3. Multi-Point Pathfinding

* Provide multiple waypoints (A → B → C → …) and get a stitched path.

### 4. K Alternative Paths

* Returns up to **K** distinct shortest paths between two points.
* Useful for AI variety, rerouting, or testing.

### 5. Walkable Discovery

* Returns all walkable tiles within a Manhattan range from a center point.
* Optional: also return a BFS path to each discovered tile.

### 6. Flexible Movement

* **4-way** (cardinal only) or **8-way** movement.
* Optional blocking of diagonal corner cutting.


---

## 📦 Requirements

* Unity 2021+ (tested in 2021.3+)
* A `GridManager` component that provides:
  * `int width, height`
  * `bool IsWalkable(Vector2Int gridPos)`
  * (Optional) `Dictionary<Vector2Int, GridGroundObject> groundGridObjects`


---

## ⚙️ Inspector Settings

| Setting | Description |
|----|----|
| **Grid** | Reference to your `GridManager`. |
| **Frame Budget Ms** | Max milliseconds spent per frame across all jobs. |
| **Step Nodes Per Job** | Max nodes processed per job per round-robin step. |
| **Eight Way** | Enable 8-direction movement. |
| **Block Corner Cut** | When 8-way is enabled, disallow diagonals if both touching cardinals are blocked. |


---

## 🛠️ Example Usage

### 1. Single Path (Async)

```csharp
var startGrid = new Vector2Int(0, 0);
var goalGrid  = new Vector2Int(5, 7);

pathfinder.RequestPath(startGrid, goalGrid, result => {
    if (result.success)
    {
        Debug.Log("Path found!");
        foreach (var p in result.path)
            Debug.Log($"Step: {p}");
    }
    else
    {
        Debug.Log("No path found.");
    }
});

// 2. Multi-Point Path

var waypoints = new List<Vector2Int> {
    new Vector2Int(2, 3),   // Start
    new Vector2Int(10, 3),  // Midpoint
    new Vector2Int(10, 8)   // Goal
};

pathfinder.RequestMultiPointPath(waypoints, result => {
    if (result.success)
        Debug.Log($"Path has {result.path.Count} steps.");
});

// 3. K Alternative Paths

var altPaths = pathfinder.FindKPaths(new Vector2Int(0,0), new Vector2Int(7,4), k: 3);
for (int i = 0; i < altPaths.Count; i++)
{
    Debug.Log($"Alternative #{i+1} length: {altPaths[i].Count}");
}

// 4. Walkable Cells in Range

var (cells, paths) = pathfinder.GetWalkablesInRange(new Vector2Int(5,5), range: 4, includePaths: true);
Debug.Log($"Found {cells.Count} walkable cells.");

if (paths != null)
{
    foreach (var kvp in paths)
        Debug.Log($"To {kvp.Key}: {kvp.Value.Count} steps");
}

// 5. Multiple Concurrent Paths

// Start two path requests at once
pathfinder.RequestPath(new Vector2Int(0,0), new Vector2Int(5,5), r => {
    Debug.Log($"Path 1: {r.success}, Steps: {r.path.Count}");
});

pathfinder.RequestPath(new Vector2Int(1,1), new Vector2Int(6,6), r => {
    Debug.Log($"Path 2: {r.success}, Steps: {r.path.Count}");
});
```


