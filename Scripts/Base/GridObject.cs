namespace MMAR.GridSystem {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using NaughtyAttributes;
    public class GridObject : InputItem {
        public Vector2Int gridPosition;
        public bool listedInGrid;
        public bool dragEnabled;
        public bool currentlyDragged;
        public string gridObjectKey="";
        public bool newObject = false;
        /// <summary>
        /// How much should elivate while dragging.<br/>
        /// It's important show ground.
        /// </summary>
        public float yoffset = .2f;
        public float dragElivate = 1;

        #region Monobehavior life cycle
        public virtual void Start() {
            if (!listedInGrid)
            {
                PlaceToNearestGrid();
            }
        }
        #endregion
        private void OnMouseDown() {
            newObject = false;
            if(MouseInputManager.instance != null) {
                MouseInputManager.instance.MouseDown(this);
            }
        }
        public override void MouseLongClick() {
            base.MouseLongClick();
            if (dragEnabled)
            {
                if (GridManager.instance != null)
                {
                    GridManager.instance.DragTheObject(this);
                }
            }
        }
        public void OnDraggedStarted() {
            currentlyDragged = true;
        }
        public void OnDraggedFinished() {
            currentlyDragged=false;
            Vector3 tempSearchPosition= new(transform.position.x, 0, transform.position.z);
            gridPosition = GridManager.instance.WorldToGrid(tempSearchPosition);
            GridManager.instance.ClearLastGridGroundColor();
            Debug.Log("Dragging finished");
        }
        [Button]
        public void PlaceToNearestGrid() {
            if(GridManager.instance != null) {
                Vector3 tempSearchPosition = new(transform.position.x, 0, transform.position.z);
                gridPosition = GridManager.instance.WorldToGrid(tempSearchPosition);
                var groundObject = GridManager.instance.groundGridObjects[gridPosition];
                if (groundObject != null) {
                    if(groundObject.onGridObject == null) {
                        if(GridManager.instance.gridObjectParent != null) {
                            transform.SetParent(GridManager.instance.gridObjectParent);
                        }
                        groundObject.onGridObject = this;
                        transform.position = GridManager.instance.GridToWorld(gridPosition) + new Vector3(0, yoffset, 0);
                        listedInGrid = true;
                        newObject = false;
                    } else {
                        Debug.LogError("Ground object already occupied at grid position: " + gridPosition);
                    }
                } else {
                    Debug.LogError("Ground object not found at grid position: " + gridPosition);
                }
            } else {
                Debug.LogError("GridManager instance is null.");
            }
        }
    }
}
