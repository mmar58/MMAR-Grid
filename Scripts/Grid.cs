namespace MMAR.Grid {
    using UnityEngine;
    using AdvancedEditorTools.Attributes;

    public class Grid : MonoBehaviour {
        public bool debugThis;
        [BeginFoldout("Grid Parameters")]
        public int width;
        public int height;
        [SerializeField] Transform gridGroundParent,gridObjectParent;
        [SerializeField] Vector3 gridStartPoint;
        [EndFoldout]
        [BeginFoldout("GameObjects")]
        public GameObject gridGroundNormal;


        #region Monobehavior life cycles
        private void Start() {
            //Setting all the grid objects on grid
            GatherAlreadyCreatedGridObject();
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
                    GameObject gridGameObject;
                    if (gridGroundParent !=null) {
                        gridGameObject = Instantiate(gridGroundNormal,gridGroundParent);
                    }
                    else {
                        gridGameObject = Instantiate(gridGroundNormal);
                    }
                    gridGameObject.transform.position = new(gridStartPoint.x + i, gridStartPoint.y, gridStartPoint.z + j);
                    gridGameObject.name = "Grid " + i + "," + j;
                }
            }
        }
        [Button("Gather Already Created Grid Objects in Grid")]
        public void GatherAlreadyCreatedGridObject() {
            if(gridObjectParent != null) {
                GridObject[] gridObjects=gridObjectParent.GetComponentsInChildren<GridObject>();
                foreach (var gridObject in gridObjects) {
                    if (!gridObject.listedInGrid) {
                        gridObject.gridPosition=gridObject.transform.position;
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
    }
}