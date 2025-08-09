using MMAR.GridSystem;
using UnityEngine;

public class AddItemOnGrid : MonoBehaviour
{
    public GridManager gridManager;
    public GridObject gridObjectPrefab;
    public void AddItem()
    {
        gridManager.AddGridObjectToPlace(gridObjectPrefab,gridManager.GetRandomGridPosition());
    }
}
