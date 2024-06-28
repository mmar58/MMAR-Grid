
using UnityEngine;

public class InputItem : MonoBehaviour
{
    public virtual void MouseClick() {
#if UNITY_EDITOR
        Debug.Log("Mouse clicked " + gameObject.name);
#endif
    }
    public virtual void MouseLongClick() {
#if UNITY_EDITOR
        Debug.Log("Mouse long clicked " + gameObject.name);
#endif
    }
}
