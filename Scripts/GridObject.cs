namespace MMAR.Grid {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class GridObject : MonoBehaviour {
        public Vector3 gridPosition;
        public bool listedInGrid;
        public bool dragEnabled;
        public bool currentlyDragged;
        public GroundGridObject groundGridObject;
        private void Start() {
        }
        private void OnMouseDown() {
            MMAR.Grid.Grid.instance.DragTheObject(this);
        }
        private void OnMouseUp() {
            if (currentlyDragged) {
                MMAR.Grid.Grid.instance.FinishDragging();
            }
        }
        public void OnDraggedStarted() {
            currentlyDragged = true;
            if(groundGridObject != null) {
                groundGridObject.onGridObject = null;
            }
        }
        public void OnDraggedFinished() {
            currentlyDragged=false;
            gridPosition=transform.position;
            if(MMAR.Grid.Grid.instance.groundGridObjects.TryGetValue(gridPosition, out groundGridObject)) {
                groundGridObject.onGridObject = this;
            }
            else {
                Debug.Log("No ground grid object found at " + gridPosition);
            }
        }
    }

}
