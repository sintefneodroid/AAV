using UnityEngine;

namespace AAV.CarChase.Scripts.Camera {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Camera/Basic Camera Input", 1)]

  //Class for setting the camera input with the input manager
  public class BasicCameraInput : MonoBehaviour {
    CameraControl cam;
    public string xInputAxis;
    public string yInputAxis;

    void Start() {
      //Get camera controller
      this.cam = this.GetComponent<CameraControl>();
    }

    void FixedUpdate() {
      //Set camera rotation input if the input axes are valid
      if (this.cam && !string.IsNullOrEmpty(this.xInputAxis) && !string.IsNullOrEmpty(this.yInputAxis)) {
        this.cam.SetInput(UnityEngine.Input.GetAxis(this.xInputAxis),
                          UnityEngine.Input.GetAxis(this.yInputAxis));
      }
    }
  }
}
