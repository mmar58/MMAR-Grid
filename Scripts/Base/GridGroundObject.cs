namespace MMAR.GridSystem {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class GridGroundObject : MonoBehaviour {
        public GridObject onGridObject;
        
        private void OnMouseOver() {
            GridManager.instance.onHoverGround.Invoke(this);
            GridManager.instance.onHoverGroundVector3.Invoke(transform.position);
            GridManager.instance.DraggedTo(this);
        }
    }
}
