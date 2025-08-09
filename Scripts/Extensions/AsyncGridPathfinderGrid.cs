// AsyncGridPathfinderGrid.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MMAR.GridSystem
{
    /// <summary>
    /// Grid-first A* pathfinder with:
    /// - Async scheduler (frame budget)
    /// - Multiple concurrent searches
    /// - Multi-point path (A -> B -> C -> ...)
    /// - K-shortest alternatives (small K recommended)
    /// - Walkable discovery within range (optional per-cell paths)
    ///
    /// Movement defaults to 4-way; enable 8-way via Inspector.
    /// Costs are integers (10/14) for speed. Heuristic is Manhattan (4-way) or Octile (8-way).
    /// </summary>
    public class AsyncGridPathfinderGrid : MonoBehaviour
    {
        // Reference to the grid manager that holds grid data
        [Header("References")]
        [SerializeField] private GridManager grid;

        // Scheduler settings for async pathfinding jobs
        [Header("Scheduler")]
        [Tooltip("CPU budget per frame (ms) for ALL jobs combined.")]
        [Range(0.1f, 5f)] public float frameBudgetMs = 1.5f;
        [Tooltip("Max node expansions per job, per round-robin step.")]
        [Range(8, 256)] public int stepNodesPerJob = 64;

        // Movement settings
        [Header("Movement")]
        public bool eightWay = false; // If true, allows 8-way movement
        [Tooltip("8-way only: disallow diagonal corner cutting.")]
        public bool blockCornerCut = true; // Prevents diagonal movement through corners

        // Internal grid representation
        private Node[,] nodes; // 2D array of nodes representing the grid
        private int width, height; // Grid dimensions
        private bool initialized; // Whether the grid has been initialized

        // Search stamping for safe concurrency (unique search IDs)
        private int searchIdCounter = 1;

        // Job scheduler for async pathfinding
        private readonly List<Job> jobs = new List<Job>(16); // List of active jobs
        private Coroutine schedulerRoutine; // Reference to the scheduler coroutine

        // Neighbor offsets for 4-way and 8-way movement
        private static readonly (int dx, int dz)[] OFF4 = { (0, 1), (0, -1), (-1, 0), (1, 0) };
        private static readonly (int dx, int dz)[] OFF8 = {
            (0,1),(0,-1),(-1,0),(1,0),
            (-1,1),(1,1),(-1,-1),(1,-1)
        };

        #region Types

        /// <summary>
        /// Represents a single cell/node in the grid for pathfinding.
        /// </summary>
        private sealed class Node
        {
            public int x, z; // Grid coordinates
            public bool walkable; // Whether this node is walkable

            public int g;        // Cost from start node
            public int h;        // Heuristic cost to goal
            public int f => g + h; // Total cost

            public Node parent; // Parent node in the path

            // Per-search state
            public int seenId;   // Which search last touched this node
            public bool opened;  // True if node is in open set for current search
            public bool closed;  // True if node is in closed set for current search

            public Node(int x, int z, bool walkable)
            {
                this.x = x; this.z = z; this.walkable = walkable;
            }

            /// <summary>
            /// Prepares the node for a new search by resetting state if the search ID is new.
            /// </summary>
            [System.Runtime.CompilerServices.MethodImpl(
                System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Touch(int sid)
            {
                if (seenId != sid)
                {
                    seenId = sid;
                    g = int.MaxValue;
                    h = 0;
                    parent = null;
                    opened = false;
                    closed = false;
                }
            }
        }

        /// <summary>
        /// Struct to return the result of a pathfinding request.
        /// </summary>
        public readonly struct PathResult
        {
            public readonly bool success; // True if a path was found
            public readonly List<Vector2Int> path; // The path as a list of grid positions
            public PathResult(bool ok, List<Vector2Int> p) { success = ok; path = p; }
        }

        /// <summary>
        /// Represents a single async pathfinding job.
        /// </summary>
        private sealed class Job
        {
            public int sid; // Search ID
            public Node start, goal; // Start and goal nodes
            public MinHeap open; // Open set (priority queue)
            public bool done; // True if the job is finished

            public Job(int sid, Node s, Node g, MinHeap heap)
            {
                this.sid = sid; start = s; goal = g; open = heap; done = false;
            }
        }

        /// <summary> 
        /// Min-heap keyed by f then h (tie-breaker closer to goal). Used for the open set in A*.
        /// </summary>
        private sealed class MinHeap
        {
            private readonly List<Node> data = new List<Node>(128); // Heap data
            private readonly Dictionary<Node, int> index = new Dictionary<Node, int>(128); // Node to index mapping

            public int Count => data.Count;

            public void Clear()
            {
                data.Clear();
                index.Clear();
            }

            /// <summary>
            /// Adds a node to the heap.
            /// </summary>
            public void Push(Node n)
            {
                data.Add(n);
                index[n] = data.Count - 1;
                SiftUp(data.Count - 1);
            }

            /// <summary>
            /// Removes and returns the node with the lowest f (and h) value.
            /// </summary>
            public Node Pop()
            {
                var root = data[0];
                var last = data[data.Count - 1];
                data[0] = last;
                index[last] = 0;
                data.RemoveAt(data.Count - 1);
                index.Remove(root);
                if (data.Count > 0) SiftDown(0);
                return root;
            }

            /// <summary>
            /// Updates the position of a node in the heap after its cost changes.
            /// </summary>
            public void Update(Node n)
            {
                if (!index.TryGetValue(n, out int i)) return;
                SiftUp(i);
                SiftDown(i);
            }

            // Comparison: lower f is better, then lower h as tie-breaker
            private static bool LessEqual(Node a, Node b)
            {
                if (a.f != b.f) return a.f < b.f;
                return a.h <= b.h;
            }

            // Moves a node up the heap to restore heap property
            private void SiftUp(int i)
            {
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (LessEqual(data[p], data[i])) break;
                    Swap(i, p); i = p;
                }
            }

            // Moves a node down the heap to restore heap property
            private void SiftDown(int i)
            {
                int n = data.Count;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, s = i;
                    if (l < n && !LessEqual(data[s], data[l])) s = l;
                    if (r < n && !LessEqual(data[s], data[r])) s = r;
                    if (s == i) break;
                    Swap(i, s); i = s;
                }
            }

            // Swaps two nodes in the heap and updates their indices
            private void Swap(int i, int j)
            {
                var a = data[i]; var b = data[j];
                data[i] = b; data[j] = a;
                index[b] = i; index[a] = j;
            }
        }

        #endregion

        #region Unity lifecycle

        // Ensures the grid is initialized on Awake
        private void Awake() => EnsureInit();

        // Starts the scheduler coroutine when enabled
        private void OnEnable()
        {
            if (schedulerRoutine == null) schedulerRoutine = StartCoroutine(Scheduler());
        }

        // Stops the scheduler and clears jobs when disabled
        private void OnDisable()
        {
            if (schedulerRoutine != null)
            {
                StopCoroutine(schedulerRoutine);
                schedulerRoutine = null;
            }
            jobs.Clear();
        }

        #endregion

        #region Public API (grid-first)

        /// <summary> 
        /// Re-scan the whole grid from GridManager. Call after bulk walkability changes. 
        /// </summary>
        public void RebuildGrid()
        {
            initialized = false;
            EnsureInit();
        }

        /// <summary> 
        /// Async single path request. Returns immediately; callback is invoked when done. 
        /// </summary>
        public void RequestPath(Vector2Int start, Vector2Int goal, Action<PathResult> onDone)
        {
            EnsureInit();
            if (!TryMakeJob(start, goal, out Job job))
            {
                onDone?.Invoke(new PathResult(false, EmptyPath()));
                return;
            }

            // Capture callback and add job to scheduler
            StartCoroutine(WaitForJob(job, onDone));
            jobs.Add(job);
        }

        /// <summary> 
        /// Multi-point async path: stitches A->B, B->C, ... using the scheduler. 
        /// </summary>
        public void RequestMultiPointPath(IList<Vector2Int> points, Action<PathResult> onDone)
        {
            EnsureInit();
            if (points == null || points.Count < 2) { onDone?.Invoke(new PathResult(false, EmptyPath())); return; }
            StartCoroutine(MultiPointRoutine(points, onDone));
        }

        /// <summary> 
        /// Get K alternative paths (sync). Keep K small (e.g. 2–4). 
        /// </summary>
        public List<List<Vector2Int>> FindKPaths(Vector2Int start, Vector2Int goal, int k = 3, int maxSpurTriesPerPath = 24)
        {
            EnsureInit();

            var best = FindPathSync(start, goal);
            var results = new List<List<Vector2Int>>();
            if (best.Count == 0) return results;
            results.Add(best);

            var candidates = new List<(int cost, List<Vector2Int> path)>();
            var removed = new HashSet<(int x, int z)>();

            // Yen's K-shortest paths algorithm (simplified)
            for (int ki = 1; ki < k; ki++)
            {
                var prev = results[ki - 1];
                bool gotCandidate = false;

                int spurTries = 0;
                for (int i = 0; i < prev.Count && spurTries < maxSpurTriesPerPath; i++)
                {
                    spurTries++;
                    var spur = prev[i];

                    removed.Clear();
                    for (int r = 0; r < results.Count; r++)
                    {
                        var rp = results[r];
                        if (i < rp.Count && SamePrefix(prev, rp, i))
                            removed.Add((rp[i].x, rp[i].y));
                    }

                    var spurToGoal = FindPathSync(spur, goal, removed);
                    if (spurToGoal.Count == 0) continue;

                    var combined = CombinePrefix(prev, i, spurToGoal);
                    InsertCandidate(candidates, (combined.Count, combined));
                    gotCandidate = true;
                }

                if (!gotCandidate || candidates.Count == 0) break;
                results.Add(candidates[0].path);
                candidates.RemoveAt(0);
            }

            return results;
        }

        /// <summary>
        /// Walkable cells within Manhattan 'range' from 'center'.
        /// If includePaths=true, returns a BFS path (grid coords) from center to each cell.
        /// </summary>
        public (List<Vector2Int> cells, Dictionary<Vector2Int, List<Vector2Int>> paths)
            GetWalkablesInRange(Vector2Int center, int range, bool includePaths = false)
        {
            EnsureInit();
            if (!InBounds(center)) return (new List<Vector2Int>(0), includePaths ? new Dictionary<Vector2Int, List<Vector2Int>>() : null);

            var q = new Queue<Vector2Int>(); // BFS queue
            var seen = new HashSet<Vector2Int>(); // Visited set
            var parents = includePaths ? new Dictionary<Vector2Int, Vector2Int>() : null; // For path reconstruction

            q.Enqueue(center);
            seen.Add(center);

            var cells = new List<Vector2Int>(64);

            // BFS to find all walkable cells within range
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (Manhattan(center, p) <= range && nodes[p.x, p.y].walkable)
                    cells.Add(p);

                foreach (var (dx, dz) in OFF4) // Manhattan frontier
                {
                    var n = new Vector2Int(p.x + dx, p.y + dz);
                    if (!InBounds(n) || seen.Contains(n)) continue;
                    if (Manhattan(center, n) > range) continue;

                    seen.Add(n);
                    q.Enqueue(n);
                    if (includePaths) parents[n] = p;
                }
            }

            Dictionary<Vector2Int, List<Vector2Int>> perTarget = null;
            if (includePaths)
            {
                perTarget = new Dictionary<Vector2Int, List<Vector2Int>>(cells.Count);
                foreach (var tail in cells)
                {
                    var path = new List<Vector2Int>();
                    var cur = tail;
                    while (cur != center)
                    {
                        path.Add(cur);
                        if (!parents.TryGetValue(cur, out cur)) break; // hit origin or gap
                    }
                    path.Reverse();
                    perTarget[tail] = path;
                }
            }

            return (cells, perTarget);
        }

        /// <summary> 
        /// Synchronous single A* (grid in/out). 
        /// </summary>
        public List<Vector2Int> FindPathSync(Vector2Int start, Vector2Int goal, HashSet<(int x, int z)> extraBlocked = null)
        {
            EnsureInit();
            if (!InBounds(start) || !InBounds(goal)) return EmptyPath();
            if (!Walkable(goal.x, goal.y, extraBlocked)) return EmptyPath();

            int sid = ++searchIdCounter;
            var heap = new MinHeap();

            var s = nodes[start.x, start.y];
            var g = nodes[goal.x, goal.y];

            s.Touch(sid); g.Touch(sid);
            s.g = 0; s.h = Heuristic(s, g);
            heap.Push(s);

            // Standard A* loop
            while (heap.Count > 0)
            {
                var cur = heap.Pop();
                if (cur == g) return Retrace(s, g);

                cur.closed = true;
                foreach (var n in Neighbors(cur))
                {
                    if (!Walkable(n.x, n.z, extraBlocked)) continue;
                    n.Touch(sid);
                    if (n.closed) continue;

                    int tentative = cur.g + StepCost(cur, n);
                    if (tentative < n.g)
                    {
                        n.g = tentative;
                        n.h = Heuristic(n, g);
                        n.parent = cur;

                        if (!n.opened) { heap.Push(n); n.opened = true; }
                        else heap.Update(n);
                    }
                }
            }

            return EmptyPath();
        }

        #endregion

        #region Scheduler

        /// <summary>
        /// Coroutine that schedules and steps through all active pathfinding jobs within the frame time budget.
        /// </summary>
        private IEnumerator Scheduler()
        {
            while (enabled)
            {
                float start = Time.realtimeSinceStartup;

                if (jobs.Count == 0)
                {
                    yield return null;
                    continue;
                }

                // Round-robin over active jobs within the frame budget
                for (int i = 0; i < jobs.Count; i++)
                {
                    var j = jobs[i];
                    if (j.done || j.open.Count == 0) continue;

                    int steps = 0;
                    // Step through a limited number of node expansions for this job
                    while (steps < stepNodesPerJob && j.open.Count > 0)
                    {
                        var cur = j.open.Pop();
                        if (cur == j.goal)
                        {
                            j.done = true;
                            break;
                        }

                        cur.closed = true;

                        foreach (var n in Neighbors(cur))
                        {
                            if (!nodes[n.x, n.z].walkable) continue;
                            if (eightWay && blockCornerCut && IsBlockedDiagonal(cur, n)) continue;

                            n.Touch(j.sid);
                            if (n.closed) continue;

                            int tentative = cur.g + StepCost(cur, n);
                            if (tentative < n.g)
                            {
                                n.g = tentative;
                                n.h = Heuristic(n, j.goal);
                                n.parent = cur;

                                if (!n.opened) { j.open.Push(n); n.opened = true; }
                                else j.open.Update(n);
                            }
                        }
                        steps++;
                    }

                    // If we've used up our frame budget, break out
                    if (Time.realtimeSinceStartup - start > frameBudgetMs / 1000f) break;
                }

                // Prune finished jobs
                for (int k = jobs.Count - 1; k >= 0; k--)
                    if (jobs[k].done) jobs.RemoveAt(k);

                yield return null;
            }
        }

        /// <summary>
        /// Coroutine that waits for a job to finish and then invokes the callback with the result.
        /// </summary>
        private IEnumerator WaitForJob(Job job, Action<PathResult> cb)
        {
            // Wait until the job object is marked done (scheduler will set it)
            while (!job.done && job.open.Count > 0) yield return null;

            // If goal found, it'll be at the top of parent chain for that search
            var path = job.goal.parent == null && job.start != job.goal
                     ? EmptyPath()
                     : Retrace(job.start, job.goal);

            cb?.Invoke(new PathResult(path.Count > 0, path));
        }

        /// <summary>
        /// Coroutine for multi-point pathfinding (A->B->C->...).
        /// </summary>
        private IEnumerator MultiPointRoutine(IList<Vector2Int> points, Action<PathResult> onDone)
        {
            var combined = new List<Vector2Int>(points.Count * 6);
            combined.Add(points[0]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                bool finished = false;
                List<Vector2Int> leg = null;

                RequestPath(points[i], points[i + 1], r => { leg = r.path; finished = true; });
                while (!finished) yield return null;

                if (leg == null || leg.Count == 0)
                {
                    onDone?.Invoke(new PathResult(false, combined));
                    yield break;
                }

                // stitch, skipping duplicate joint
                if (combined.Count > 0 && combined[^1] == leg[0]) combined.AddRange(leg.GetRange(1, leg.Count - 1));
                else combined.AddRange(leg);
            }

            onDone?.Invoke(new PathResult(true, combined));
        }

        /// <summary>
        /// Attempts to create a new pathfinding job for the given start and goal.
        /// </summary>
        private bool TryMakeJob(Vector2Int start, Vector2Int goal, out Job job)
        {
            job = null;
            if (!InBounds(start) || !InBounds(goal)) return false;
            if (!nodes[goal.x, goal.y].walkable) return false;

            int sid = ++searchIdCounter;

            var s = nodes[start.x, start.y];
            var g = nodes[goal.x, goal.y];

            s.Touch(sid); g.Touch(sid);
            s.g = 0; s.h = Heuristic(s, g);

            var open = new MinHeap();
            open.Push(s);

            job = new Job(sid, s, g, open);
            return true;
        }

        #endregion

        #region Core helpers

        /// <summary>
        /// Returns the walkable neighbors of a node, depending on movement mode.
        /// </summary>
        private IEnumerable<Node> Neighbors(Node n)
        {
            var OFF = eightWay ? OFF8 : OFF4;
            for (int i = 0; i < OFF.Length; i++)
            {
                int nx = n.x + OFF[i].dx;
                int nz = n.z + OFF[i].dz;
                if (!InBounds(nx, nz)) continue;
                var node = nodes[nx, nz];
                if (!node.walkable) continue;
                yield return node;
            }
        }

        /// <summary>
        /// Checks if a diagonal move is blocked by corners (for 8-way movement).
        /// </summary>
        private bool IsBlockedDiagonal(Node from, Node to)
        {
            int dx = to.x - from.x, dz = to.z - from.z;
            if (Mathf.Abs(dx) + Mathf.Abs(dz) != 2) return false; // not diagonal
            // Corner cutting rule: diagonal allowed if either adjacent cardinal is open
            bool a = InBounds(from.x + dx, from.z) && nodes[from.x + dx, from.z].walkable;
            bool b = InBounds(from.x, from.z + dz) && nodes[from.x, from.z + dz].walkable;
            return !(a || b);
        }

        /// <summary>
        /// Manhattan distance between two grid positions.
        /// </summary>
        private static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        /// <summary>
        /// Returns the step cost between two nodes (10 for straight, 14 for diagonal).
        /// </summary>
        private int StepCost(Node a, Node b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dz = Mathf.Abs(a.z - b.z);
            return (dx + dz == 2) ? 14 : 10;
        }

        /// <summary>
        /// Heuristic cost from node a to node b (Manhattan or Octile).
        /// </summary>
        private int Heuristic(Node a, Node b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dz = Mathf.Abs(a.z - b.z);
            if (!eightWay) return 10 * (dx + dz); // Manhattan
            int dMin = Mathf.Min(dx, dz);
            int dMax = Mathf.Max(dx, dz);
            return 14 * dMin + 10 * (dMax - dMin); // Octile
        }

        /// <summary>
        /// Reconstructs the path from start to goal by following parent links.
        /// </summary>
        private List<Vector2Int> Retrace(Node start, Node goal)
        {
            var path = new List<Vector2Int>(64);
            var cur = goal;
            while (cur != null && cur != start)
            {
                path.Add(new Vector2Int(cur.x, cur.z));
                cur = cur.parent;
            }
            // Optionally include the start cell:
            // path.Add(new Vector2Int(start.x, start.z));
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Checks if two paths share the same prefix up to a given index.
        /// </summary>
        private static bool SamePrefix(List<Vector2Int> a, List<Vector2Int> b, int idxInclusive)
        {
            if (b.Count <= idxInclusive) return false;
            for (int i = 0; i <= idxInclusive; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>
        /// Combines a prefix and a spur path, skipping the duplicate joint.
        /// </summary>
        private static List<Vector2Int> CombinePrefix(List<Vector2Int> prefix, int lastIdx, List<Vector2Int> spur)
        {
            var combined = new List<Vector2Int>(prefix.Count + spur.Count);
            for (int i = 0; i <= lastIdx; i++) combined.Add(prefix[i]);
            for (int j = 1; j < spur.Count; j++) combined.Add(spur[j]); // skip duplicate joint
            return combined;
        }

        /// <summary>
        /// Returns an empty path.
        /// </summary>
        private List<Vector2Int> EmptyPath() => new List<Vector2Int>(0);

        #endregion

        #region Init & grid plumbing

        /// <summary>
        /// Ensures the grid is initialized and up-to-date with walkability.
        /// </summary>
        private void EnsureInit()
        {
            if (initialized) return;

            if (grid == null)
            {
                grid = GetComponent<GridManager>();
                if (grid == null) throw new Exception("AsyncGridPathfinderGrid: GridManager reference is required.");
            }

            width = grid.width;
            height = grid.height;
            nodes = new Node[width, height];

            for (int x = 0; x < width; x++)
                for (int z = 0; z < height; z++)
                    nodes[x, z] = new Node(x, z, IsWalkable(new Vector2Int(x, z)));

            initialized = true;
        }

        /// <summary>
        /// Returns true if the given grid position is walkable (no object on it).
        /// </summary>
        public bool IsWalkable(Vector2Int gridPos)
        {
            if(grid.groundGridObjects.TryGetValue(gridPos, out var groundObject))
            {
                return groundObject.onGridObject == null;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the given grid position is within bounds.
        /// </summary>
        private bool InBounds(Vector2Int g) => InBounds(g.x, g.y);
        private bool InBounds(int x, int z) => x >= 0 && x < width && z >= 0 && z < height;

        /// <summary>
        /// Returns true if the node at (x, z) is walkable and not in extraBlocked.
        /// </summary>
        private bool Walkable(int x, int z, HashSet<(int x, int z)> extraBlocked)
        {
            if (!InBounds(x, z)) return false;
            if (extraBlocked != null && extraBlocked.Contains((x, z))) return false;
            return nodes[x, z].walkable;
        }

        #endregion

        /// <summary>
        /// Inserts a candidate path into the sorted candidate list, keeping it ordered by cost (lowest first).
        /// </summary>
        private static void InsertCandidate(List<(int cost, List<Vector2Int> path)> list, (int cost, List<Vector2Int> path) item)
        {
            // Simple linear insert to maintain ascending order by cost
            int index = list.BinarySearch(item, Comparer<(int cost, List<Vector2Int> path)>.Create((a, b) =>
            {
                int cmp = a.cost.CompareTo(b.cost);
                if (cmp != 0) return cmp;
                // Tie-break: shorter path length first
                return a.path.Count.CompareTo(b.path.Count);
            }));

            if (index < 0) index = ~index; // BinarySearch returns bitwise complement if not found
            list.Insert(index, item);
        }

    }
}
