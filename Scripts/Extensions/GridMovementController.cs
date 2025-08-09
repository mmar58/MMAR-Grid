using UnityEngine;
using UnityEngine.InputSystem;
using MMAR.GridSystem;

namespace MMAR.GridSystem
{
    /// <summary>
    /// Bomber‑style tile controller with **optional diagonal** moves and smooth **look‑at rotation**.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class GridMovementController : MonoBehaviour
    {
        #region ── Inspector ───────────────────────────────────────────
        [Header("Grid Reference")]
        [SerializeField] public GridManager grid;

        [Header("Movement")]
        [SerializeField] private float unitsPerSecond = 6f;
        [Tooltip("When TRUE, releasing keys finishes the current tile; when FALSE, stops instantly.")]
        [SerializeField] private bool finishCurrentTileOnRelease = false;
        [Tooltip("Allow 8‑way movement instead of the 4 cardinal directions.")]
        [SerializeField] private bool allowDiagonal = true;

        [Header("Rotation")]
        [SerializeField] private bool rotateTowardMove = true;
        [SerializeField] private float rotationSpeed = 720f; // deg/sec

        [Header("Dash")]
        [SerializeField] private bool enableDash = true;
        [SerializeField] private int dashTiles = 2;
        [SerializeField] private float dashCooldown = 1f;
        [SerializeField] private AnimationCurve dashEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Animation")]
        [SerializeField] private Animator animator;
        #endregion

        #region ── Input / cached ──────────────────────────────────────
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction dashAction;
        #endregion

        #region ── State ───────────────────────────────────────────────
        private Vector2Int gridPos;
        private Vector3 tileCenter;
        private Vector2Int moveDir;        // −1/0/1 for each axis (diag possible)
        private Vector2Int bufferedDir;
        private bool isMoving;

        // Rotation
        private Vector3 lastFacing = Vector3.forward;

        // Dash
        private bool isDashing;
        private Vector3 dashStart, dashEnd;
        private float dashT;
        private float dashReadyAt;
        #endregion

        /*────────────────────────────────────────────────────────────*/
        #region Unity
        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            grid = grid ? grid : FindObjectOfType<GridManager>();
            if (!grid) { Debug.LogError("GridManager not found – controller disabled"); enabled = false; return; }
            WireInput();
        }

        private void Start()
        {
            gridPos = grid.WorldToGrid(transform.position);
            tileCenter = grid.GridToWorld(gridPos);
            transform.position = tileCenter;
        }

        private void Update()
        {
            if (isDashing)
            {
                DashTick();
                if (animator != null)
                {
                    animator.SetBool("Dashing", true);
                    animator.SetBool("Walking", false);
                }
            }
            else
            {
                WalkTick();
                if (animator != null)
                {
                    animator.SetBool("Dashing", false);
                    animator.SetBool("Walking", isMoving);
                }
            }
            HandleRotation();
        }
        #endregion

        /*────────────────────────────────────────────────────────────*/
        #region Input helpers
        private void WireInput()
        {
            if (playerInput?.actions == null) return;
            moveAction = playerInput.actions["Move"];
            dashAction = playerInput.actions["Dash"];
            if (dashAction != null) dashAction.performed += _ => TryBeginDash();
        }

        private Vector2Int ReadMoveVector()
        {
            Vector2 raw = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            if (raw.sqrMagnitude < 0.01f) return Vector2Int.zero;

            if (allowDiagonal)
            {
                return new Vector2Int(Mathf.RoundToInt(Mathf.Clamp(raw.x, -1f, 1f)), Mathf.RoundToInt(Mathf.Clamp(raw.y, -1f, 1f)));
            }
            // 4‑way: choose dominant axis
            return Mathf.Abs(raw.x) > Mathf.Abs(raw.y)
                ? new Vector2Int((int)Mathf.Sign(raw.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(raw.y));
        }
        #endregion

        /*────────────────────────────────────────────────────────────*/
        #region Walk logic
        private void WalkTick()
        {
            Vector2Int inputDir = ReadMoveVector();

            // ─ direction selection / queuing
            if (!isMoving)
            {
                if (inputDir != Vector2Int.zero) TryStep(inputDir);
            }
            else // already walking
            {
                if (inputDir != Vector2Int.zero && inputDir != moveDir)
                    bufferedDir = inputDir;

                if (!finishCurrentTileOnRelease && inputDir == Vector2Int.zero)
                {
                    isMoving = false;
                    bufferedDir = Vector2Int.zero;
                    tileCenter = transform.position;
                    gridPos = grid.WorldToGrid(tileCenter);
                }
            }

            // ─ movement lerp
            if (isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, tileCenter, unitsPerSecond * Time.deltaTime);
                if ((transform.position - tileCenter).sqrMagnitude < 1e-6f)
                {
                    transform.position = tileCenter;
                    isMoving = false;
                    gridPos = grid.WorldToGrid(tileCenter);

                    if (bufferedDir != Vector2Int.zero)
                    {
                        TryStep(bufferedDir);
                        bufferedDir = Vector2Int.zero;
                    }
                }
            }
        }

