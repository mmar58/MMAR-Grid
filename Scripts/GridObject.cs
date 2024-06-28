namespace MMAR.Grid {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class GridObject : InputItem {
        public Vector3 gridPosition;
        public bool listedInGrid;
        public bool dragEnabled;
        public bool currentlyDragged;
        public GroundGridObject groundGridObject;
        /// <summary>
        /// How much should elivate while dragging.<br/>
        /// It's important show ground.
        /// </summary>
        public float dragElivate = 1;

        #region Monobehavior life cycle

        #endregion
        private void OnMouseDown() {
            if(MouseInputManager.instance != null) {
                MouseInputManager.instance.MouseDown(this);
            }
        }
        public override void MouseLongClick() {
            base.MouseLongClick();
            if(Grid.instance != null ) {
                Grid.instance.DragTheObject(this);
            }
        }
        public void OnDraggedStarted() {
            currentlyDragged = true;
            var tempPosition=transform.position;
            tempPosition.y += dragElivate;
            transform.position = tempPosition;
        }
        public void OnDraggedFinished() {
            currentlyDragged=false;
            var tempPosition=transform.position;
            tempPosition.y -= dragElivate;
            transform.position = tempPosition;
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
