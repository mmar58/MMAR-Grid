using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MMAR.GridSystem
{
    public class GridPathfinding : MonoBehaviour
    {
        [Header("Pathfinding Settings")]
        [SerializeField] private bool visualizeNodes = true;
        [SerializeField] private float nodeVisualizationDuration = 2f;
        [SerializeField] private Color openNodeColor = Color.green;
        [SerializeField] private Color closedNodeColor = Color.red;
        [SerializeField] private Color pathColor = Color.blue;
        
        private GridManager gridManager;
        private PathNode[,] nodeGrid;
        
        // Events for real-time path updates
        public event Action<List<Vector3>> OnPathFound;
        public event Action<List<Vector3>> OnPartialPathFound;
        public event Action OnPathNotFound;
        
        public class PathNode
        {
            public int x, z;
            public bool walkable;
            public float gCost, hCost;
            public PathNode parent;
            public bool isProcessed;
            
            public float fCost => gCost + hCost;
            
            public PathNode(int x, int z, bool walkable)
            {
                this.x = x;
                this.z = z;
                this.walkable = walkable;
                Reset();
            }
            
            public void Reset()
            {
                gCost = 0;
                hCost = 0;
                parent = null;
                isProcessed = false;
            }
            
            public Vector3 WorldPosition(GridManager gridManager)
            {
                return gridManager.GridToWorld(new Vector2Int(x, z));
            }
        }
        
        private void Start()
        {
            InitializeNodeGrid();
        }
        
        private void InitializeNodeGrid()
        {
            if (gridManager == null) return;
            
            nodeGrid = new PathNode[gridManager.width, gridManager.height];
            
            for (int x = 0; x < gridManager.width; x++)
            {
                for (int z = 0; z < gridManager.height; z++)
                {
                    
                    bool walkable = IsPositionWalkable(new Vector2Int(x,z));
                    nodeGrid[x, z] = new PathNode(x, z, walkable);
                }
            }
        }
        
        public void UpdateNodeWalkability(Vector2Int gridPos, bool walkable)
        {
            if (IsValidGridPosition(gridPos))
            {
                nodeGrid[gridPos.x, gridPos.y].walkable = walkable;
            }
        }
        
        public bool IsPositionWalkable(Vector2Int gridPos)
        {
            // Check if there's a grid object at this position
            if (gridManager.groundGridObjects.TryGetValue(gridPos, out GridGroundObject groundObj))
            {
                return groundObj.onGridObject == null;
            }
            return true;
        }
        public bool IsPositionWalkable(Vector3 worldPos)
        {
            // Check if there's a grid object at this position
            if (gridManager.groundGridObjects.TryGetValue(gridManager.WorldToGrid(worldPos), out GridGroundObject groundObj))
            {
                return groundObj.onGridObject == null;
            }
            return true;
        }
        
        private bool IsValidGridPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridManager.width && 
                   pos.y >= 0 && pos.y < gridManager.height;
        }
        
        /// <summary>
        /// Find path using coroutine with real-time updates
        /// </summary>
        public Coroutine FindPathAsync(Vector3 startPos, Vector3 targetPos)
        {
            return StartCoroutine(FindPathCoroutine(startPos, targetPos));
        }
        
        /// <summary>
        /// Find path synchronously (blocking)
        /// </summary>
        public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            Vector2Int startGrid = gridManager.WorldToGrid(startPos);
            Vector2Int targetGrid = gridManager.WorldToGrid(targetPos);
            
            if (!IsValidGridPosition(startGrid) || !IsValidGridPosition(targetGrid))
                return new List<Vector3>();
                
            if (!nodeGrid[targetGrid.x, targetGrid.y].walkable)
                return new List<Vector3>();
            
            ResetNodes();
            
            PathNode startNode = nodeGrid[startGrid.x, startGrid.y];
            PathNode targetNode = nodeGrid[targetGrid.x, targetGrid.y];
            
            List<PathNode> openSet = new List<PathNode>();
            HashSet<PathNode> closedSet = new HashSet<PathNode>();
            
            openSet.Add(startNode);
            
            while (openSet.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openSet);
                
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);
                currentNode.isProcessed = true;
                
                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, targetNode);
                }
                
                foreach (PathNode neighbor in GetNeighbors(currentNode))
                {
                    if (!neighbor.walkable || closedSet.Contains(neighbor))
                        continue;
                        
                    float newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                    
                    if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                    {
                        neighbor.gCost = newMovementCostToNeighbor;
                        neighbor.hCost = GetDistance(neighbor, targetNode);
                        neighbor.parent = currentNode;
                        
                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }
            
            return new List<Vector3>(); // No path found
        }
        
        private IEnumerator FindPathCoroutine(Vector3 startPos, Vector3 targetPos)
        {
            Vector2Int startGrid = gridManager.WorldToGrid(startPos);
            Vector2Int targetGrid = gridManager.WorldToGrid(targetPos);
            
            if (!IsValidGridPosition(startGrid) || !IsValidGridPosition(targetGrid))
            {
                OnPathNotFound?.Invoke();
                yield break;
            }
                
            if (!nodeGrid[targetGrid.x, targetGrid.y].walkable)
            {
                OnPathNotFound?.Invoke();
                yield break;
            }
            
            ResetNodes();
            
            PathNode startNode = nodeGrid[startGrid.x, startGrid.y];
            PathNode targetNode = nodeGrid[targetGrid.x, targetGrid.y];
            
            List<PathNode> openSet = new List<PathNode>();
            HashSet<PathNode> closedSet = new HashSet<PathNode>();
            
            openSet.Add(startNode);
            
            int processedNodes = 0;
            const int maxNodesPerFrame = 50; // Limit nodes processed per frame
            
            while (openSet.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openSet);
                
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);
                currentNode.isProcessed = true;
                
                // Visualize current node if enabled
                if (visualizeNodes)
                {
                    VisualizeNode(currentNode, closedNodeColor);
                }
                
                // Send partial path update every few nodes
                if (processedNodes % 10 == 0)
                {
                    List<Vector3> partialPath = RetracePath(startNode, currentNode);
                    OnPartialPathFound?.Invoke(partialPath);
                }
                
                if (currentNode == targetNode)
                {
                    List<Vector3> finalPath = RetracePath(startNode, targetNode);
                    OnPathFound?.Invoke(finalPath);
                    
                    if (visualizeNodes)
                    {
                        StartCoroutine(VisualizeFinalPath(finalPath));
                    }
                    
                    yield break;
                }
                
                foreach (PathNode neighbor in GetNeighbors(currentNode))
                {
                    if (!neighbor.walkable || closedSet.Contains(neighbor))
                        continue;
                        
                    float newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                    
                    if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                    {
                        neighbor.gCost = newMovementCostToNeighbor;
                        neighbor.hCost = GetDistance(neighbor, targetNode);
                        neighbor.parent = currentNode;
                        
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                            
                            if (visualizeNodes)
                            {
                                VisualizeNode(neighbor, openNodeColor);
                            }
                        }
                    }
                }
                
                processedNodes++;
                
                // Yield control every maxNodesPerFrame to prevent frame drops
                if (processedNodes % maxNodesPerFrame == 0)
                {
                    yield return null;
                }
            }
            
            OnPathNotFound?.Invoke();
        }
        
        private void ResetNodes()
        {
            for (int x = 0; x < gridManager.width; x++)
            {
                for (int z = 0; z < gridManager.height; z++)
                {
                    nodeGrid[x, z].Reset();
                }
            }
        }
        
        private PathNode GetLowestFCostNode(List<PathNode> openSet)
        {
            PathNode lowestFCostNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < lowestFCostNode.fCost ||
                    (openSet[i].fCost == lowestFCostNode.fCost && openSet[i].hCost < lowestFCostNode.hCost))
                {
                    lowestFCostNode = openSet[i];
                }
            }
            return lowestFCostNode;
        }
        
        private List<PathNode> GetNeighbors(PathNode node)
        {
            List<PathNode> neighbors = new List<PathNode>();
            
            // Cardinal directions (up, down, left, right)
            Vector2Int[] directions = { 
                new Vector2Int(0, 1),   // Up
                new Vector2Int(0, -1),  // Down
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(1, 0)    // Right
            };
            
            foreach (Vector2Int dir in directions)
            {
                int checkX = node.x + dir.x;
                int checkZ = node.z + dir.y;
                
                if (IsValidGridPosition(new Vector2Int(checkX, checkZ)))
                {
                    neighbors.Add(nodeGrid[checkX, checkZ]);
                }
            }
            
            return neighbors;
        }
        
        private float GetDistance(PathNode nodeA, PathNode nodeB)
        {
            int distX = Mathf.Abs(nodeA.x - nodeB.x);
            int distZ = Mathf.Abs(nodeA.z - nodeB.z);
            
            // Manhattan distance for grid-based movement
            return distX + distZ;
        }
        
        private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode currentNode = endNode;
            
            while (currentNode != startNode && currentNode != null)
            {
                path.Add(currentNode.WorldPosition(gridManager));
                currentNode = currentNode.parent;
            }
            
            path.Reverse();
            return path;
        }
        
        private void VisualizeNode(PathNode node, Color color)
        {
            Vector3 worldPos = node.WorldPosition(gridManager);
            Debug.DrawRay(worldPos + Vector3.up * 0.1f, Vector3.up * 0.5f, color, nodeVisualizationDuration);
        }
        
        private IEnumerator VisualizeFinalPath(List<Vector3> path)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 from = path[i] + Vector3.up * 0.2f;
                Vector3 to = path[i + 1] + Vector3.up * 0.2f;
                
                float duration = 0.1f;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    Debug.DrawLine(from, to, pathColor);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }
        
        /// <summary>
        /// Get all walkable positions within a certain range
        /// </summary>
        public List<Vector3> GetWalkablePositionsInRange(Vector3 center, int range)
        {
            List<Vector3> positions = new List<Vector3>();
            Vector2Int centerGrid = gridManager.WorldToGrid(center);
            
            for (int x = centerGrid.x - range; x <= centerGrid.x + range; x++)
            {
                for (int z = centerGrid.y - range; z <= centerGrid.y + range; z++)
                {
                    Vector2Int checkPos = new Vector2Int(x, z);
                    if (IsValidGridPosition(checkPos) && nodeGrid[x, z].walkable)
                    {
                        positions.Add(gridManager.GridToWorld(checkPos));
                    }
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Check if a direct path exists between two points
        /// </summary>
        public bool HasDirectPath(Vector3 start, Vector3 end)
        {
            Vector2Int startGrid = gridManager.WorldToGrid(start);
            Vector2Int endGrid = gridManager.WorldToGrid(end);
            
            // Use Bresenham's line algorithm to check all tiles in the line
            List<Vector2Int> line = GetLine(startGrid, endGrid);
            
            foreach (Vector2Int pos in line)
            {
                if (!IsValidGridPosition(pos) || !nodeGrid[pos.x, pos.y].walkable)
                    return false;
            }
            
            return true;
        }
        
        private List<Vector2Int> GetLine(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> line = new List<Vector2Int>();
            
            int x0 = start.x, y0 = start.y;
            int x1 = end.x, y1 = end.y;
            
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            
            int err = dx - dy;
            
            while (true)
            {
                line.Add(new Vector2Int(x0, y0));
                
                if (x0 == x1 && y0 == y1) break;
                
                int e2 = 2 * err;
                
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
            
            return line;
        }
    }
}