        private void TryStep(Vector2Int dir)
        {
            if (!allowDiagonal && dir.x != 0 && dir.y != 0) return; // guard

            Vector2Int next = gridPos + dir;
            if (!grid.IsValidGridPosition(next) || IsBlocked(next)) return;

            // Diagonal safety: ensure adjacent cardinal tiles open, avoids corner‑cut
            if (allowDiagonal && dir.x != 0 && dir.y != 0)
            {
                Vector2Int xCheck = gridPos + new Vector2Int(dir.x, 0);
                Vector2Int yCheck = gridPos + new Vector2Int(0, dir.y);
                if (IsBlocked(xCheck) || IsBlocked(yCheck)) return;
            }

            moveDir = dir;
            isMoving = true;
            tileCenter = grid.GridToWorld(next);
        }
        #endregion

        /*────────────────────────────────────────────────────────────*/
        #region Dash
        private void TryBeginDash()
        {
            if (!enableDash || isDashing || Time.time < dashReadyAt) return;
            Vector2Int heading = moveDir != Vector2Int.zero ? moveDir : ReadMoveVector();
            if (heading == Vector2Int.zero) return;

            Vector2Int dst = gridPos;
            for (int i = 0; i < dashTiles; i++)
            {
                Vector2Int step = dst + heading;
                if (!grid.IsValidGridPosition(step) || IsBlocked(step)) break;
                if (allowDiagonal && heading.x != 0 && heading.y != 0)
                {
                    if (IsBlocked(dst + new Vector2Int(heading.x, 0)) || IsBlocked(dst + new Vector2Int(0, heading.y))) break;
                }
                dst = step;
            }
            if (dst == gridPos) return;

            dashStart = transform.position;
            dashEnd = grid.GridToWorld(dst);
            dashT = 0f;
            isDashing = true;
            gridPos = dst;
            bufferedDir = Vector2Int.zero;
        }

        private void DashTick()
        {
            dashT += (unitsPerSecond * 2f) * Time.deltaTime / Vector3.Distance(dashStart, dashEnd);
            float t = dashEase.Evaluate(dashT);
            transform.position = Vector3.LerpUnclamped(dashStart, dashEnd, t);
            if (t >= 1f)
            {
                isDashing = false;
                dashReadyAt = Time.time + dashCooldown;
                tileCenter = transform.position;
            }
        }
        #endregion

        /*────────────────────────────────────────────────────────────*/
        #region Rotation
        private void HandleRotation()
        {
            if (!rotateTowardMove) return;
            Vector3 faceDir = isDashing ? (dashEnd - dashStart).normalized : new Vector3(moveDir.x, 0, moveDir.y);
            if (faceDir.sqrMagnitude < 0.001f) faceDir = lastFacing;
            else lastFacing = faceDir;

            Quaternion targetRot = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
        #endregion

        /*────────────────────────────────────────────────────────────*/
        #region Helpers
        private bool IsBlocked(Vector2Int gp)
        {
            if (grid.groundGridObjects.TryGetValue(gp, out GridGroundObject ggo))
            {
                if (ggo.onGridObject != null && ggo.onGridObject.gameObject != this.gameObject) return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the grid position directly in front of the player based on facing direction
        /// </summary>
        public Vector2Int GetFrontGridPosition()
        {
            Vector2Int facingDir = GetFacingDirection();
            return gridPos + facingDir;
        }

        /// <summary>
        /// Gets the current facing direction as a Vector2Int
        /// </summary>
        public Vector2Int GetFacingDirection()
        {
            // If currently moving, use movement direction
            if (isMoving || isDashing)
            {
                return moveDir;
            }
            
            // If not moving, derive from lastFacing vector
            Vector2Int facingDir = new Vector2Int(
                Mathf.RoundToInt(lastFacing.x),
                Mathf.RoundToInt(lastFacing.z)
            );
            
            // Ensure we have a valid direction (default to forward if none)
            if (facingDir == Vector2Int.zero)
            {
                facingDir = new Vector2Int(0, 1); // Default to forward (positive Z)
            }
            
            return facingDir;
        }

        /// <summary>
        /// Gets the current grid position of the player
        /// </summary>
        public Vector2Int CurrentGridPosition => gridPos;

        /// <summary>
        /// Checks if the front position is valid and not blocked
        /// </summary>
        public bool IsFrontPositionClear()
        {
            Vector2Int frontPos = GetFrontGridPosition();
            return grid.IsValidGridPosition(frontPos) && !IsBlocked(frontPos);
        }
        #endregion
    }
}
