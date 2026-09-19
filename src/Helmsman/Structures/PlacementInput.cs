#nullable disable
using UnityEngine;

namespace Helmsman.Structures
{
    public sealed partial class PlacementTool
    {
        private int backInputFrame=-1;
        private float wheelRotation,nextRotation;
        private int heldRotation;
        internal bool ReadingPlacementInput {get;private set;}
        internal bool Positioning=>preview&&!confirm&&!busy;
        internal bool ControllerRotating=>Positioning&&ZInput.IsGamepadActive()&&(ZInput.GetButton("JoyRotate")||ZInput.GetButton("JoyRotateRight"));
        // Menu.Update may run either before or after our Update. Keep the consumed
        // frame blocked even when Back closes the last POI panel on that frame.
        internal bool SuppressGameMenu=>browser||confirm||preview||busy||backInputFrame==Time.frameCount;
        private bool HandleBackInput()
        {
            if(!(browser||confirm||preview||busy)||backInputFrame==Time.frameCount)return false;
            if(!ZInput.GetKeyDown(KeyCode.Escape)&&!ZInput.GetButtonDown("JoyButtonB"))return false;
            backInputFrame=Time.frameCount;
            ZInput.ResetButtonStatus("JoyButtonB");
            if(busy)return true;
            if(preview||confirm)
            {
                ResetPreview();confirm=false;browser=true;error="";menuState="";
                SetInput(true);
            }
            else {browser=false;SetInput(false);}
            return true;
        }
        private void UpdatePositioningKeys()
        {
            ReadingPlacementInput=true;
            try{UpdateRotationInput();}finally{ReadingPlacementInput=false;}
            // Valheim uses the new input backend. Unity's legacy Input.GetKeyDown
            // does not reliably receive these keys; use the same API as the game.
            if(ZInput.GetKeyDown(KeyCode.LeftBracket))RotatePreview(-1);
            if(ZInput.GetKeyDown(KeyCode.RightBracket))RotatePreview(1);
            float lift=ZInput.GetKey(KeyCode.LeftShift)||ZInput.GetKey(KeyCode.RightShift)?.1f:.5f;
            if(ZInput.GetKeyDown(KeyCode.PageUp))height+=lift;
            if(ZInput.GetKeyDown(KeyCode.PageDown))height-=lift;
            if((ZInput.GetKeyDown(KeyCode.Return)||ZInput.GetKeyDown(KeyCode.KeypadEnter))&&hit)
            {locked=true;confirm=true;SetInput(true);}
        }
        private void UpdateRotationInput()
        {
            wheelRotation+=ZInput.GetMouseScrollWheel();
            if(wheelRotation>.1f){RotatePreview(1);wheelRotation=0;}
            else if(wheelRotation<-.1f){RotatePreview(-1);wheelRotation=0;}
            int direction=0;
            if(ZInput.IsGamepadActive())
            {
                if(ZInput.InputLayout==InputLayout.Default)
                {
                    float stick=ZInput.GetJoyRightStickX();
                    if(ZInput.GetButton("JoyRotate")&&System.Math.Abs(stick)>.5f)direction=stick<0?1:-1;
                }
                else
                {
                    if(ZInput.GetButton("JoyRotate"))direction=-1;
                    else if(ZInput.GetButton("JoyRotateRight"))direction=1;
                }
            }
            if(direction==0){heldRotation=0;return;}
            if(direction!=heldRotation){RotatePreview(direction);heldRotation=direction;nextRotation=Time.unscaledTime+.25f;}
            else if(Time.unscaledTime>=nextRotation){RotatePreview(direction);nextRotation=Time.unscaledTime+.08f;}
        }
        private void ResetPositioningInput(){wheelRotation=0;heldRotation=0;nextRotation=0;}
        private void RotatePreview(int direction){yaw=Geometry.Rotate(yaw,direction);}
    }
}
