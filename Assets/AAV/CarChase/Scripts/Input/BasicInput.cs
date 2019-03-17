using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Input {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Input/Basic Input", 0)]

  //Class for setting the input with the input manager
  public class BasicInput : MonoBehaviour {
    VehicleParent vp;
    public string accelAxis;
    public string brakeAxis;
    public string steerAxis;
    public string ebrakeAxis;
    public string boostButton;
    public string upshiftButton;
    public string downshiftButton;
    public string pitchAxis;
    public string yawAxis;
    public string rollAxis;

    void Start() { this.vp = this.GetComponent<VehicleParent>(); }

    void Update() {
      //Get single-frame input presses
      if (!string.IsNullOrEmpty(this.upshiftButton)) {
        if (UnityEngine.Input.GetButtonDown(this.upshiftButton)) {
          this.vp.PressUpshift();
        }
      }

      if (!string.IsNullOrEmpty(this.downshiftButton)) {
        if (UnityEngine.Input.GetButtonDown(this.downshiftButton)) {
          this.vp.PressDownshift();
        }
      }
    }

    void FixedUpdate() {
      //Get constant inputs
      if (!string.IsNullOrEmpty(this.accelAxis)) {
        this.vp.SetAccel(UnityEngine.Input.GetAxis(this.accelAxis));
      }

      if (!string.IsNullOrEmpty(this.brakeAxis)) {
        this.vp.SetBrake(UnityEngine.Input.GetAxis(this.brakeAxis));
      }

      if (!string.IsNullOrEmpty(this.steerAxis)) {
        this.vp.SetSteer(UnityEngine.Input.GetAxis(this.steerAxis));
      }

      if (!string.IsNullOrEmpty(this.ebrakeAxis)) {
        this.vp.SetEbrake(UnityEngine.Input.GetAxis(this.ebrakeAxis));
      }

      if (!string.IsNullOrEmpty(this.boostButton)) {
        this.vp.SetBoost(UnityEngine.Input.GetButton(this.boostButton));
      }

      if (!string.IsNullOrEmpty(this.pitchAxis)) {
        this.vp.SetPitch(UnityEngine.Input.GetAxis(this.pitchAxis));
      }

      if (!string.IsNullOrEmpty(this.yawAxis)) {
        this.vp.SetYaw(UnityEngine.Input.GetAxis(this.yawAxis));
      }

      if (!string.IsNullOrEmpty(this.rollAxis)) {
        this.vp.SetRoll(UnityEngine.Input.GetAxis(this.rollAxis));
      }

      if (!string.IsNullOrEmpty(this.upshiftButton)) {
        this.vp.SetUpshift(UnityEngine.Input.GetAxis(this.upshiftButton));
      }

      if (!string.IsNullOrEmpty(this.downshiftButton)) {
        this.vp.SetDownshift(UnityEngine.Input.GetAxis(this.downshiftButton));
      }
    }
  }
}
