using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using UnityEngine;

namespace AAV.CarChase.Scripts.Vehicle_Control {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Vehicle Controllers/Steering Control", 2)]

  //Class for steering vehicles
  public class SteeringControl : MonoBehaviour {
    Transform tr;
    VehicleParent vp;
    public float steerRate = 0.1f;
    float steerAmount;

    [Tooltip("Curve for limiting steer range based on speed, x-axis = speed, y-axis = multiplier")]
    public AnimationCurve steerCurve = AnimationCurve.Linear(0, 1, 30, 0.1f);

    public bool limitSteer = true;

    [Tooltip("Horizontal stretch of the steer curve")]
    public float steerCurveStretch = 1;

    public bool applyInReverse = true; //Limit steering in reverse?
    public Suspension.Suspension[] steeredWheels;

    [Header("Visual")] public bool rotate;
    public float maxDegreesRotation;
    public float rotationOffset;
    float steerRot;

    void Start() {
      this.tr = this.transform;
      this.vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(this.tr);
      this.steerRot = this.rotationOffset;
    }

    void FixedUpdate() {
      var rbSpeed = this.vp.localVelocity.z / this.steerCurveStretch;
      var steerLimit = this.limitSteer
                           ? this.steerCurve.Evaluate(this.applyInReverse ? Mathf.Abs(rbSpeed) : rbSpeed)
                           : 1;
      this.steerAmount = this.vp.steerInput * steerLimit;

      //Set steer angles in wheels
      foreach (var curSus in this.steeredWheels) {
        curSus.steerAngle = Mathf.Lerp(curSus.steerAngle,
                                       this.steerAmount
                                       * curSus.steerFactor
                                       * (curSus.steerEnabled ? 1 : 0)
                                       * (curSus.steerInverted ? -1 : 1),
                                       this.steerRate * TimeMaster.inverseFixedTimeFactor * Time.timeScale);
      }
    }

    void Update() {
      if (this.rotate) {
        this.steerRot = Mathf.Lerp(this.steerRot,
                                   this.steerAmount * this.maxDegreesRotation + this.rotationOffset,
                                   this.steerRate * Time.timeScale);
        this.tr.localEulerAngles =
            new Vector3(this.tr.localEulerAngles.x, this.tr.localEulerAngles.y, this.steerRot);
      }
    }
  }
}
