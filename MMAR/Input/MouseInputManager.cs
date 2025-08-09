
using UnityEngine;

public class MouseInputManager : MonoBehaviour
{
    InputItem lastItem;
    bool triggerClick=false;
    [SerializeField] float longClickTime = .2f;
    float lastTimeClickTriggered = 0;
    public static MouseInputManager instance;

    private void Awake() {
        instance = this;
    }
    public void MouseDown(InputItem item) {
        lastItem = item;
        triggerClick = true;
        lastTimeClickTriggered=Time.time;
        Debug.Log(item);
    }
    private void Update() {
        if (triggerClick) {
            if(Input.GetMouseButtonUp(0)) {
                lastItem.MouseClick();
                triggerClick = false;
            }
            else if(Time.time > lastTimeClickTriggered + longClickTime) {
                lastItem.MouseLongClick();
                triggerClick = false;
            }
        }
    }
}
