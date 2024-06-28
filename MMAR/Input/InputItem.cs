
using UnityEngine;

public class InputItem : MonoBehaviour
{
    public void MouseClick() {
#if UNITY_EDITOR
        Debug.Log("Mouse clicked " + gameObject.name);
#endif
    }
    public void MouseLongClick() {
#if UNITY_EDITOR
        Debug.Log("Mouse long clicked " + gameObject.name);
#endif
    }
}
