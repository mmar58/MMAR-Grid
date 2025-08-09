namespace MMAR.GridSystem {
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEngine.Events;
    using MMAR.Util;
    using NaughtyAttributes;

    public class GridManager : MonoBehaviour {
        public bool debugThis;
        [Foldout("Grid Parameters")]
        public int width;
        [Foldout("Grid Parameters")]
        public int height;
        [Foldout("Grid Parameters")]
        public Transform gridGroundParent,gridObjectParent;
        [Foldout("Grid Parameters")]
        public Vector3 gridStartPoint; // Made public for pathfinding access
        [Foldout("Unity  Actions")]
        public UnityEvent onDragStarted;
        [Foldout("Unity  Actions")]
        public UnityEvent onDragFinished;
        [Foldout("Unity  Actions")]
        public UnityEvent<GridGroundObject> onHoverGround;
        [Foldout("Unity  Actions")]
        public UnityEvent<Vector3> onHoverGroundVector3;
        [Foldout("GameObjects")]
        public GridGroundObject gridGroundNormal;
        [Foldout("GameObjects")]
        //Grid game objects list
        public Dictionary<Vector2Int,GridGroundObject> groundGridObjects=new();
        [Foldout("Specific used game objects")]
        public GridObject draggedGameObject;
        [Foldout("Materials")]
        public Material gridGroundNormalMaterial;
        [Foldout("Materials")]
        public Material gridGroundAllowMaterial;
        [Foldout("Materials")]
        public Material gridGroundNotPossbleMaterial;
        public static GridManager instance;
        private GridGroundObject lastFloatingGround;
        public bool allowToPlaceObject = false;

        #region Grid Position Functions
        // New helper methods for coordinate conversion
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x - gridStartPoint.x);
            int z = Mathf.RoundToInt(worldPos.z - gridStartPoint.z);
            return new Vector2Int(Mathf.Clamp(x, 0, width - 1), Mathf.Clamp(z, 0, height - 1));
        }
        
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridStartPoint.x + gridPos.x, gridStartPoint.y, gridStartPoint.z + gridPos.y);
        }
        public Vector2Int GetRandomGridPosition()
        {
            int x = Random.Range(0, width);
            int z = Random.Range(0, height);
            return new Vector2Int(x, z);
        }
        public bool IsValidGridPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }
        #endregion

        #region Monobehavior life cycle
        private void Awake() {
            instance = this;
            //If grid ground dictionary have less item, update it
            if (gridGroundParent != null && groundGridObjects.Count < gridGroundParent.childCount)
            {
                CollectGroundGridObjects();
            }
        }
        private void Start() {
            
            
        }
        private void Update() {
            if(draggedGameObject != null&&!draggedGameObject.newObject&&Input.GetMouseButtonUp(0)) {
                FinishDragging();
            }
        }
        #endregion

        #region Generating Grid Ground
        [Button("Generate Grid Ground")]
        public void GenerateGridGround() {
            #region Clearing previous grid items from grid parent
            if(gridGroundParent != null) {
                while(gridGroundParent.childCount > 0) {
                    DestroyImmediate(gridGroundParent.GetChild(0).gameObject);
                }
            }
            groundGridObjects.Clear();
            #endregion
            var gridStartPoint = new Vector3(this.gridStartPoint.x, this.gridStartPoint.y, this.gridStartPoint.z);
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    GridGroundObject gridGameObject;
                    if (gridGroundParent !=null) {
                        gridGameObject = Instantiate(gridGroundNormal,gridGroundParent);
                    }
                    else {
                        gridGameObject = Instantiate(gridGroundNormal);
                    }
                    gridGameObject.transform.position = new(gridStartPoint.x + i, gridStartPoint.y, gridStartPoint.z + j);
                    gridGameObject.name = "Grid " + i + "," + j;
                    Vector2Int gridPos = new Vector2Int(i, j);
                    groundGridObjects.Add(gridPos, gridGameObject);
                }
            }
        }
        [Button("Collect Ground Grid Objects in Grid")]
        void CollectGroundGridObjects() {
            if(gridGroundParent!=null) {
                this.groundGridObjects.Clear();
                GridGroundObject[] groundGridObjectsArr = gridGroundParent.GetComponentsInChildren<GridGroundObject>();
                foreach(var groundGridObject in groundGridObjectsArr) {
                    Vector2Int gridPos = WorldToGrid(groundGridObject.transform.position);
                    this.groundGridObjects.Add(gridPos, groundGridObject);
                }
                Debug.Log("Collected "+this.groundGridObjects.Count+" ground grid objects");
            }
        }
        #endregion
        #region Get the grid objects
        /// <summary>
        /// If x is in the range of Grid width
        /// </summary>
        /// <param name="x">X / Width Value</param>
        /// <returns></returns>
        bool XInOfRange(float x) {
            DebugLog("Start Point x-" + x + ", grid  width-" + width + ", given x-" + x);
            return x>=gridStartPoint.x&&x<gridStartPoint.x+width;
        }
        /// <summary>
        /// If z is in the range of Grid height
        /// </summary>
        /// <param name="z">Z / Height Value</param>
        /// <returns></returns>
        bool ZInOfRange(float z) {
            DebugLog("Start Point z-" + z + ", grid  height-" + height + ", given z-" + z);
            return z >= gridStartPoint.z && z < gridStartPoint.z + height;
        }
        void DebugLog(object msg) {
            if (debugThis) {
                Debug.Log(msg);
            }
        }
        #endregion

        #region Placing Grid Objects
        public GridObject PlaceObjectFromPrefab(GridObject gridObjectPrefab,Vector2Int gridPosition)
        {
            if (gridObjectPrefab == null) return null;
            GridObject newGridObject;
            if(gridObjectParent != null)
            {
                newGridObject = Instantiate(gridObjectPrefab, gridObjectParent);
            }
            else
            {
                newGridObject = Instantiate(gridObjectPrefab);
            }
            newGridObject.listedInGrid = true;
            newGridObject.gridPosition = gridPosition;
            newGridObject.transform.position = new(gridStartPoint.x+newGridObject.gridPosition.x, newGridObject.yoffset,gridStartPoint.z+ newGridObject.gridPosition.y);
            var gridGroundObject = groundGridObjects[newGridObject.gridPosition];
            gridGroundObject.onGridObject = newGridObject;
            return newGridObject;
        }

        public void AddGridObjectToPlace(GridObject gridObjectPrefab, Vector2Int gridPosition)
        {
            var newGridObject = Instantiate(gridObjectPrefab, gridObjectParent);
            newGridObject.newObject = true;
            newGridObject.gridPosition = gridPosition;
            newGridObject.transform.position = new(gridStartPoint.x + newGridObject.gridPosition.x, newGridObject.yoffset, gridStartPoint.z + newGridObject.gridPosition.y);
            draggedGameObject = newGridObject;
            draggedGameObject.OnDraggedStarted();
            ChangeGroundGridColor(groundGridObjects[newGridObject.gridPosition]);
        }
        #endregion
        #region Dragging functions
        public void DragTheObject(GridObject gridObject) {
            draggedGameObject = gridObject;
            gridObject.OnDraggedStarted();
            //Changing the color of ground grid object
            ChangeGroundGridColor(groundGridObjects[gridObject.gridPosition]);
            onDragStarted.Invoke();
        }
        public void FinishDragging() {
            if(draggedGameObject != null&&GridManager.instance.allowToPlaceObject) {
                draggedGameObject.OnDraggedFinished();
                onDragFinished.Invoke();
                draggedGameObject=null;
            }
        }

        public void DraggedTo(GridGroundObject groundGridObject) {
            //var tempPosition = groundGridObject.transform.position;
            //tempPosition.y = draggedGameObject.transform.position.y;
            //draggedGameObject.transform.position = tempPosition;
            ChangeGroundGridColor(groundGridObject);
        }
        #endregion

        

        #region Changing materials of the ground
        public void ChangeGroundGridColor(GridGroundObject groundGridObject) {
            ClearLastGridGroundColor();
            lastFloatingGround = groundGridObject;
            //lastFloatingGround.outline.enabled = true;
            //if(lastFloatingGround.onGridObject != null) {
            //    if(lastFloatingGround.onGridObject==draggedGameObject) {
            //        lastFloatingGround.meshRenderer.material = gridGroundAllowMaterial;
            //        allowToPlaceObject = true;
            //    }
            //    else {
            //        lastFloatingGround.meshRenderer.material = gridGroundNotPossbleMaterial;
            //        allowToPlaceObject = false;
            //    }
            //}
            //else {
            //    lastFloatingGround.meshRenderer.material = gridGroundAllowMaterial;
            //    allowToPlaceObject = true;
            //}
        }
        public void ClearLastGridGroundColor() {
            if (lastFloatingGround != null)
            {
                //lastFloatingGround.meshRenderer.material = gridGroundNormalMaterial;
                //lastFloatingGround.outline.enabled = false;
            }
        }
        #endregion

        [Button]
        void SetThisGridAsInstance()
        {
            instance = this;
        }
    }
}
