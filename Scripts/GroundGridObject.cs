using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundGridObject : MonoBehaviour
{
    private void OnMouseOver() {
        if (MMAR.Grid.Grid.instance != null && MMAR.Grid.Grid.instance.draggedGameObject != null) {
            MMAR.Grid.Grid.instance.draggedGameObject.transform.position=transform.position;
        }
    }
}
