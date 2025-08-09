# GridMovementController

Bomberman-style per-tile movement driven by Unity's Input System, built on MMAR Grid. Supports 4/8-way movement, optional corner-cut prevention, smooth look rotation, and dash.

## Features

* 4-way or 8-way movement (diagonals optional)
* Buffered direction changes mid-tile
* Finish-current-tile toggle when input is released
* Dash across N tiles with easing and cooldown
* Optional rotation toward movement or dash heading

## Component

Script: `GridMovementController.cs`

Inspector settings:

* grid (GridManager)
* unitsPerSecond (float)
* finishCurrentTileOnRelease (bool)
* allowDiagonal (bool)
* rotateTowardMove (bool), rotationSpeed (deg/sec)
* enableDash (bool), dashTiles, dashCooldown, dashEase
* animator (optional)

## Input

Requires a `PlayerInput` with:

* `Move` (Vector2)
* `Dash` (Button)

## Public Helpers

* `Vector2Int CurrentGridPosition`
* `Vector2Int GetFacingDirection()`
* `Vector2Int GetFrontGridPosition()`
* `bool IsFrontPositionClear()`

## Usage


1. Add `PlayerInput` and bind actions
2. Add `GridMovementController` and assign a `GridManager`
3. Optionally add an Animator and hook up `Walking`/`Dashing` bools

## Notes

* When diagonals are enabled, the controller prevents corner cutting by requiring adjacent cardinals to be free for diagonal steps.
* You can pair this with the pathfinder by feeding waypoints to move toward each tile in sequence.


