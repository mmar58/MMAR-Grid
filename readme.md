# MMAR Grid

A lightweight, extensible, grid-first framework for Unity. MMAR Grid lets you:

* Generate and manage 2D grid worlds (X/Z plane) with integer precision
* Place and drag GridObjects that align/snap to the grid
* Query and convert between world and grid coordinates (Vector3 ⇄ Vector2Int)
* Build gameplay extensions on top: pathfinding, movement, explosions, etc.

This project already includes production-ready extensions:

* GridPathfinding (A\*)
* GridMovementController (character controller)
* ExplosionManager (grid-aligned explosions with cardinal/diagonal options)

## Contents

* Core (Base)
  * GridManager: grid definition, coordinate conversion, placement helpers
  * GridGroundObject: one per tile; holds an optional GridObject
  * GridObject: base class for placeable things (bombs, crates, players...)
* Extensions
  * GridPathfinding: A\* search over the grid
  * GridMovementController: Bomberman-style 4/8-way tile movement with dash
  * ExplosionManager: tile-by-tile explosion visualisation and blocking

## Install

You can use any **one** of the following three methods:

* Download and import from Asset Store.
* Unity **Package Manager → Add package by Git URL** `https://github.com/mmar58/MMAR-Grid.git`
* Clone the repo anywhere on your machine, and use `Install package from disk` from Package Manager.
* Copy the repo in your unity project

## Core Concepts

### GridManager

The heart of the system. Responsibilities:

* Holds grid size `width` × `height` and `gridStartPoint`
* Owns `groundGridObjects: Dictionary<Vector2Int, GridGroundObject>`
* Converts coordinates:
  * `Vector2Int WorldToGrid(Vector3 world)` → clamps into bounds
  * `Vector3 GridToWorld(Vector2Int grid)`
  * `bool IsValidGridPosition(Vector2Int pos)`
* Placement helpers:
  * `PlaceObjectFromPrefab(GridObject prefab, Vector2Int pos)`
  * `AddGridObject(GridObject prefab)` for drag-and-drop authoring

Each cell has a `GridGroundObject` that may hold one `GridObject` via `onGridObject`.

### GridObject

Base class for anything that lives on the grid.

* Tracks `gridPosition` and y-offset on placement
* Snaps itself to the nearest valid tile on start (`PlaceToNearestGrid()`)
* Provides drag UX via `MouseInputManager` integration

### GridGroundObject

* Holds a reference to the `onGridObject`
* Surfaces mouse hover events through `GridManager` UnityEvents

## Pathfinding (Extensions/GridPathfinding.cs)

A\* pathfinding over the MMAR grid.

* 4-way movement (cardinals) with Manhattan heuristic
* Coroutine mode with partial updates and debug drawing
* Synchronous mode for one-off queries
* Utility methods: direct line checks, walkables in range

Important notes:

* Ensure `GridManager` is assigned/available before calling `FindPath`
* Keep walkability fresh:
  * Either call `UpdateNodeWalkability(pos, walkable)` when tiles change
  * Or query `IsPositionWalkable(pos)` dynamically when expanding neighbors
* For large maps, consider replacing the open set with a binary heap for speed
* If you need 8-way movement, extend `GetNeighbors()` with diagonals and switch to an octile heuristic

## Character Movement (Extensions/GridMovementController.cs)

Plug-and-play player controller built on the grid.

* 4/8-way inputs (Unity Input System) with optional diagonal corner-cut protection
* Smooth per-tile motion (MoveTowards) with input buffering
* Optional dash across N tiles with easing
* Smooth look-at rotation toward movement/dash direction
* Helpers:
  * `CurrentGridPosition`
  * `GetFacingDirection()` and `GetFrontGridPosition()`
  * `IsFrontPositionClear()`

Wire it by adding `PlayerInput` and binding a "Move" (Vector2) and "Dash" action.

## Explosions (Extensions/ExplosionManager.cs)

Tile-aligned explosions that respect blocking.

* Cardinal rays plus optional diagonals
* Stops on blocking objects; includes destructibles on impact
* Spawns an effect prefab per affected tile, with optional ring spread

Usage:

```csharp
// From a Bomb, when exploding:
ExplosionManager.Instance.TriggerExplosion(centerGrid, range, allowDiagonal);
// Bomb object can then destroy itself
```

To integrate damage, iterate affected tiles and apply to any `GridObject` (or a custom interface).

## Quick Start


1. Add a GridManager to a scene

* Set width/height and gridStartPoint
* Assign `Grid Ground Object` in Game Objects prefab of GridManager
* Assign `gridGroundParent` and `gridObjectParent` (optional)
* Generate tiles via the button or call `GenerateGridGround()`


2. Spawn/place objects

* Derive your items from `GridObject`
* Call `PlaceObjectFromPrefab(prefab, pos)` or drag in editor


3. Add Extensions as needed

* Pathfinding: add `GridPathfinding` and call `FindPath(startWorld, endWorld)` or `FindPathAsync`
* Movement: add `GridMovementController` + `PlayerInput`
* Explosions: add `ExplosionManager` and assign the effect prefab


