namespace MMAR.Grid {
    using UnityEngine;
    using AdvancedEditorTools.Attributes;
    using System.Collections.Generic;
    using UnityEngine.Events;
    using System.Collections;
    using Shared.Extensions;
    using MMAR.Util;

    public class Grid : MonoBehaviour {
        public bool debugThis;
        [BeginFoldout("Grid Parameters")]
        public int width;
        public int height;
        [SerializeField] Transform gridGroundParent,gridObjectParent;
        [SerializeField] Vector3 gridStartPoint;
        [EndFoldout]
        [BeginFoldout("Unity  Actions")]
        public UnityEvent onDragStarted;
        public UnityEvent onDragFinished;
        [EndFoldout]
        [BeginFoldout("GameObjects")]
        public GroundGridObject gridGroundNormal;
        //Grid game objects list
        public Dictionary<Vector3,GroundGridObject> groundGridObjects=new();
        [EndFoldout]
        [BeginFoldout("Specific used game objects")]
        public GridObject draggedGameObject;
        [EndFoldout]
        [BeginFoldout("Materials")]
        public Material gridGroundNormalMaterial;
        public Material gridGroundAllowMaterial;
        public Material gridGroundNotPossinle;
        [EndFoldout]
        [BeginFoldout("Other Properties")]
        public float DoubleClickTime = .1f;
        [EndFoldout]
        public static Grid instance;
        #region Monobehavior life cycles

        private void Awake() {
            instance = this;
        }
        private void Start() {
            //Setting all the grid objects on grid
            GatherAlreadyCreatedGridObject();
            //If grid ground dictionary have less item, update it
            if(gridGroundParent != null&&groundGridObjects.Count<gridGroundParent.childCount) {
                CollectGroundGridObjects();
            }
        }
        #endregion
        [Button("Generate Grid Grid")]
        public void GenerateGridGround() {
            #region Clearing previous grid items from grid parent
            if(gridGroundParent != null) {
                while(gridGroundParent.childCount > 0) {
                    DestroyImmediate(gridGroundParent.GetChild(0).gameObject);
                }
            }
            #endregion
            var gridStartPoint = new Vector3(this.gridStartPoint.x, this.gridStartPoint.y, this.gridStartPoint.z);
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    GroundGridObject gridGameObject;
                    if (gridGroundParent !=null) {
                        gridGameObject = Instantiate(gridGroundNormal,gridGroundParent);
                    }
                    else {
                        gridGameObject = Instantiate(gridGroundNormal);
                    }
                    gridGameObject.transform.position = new(gridStartPoint.x + i, gridStartPoint.y, gridStartPoint.z + j);
                    gridGameObject.name = "Grid " + i + "," + j;
                    groundGridObjects.Add(gridGameObject.transform.position, gridGameObject);
                }
            }
        }
        [Button("Collext Ground Grid Objects in Grid")]
        void CollectGroundGridObjects() {
            if(gridGroundParent!=null) {
                this.groundGridObjects.Clear();
                GroundGridObject[] groundGridObjects = gridGroundParent.GetComponentsInChildren<GroundGridObject>();
                foreach(var groundGridObject in groundGridObjects) {
                    this.groundGridObjects.Add(groundGridObject.transform.position, groundGridObject);
                }
                Debug.Log("Collected "+this.groundGridObjects.Count+" ground grid objects");
            }
        }
        #region Get the grid objects
        [Button("Gather Already Created Grid Objects in Grid")]
        void GatherAlreadyCreatedGridObject() {
            if(gridObjectParent != null) {
                GridObject[] gridObjects=gridObjectParent.GetComponentsInChildren<GridObject>();
                foreach (var gridObject in gridObjects) {
                    if (!gridObject.listedInGrid) {
                        gridObject.gridPosition = gridObject.transform.position;
                        GroundGridObject groundGridObject;
                        if(groundGridObjects.TryGetValue(gridObject.gridPosition, out groundGridObject)) {
                            gridObject.groundGridObject = groundGridObject;
                            groundGridObject.onGridObject = gridObject;
                        }
                        
                        
                        GetTheObjectGridPoint(gridObject);
                    }
                }
            }
            else {
                Debug.LogWarning("Grid Object Parent is null");
            }
        }

        public void GetTheObjectGridPoint(GridObject gridObject) {
            #region Setting x
            if (XInOfRange(gridObject.gridPosition.x)) {
                var previousX = gridStartPoint.x;
                for (int i = 1; i < width; i++) {
                    var nextX = previousX + 1;
                    if (gridObject.gridPosition.x <= nextX) {
                        if (MathHelper.Difference(previousX, gridObject.gridPosition.x) < MathHelper.Difference(nextX, gridObject.gridPosition.x)) {
                            gridObject.gridPosition.x = previousX;
                        }
                        else {
                            gridObject.gridPosition.x = nextX;
                        }
                        break;
                    }
                    else {
                        previousX = nextX;
                    }
                }
            }
            else {
                if (gridObject.gridPosition.x < gridStartPoint.x) {
                    gridObject.gridPosition.x = gridStartPoint.x;
                }
                else {
                    gridObject.gridPosition.x = gridStartPoint.x + width - 1;
                }
            }
            #endregion
            #region Setting Z
            if (ZInOfRange(gridObject.gridPosition.z)) {
                var previousZ = gridStartPoint.z;
                for (int i = 1; i < height; i++) {
                    var nextZ = previousZ + 1;
                    if (gridObject.gridPosition.z <= nextZ) {
                        if (MathHelper.Difference(previousZ, gridObject.gridPosition.z) < MathHelper.Difference(nextZ, gridObject.gridPosition.z)) {
                            gridObject.gridPosition.z = previousZ;
                        }
                        else {
                            gridObject.gridPosition.z = nextZ;
                        }
                        break;
                    }
                    else {
                        previousZ = nextZ;
                    }
                }
            }
            else {
                if (gridObject.gridPosition.z < gridStartPoint.z) {
                    gridObject.gridPosition.z = gridStartPoint.z;
                }
                else {
                    gridObject.gridPosition.z = gridStartPoint.z + width - 1;
                }
            }
            #endregion
            gridObject.transform.position = gridObject.gridPosition;
            gridObject.listedInGrid = true;
        }
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
        public void DragTheObject(GridObject gridObject) {
            draggedGameObject = gridObject;
            gridObject.OnDraggedStarted();
            onDragStarted.Invoke();
        }
        public void FinishDragging() {
            if(draggedGameObject != null) {
                draggedGameObject.OnDraggedFinished();
                onDragFinished.Invoke();
                draggedGameObject=null;
            }
        }

        public void DraggedTo(GroundGridObject groundGridObject) {
            draggedGameObject.transform.position=groundGridObject.transform.position;
        }
    }
}
