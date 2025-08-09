
using UnityEngine;

namespace MMAR.GridSystem.GridObjectAnimation {
    public class ReachBottomAnimation : BaseAnimationClass {
        float targetBottom;
        float movingSpeed = 2f;
        public ReachBottomAnimation(GridObject gridObject) : base(gridObject) {
            targetBottom = gridObject.gridPosition.y+gridObject.yoffset;
        }
        public override void Update() {
            if(!animationDone) {
                var tempPosition=gridObject.transform.position;
                if(tempPosition.y> targetBottom) {
                    tempPosition.y-=movingSpeed*Time.deltaTime;
                }
                if (tempPosition.y <= targetBottom) {
                    animationDone = true;
                    tempPosition.y = targetBottom;
                }
                gridObject.transform.position = tempPosition;
            }
        }
    }
}
