using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Input {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Input/Mobile Input Setter", 1)]

  //Class for setting mobile input
  public class MobileInput : MonoBehaviour {
    //Orientation the screen is locked at
    public ScreenOrientation screenRot = ScreenOrientation.LandscapeLeft;

    [NonSerialized] public float accel;
    [NonSerialized] public float brake;
    [NonSerialized] public float steer;
    [NonSerialized] public float ebrake;
    [NonSerialized] public bool boost;

    //Set screen orientation
    void Start() {
      Screen.autorotateToPortrait = this.screenRot == ScreenOrientation.Portrait
                                    || this.screenRot == ScreenOrientation.AutoRotation;
      Screen.autorotateToPortraitUpsideDown = this.screenRot == ScreenOrientation.PortraitUpsideDown
                                              || this.screenRot == ScreenOrientation.AutoRotation;
      Screen.autorotateToLandscapeRight = this.screenRot == ScreenOrientation.LandscapeRight
                                          || this.screenRot == ScreenOrientation.Landscape
                                          || this.screenRot == ScreenOrientation.AutoRotation;
      Screen.autorotateToLandscapeLeft = this.screenRot == ScreenOrientation.LandscapeLeft
                                         || this.screenRot == ScreenOrientation.Landscape
                                         || this.screenRot == ScreenOrientation.AutoRotation;
      Screen.orientation = this.screenRot;
    }

    //Input setting functions that can be linked to buttons
    public void SetAccel(float f) { this.accel = Mathf.Clamp01(f); }

    public void SetBrake(float f) { this.brake = Mathf.Clamp01(f); }

    public void SetSteer(float f) { this.steer = Mathf.Clamp(f, -1, 1); }

    public void SetEbrake(float f) { this.ebrake = Mathf.Clamp01(f); }

    public void SetBoost(bool b) { this.boost = b; }
  }
}
