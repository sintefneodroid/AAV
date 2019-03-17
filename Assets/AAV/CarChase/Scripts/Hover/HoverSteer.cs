using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Hover {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Hover/Hover Steer", 2)]

  //Class for steering hover vehicles
  public class HoverSteer : MonoBehaviour {
    Transform tr;
    VehicleParent vp;
    public float steerRate = 1;
    float steerAmount;

    [Tooltip("Curve for limiting steer range based on speed, x-axis = speed, y-axis = multiplier")]
    public AnimationCurve steerCurve = AnimationCurve.Linear(0, 1, 30, 0.1f);

    [Tooltip("Horizontal stretch of the steer curve")]
    public float steerCurveStretch = 1;

    public HoverWheel[] steeredWheels;

    [Header("Visual")] public bool rotate;
    public float maxDegreesRotation;
    public float rotationOffset;
    float steerRot;

    void Start() {
      this.tr = this.transform;
      this.vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(this.tr);
    }

    void FixedUpdate() {
      //Set steering of hover wheels
      var rbSpeed = this.vp.localVelocity.z / this.steerCurveStretch;
      var steerLimit = this.steerCurve.Evaluate(Mathf.Abs(rbSpeed));
      this.steerAmount = this.vp.steerInput * steerLimit;

      foreach (var curWheel in this.steeredWheels) {
        curWheel.steerRate = this.steerAmount * this.steerRate;
      }
    }

    void Update() {
      if (this.rotate) {
        this.steerRot = Mathf.Lerp(this.steerRot,
                                   this.steerAmount * this.maxDegreesRotation + this.rotationOffset,
                                   this.steerRate * 0.1f * Time.timeScale);
        this.tr.localEulerAngles =
            new Vector3(this.tr.localEulerAngles.x, this.tr.localEulerAngles.y, this.steerRot);
      }
    }
  }
}
