namespace MMAR.Grid {
    using System.Collections;
    using System.Collections.Generic;
    using MMAR.Grid.GridObjectAnimation;
    using UnityEngine;

    public class GridObject : InputItem {
        public Vector3 gridPosition;
        public bool listedInGrid;
        public bool dragEnabled;
        public bool currentlyDragged;
        public GroundGridObject groundGridObject;
        BaseAnimationClass gridObjectAnimation;
        /// <summary>
        /// How much should elivate while dragging.<br/>
        /// It's important show ground.
        /// </summary>
        public float dragElivate = 1;

        #region Monobehavior life cycle
        private void Start() {
            gridObjectAnimation = new(this);
        }
        private void Update() {
            gridObjectAnimation.Update();
        }
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
            gridObjectAnimation=new ReachTopAnimation(this);
        }
        public void OnDraggedFinished() {
            currentlyDragged=false;
            gridPosition = new(transform.position.x,gridPosition.y,transform.position.z);
            Vector3 tempSearchPosition= new(transform.position.x, 0, transform.position.z);
            gridObjectAnimation =new ReachBottomAnimation(this);
            #region Clearing from the memory of ground object
            if (groundGridObject != null) {
                groundGridObject.onGridObject = null;
            }
            #endregion
            if (MMAR.Grid.Grid.instance.groundGridObjects.TryGetValue(tempSearchPosition, out groundGridObject)) {
                groundGridObject.onGridObject = this;
            }
            else {
                Debug.Log("No ground grid object found at " + gridPosition);
            }
        }
    }
}
