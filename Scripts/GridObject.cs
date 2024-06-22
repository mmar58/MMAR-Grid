namespace MMAR.Grid {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class GridObject : MonoBehaviour {
        public Vector3 gridPosition;
        public bool listedInGrid;
        public bool dragEnabled;
        public bool currentlyDragged;
        private void Start() {
        }
        private void OnMouseDown() {
            MMAR.Grid.Grid.instance.draggedGameObject = this;
            currentlyDragged = true;
        }
        private void OnMouseUp() {
            if (currentlyDragged) {
                currentlyDragged = false;
                MMAR.Grid.Grid.instance.draggedGameObject = null;
            }
        }
    }

}
