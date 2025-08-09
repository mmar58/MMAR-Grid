
using UnityEngine;

namespace MMAR.GridSystem.GridObjectAnimation {
    public class ReachTopAnimation : BaseAnimationClass {
        float targetHeight;
        float movingSpeed = 2f;
        public ReachTopAnimation(GridObject gridObject) : base(gridObject) {
            targetHeight= gridObject.gridPosition.y+gridObject.dragElivate;
        }
        public override void Update() {
            if(!animationDone) {
                var tempPosition=gridObject.transform.position;
                if(tempPosition.y<targetHeight) {
                    tempPosition.y+=movingSpeed*Time.deltaTime;
                }
                if (tempPosition.y >= targetHeight) {
                    animationDone = true;
                    tempPosition.y = targetHeight;
                }
                gridObject.transform.position = tempPosition;
            }
        }
    }
}
