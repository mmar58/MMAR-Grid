
namespace MMAR.GridSystem.GridObjectAnimation {
    public class BaseAnimationClass {
        public GridObject gridObject;
        public bool animationDone=false;
        public BaseAnimationClass(GridObject gridObject) {
            this.gridObject = gridObject;
            Reset();
        }
        public virtual void Reset() {
            animationDone = false;
        }
        public virtual void Update() {

        }
    }
}
