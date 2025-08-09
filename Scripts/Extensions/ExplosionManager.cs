using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMAR.GridSystem;

public class ExplosionManager : MonoBehaviour
{
    [Header("Explosion Prefabs")]
    public GameObject explosionEffectPrefab;
    public float explosionEffectDuration = 1.0f;
    public float explotionHeight = 0.35f;
    [Header("Grid Reference")]
    public GridManager gridManager;
    public static ExplosionManager Instance;
    public bool DebugMode = false;

    private void Awake()
    {
        Instance = this;
    }

    private readonly Vector2Int[] cardinalDirections = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };
    private readonly Vector2Int[] diagonalDirections = {
        new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
    };

    public void TriggerExplosion(Vector2Int center, int range, bool allowDiagonal)
    {
        List<Vector2Int> affectedTiles = CalculateExplosionPattern(center, range, allowDiagonal);
        StartCoroutine(ShowExplosionEffectsSpread(center, affectedTiles));
        // Add damage logic here if needed
        if (DebugMode)
        {
            Debug.Log($"Explosion triggered at {center} with range {range}. Affected tiles: {string.Join(", ", affectedTiles)}");
        }
    }

    private List<Vector2Int> CalculateExplosionPattern(Vector2Int center, int range, bool allowDiagonal)
    {
        List<Vector2Int> affectedTiles = new List<Vector2Int> { center };
        foreach (var dir in cardinalDirections)
            AddBlastLine(center, dir, range, affectedTiles);
        if (allowDiagonal)
            foreach (var dir in diagonalDirections)
                AddBlastLine(center, dir, range, affectedTiles);
        return affectedTiles;
    }

    private void AddBlastLine(Vector2Int start, Vector2Int direction, int range, List<Vector2Int> affectedTiles)
    {
        for (int i = 1; i <= range; i++)
        {
            Vector2Int pos = start + direction * i;
            if (!gridManager.IsValidGridPosition(pos))
                break;
            if (IsBlocked(pos))
            {
                if (IsDestructible(pos))
                    affectedTiles.Add(pos);
                break;
            }
            affectedTiles.Add(pos);
        }
    }

    private bool IsBlocked(Vector2Int gridPos)
    {
        if (gridManager.groundGridObjects.TryGetValue(gridPos, out GridGroundObject groundObj))
        {
            if (groundObj.onGridObject != null)
            {
                //var obj = groundObj.onGridObject;
                //if (obj.CompareTag("Wall") || obj.CompareTag("Indestructible") ||
                //    obj.CompareTag("Destructible") || obj.CompareTag("Crate"))
                //    return true;
            }
        }
        return false;
    }

    private bool IsDestructible(Vector2Int gridPos)
    {
        if (gridManager.groundGridObjects.TryGetValue(gridPos, out GridGroundObject groundObj))
        {
            if (groundObj.onGridObject != null)
            {
                var obj = groundObj.onGridObject;
                return obj.CompareTag("Destructible") || obj.CompareTag("Crate");
            }
        }
        return false;
    }

    private IEnumerator ShowExplosionEffectsSpread(Vector2Int center, List<Vector2Int> affectedTiles)
    {
        List<List<Vector2Int>> sortedTiles = new List<List<Vector2Int>>();
        for (int i = 0;i < affectedTiles.Count; i++)
        {
            int distance = (int)Vector2Int.Distance(center, affectedTiles[i]);
            while (sortedTiles.Count <= distance)
            {
                sortedTiles.Add(new List<Vector2Int>());
            }
            sortedTiles[distance].Add(affectedTiles[i]);
        }
        for (int i = 0; i < sortedTiles.Count; i++)
        {
            if (sortedTiles[i].Count > 0)
            {
                foreach (var tile in sortedTiles[i])
                {
                    Vector3 worldPos = gridManager.GridToWorld(tile);
                    worldPos.y += explotionHeight; // Adjust height for explosion effect
                    if (explosionEffectPrefab != null)
                    {
                        var effect = Instantiate(explosionEffectPrefab, worldPos, Quaternion.identity);
                        Destroy(effect, explosionEffectDuration);
                    }
                }
            }
            yield return new WaitForSeconds(0.08f); // Delay between rings
        }
        
    }
}
