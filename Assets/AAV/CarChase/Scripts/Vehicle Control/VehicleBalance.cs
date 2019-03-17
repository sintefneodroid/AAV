using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using UnityEngine;

namespace AAV.CarChase.Scripts.Vehicle_Control {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Vehicle Controllers/Vehicle Balance", 4)]

  //Class for balancing vehicles
  public class VehicleBalance : MonoBehaviour {
    Transform tr;
    Rigidbody rb;
    VehicleParent vp;

    float actualPitchInput;
    Vector3 targetLean;
    Vector3 targetLeanActual;

    [Tooltip("Lean strength along each axis")]
    public Vector3 leanFactor;

    [Range(0, 0.99f)] public float leanSmoothness;

    [Tooltip("Adjusts the roll based on the speed, x-axis = speed, y-axis = roll amount")]
    public AnimationCurve leanRollCurve = AnimationCurve.Linear(0, 0, 10, 1);

    [Tooltip("Adjusts the pitch based on the speed, x-axis = speed, y-axis = pitch amount")]
    public AnimationCurve leanPitchCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Adjusts the yaw based on the speed, x-axis = speed, y-axis = yaw amount")]
    public AnimationCurve leanYawCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Speed above which endos (forward wheelies) aren't allowed")]
    public float endoSpeedThreshold;

    [Tooltip("Exponent for pitch input")] public float pitchExponent;

    [Tooltip("How much to lean when sliding sideways")]
    public float slideLeanFactor = 1;

    void Start() {
      this.tr = this.transform;
      this.rb = this.GetComponent<Rigidbody>();
      this.vp = this.GetComponent<VehicleParent>();
    }

    void FixedUpdate() {
      //Apply endo limit
      this.actualPitchInput = this.vp.wheels.Length == 1
                                  ? 0
                                  : Mathf.Clamp(this.vp.pitchInput,
                                                -1,
                                                this.vp.velMag > this.endoSpeedThreshold ? 0 : 1);

      if (this.vp.groundedWheels > 0) {
        if (this.leanFactor != Vector3.zero) {
          this.ApplyLean();
        }
      }
    }

    void ApplyLean() {
      if (this.vp.groundedWheels > 0) {
        Vector3 inverseWorldUp;
        inverseWorldUp =
            this.vp.norm.InverseTransformDirection(Vector3.Dot(this.vp.wheelNormalAverage,
                                                               GlobalControl.worldUpDir)
                                                   <= 0
                                                       ? this.vp.wheelNormalAverage
                                                       : Vector3.Lerp(GlobalControl.worldUpDir,
                                                                      this.vp.wheelNormalAverage,
                                                                      Mathf.Abs(Vector3.Dot(this.vp.norm.up,
                                                                                            GlobalControl
                                                                                                .worldUpDir))
                                                                      * 2));
        Debug.DrawRay(this.tr.position, this.vp.norm.TransformDirection(inverseWorldUp), Color.white);

        //Calculate target lean direction
        this.targetLean =
            new Vector3(Mathf.Lerp(inverseWorldUp.x,
                                   Mathf.Clamp(-this.vp.rollInput
                                               * this.leanFactor.z
                                               * this.leanRollCurve
                                                     .Evaluate(Mathf.Abs(this.vp.localVelocity.z))
                                               + Mathf.Clamp(this.vp.localVelocity.x * this.slideLeanFactor,
                                                             -this.leanFactor.z * this.slideLeanFactor,
                                                             this.leanFactor.z * this.slideLeanFactor),
                                               -this.leanFactor.z,
                                               this.leanFactor.z),
                                   Mathf.Max(Mathf.Abs(F.MaxAbs(this.vp.steerInput, this.vp.rollInput)))),
                        Mathf.Pow(Mathf.Abs(this.actualPitchInput), this.pitchExponent)
                        * Mathf.Sign(this.actualPitchInput)
                        * this.leanFactor.x,
                        inverseWorldUp.z
                        * (1
                           - Mathf.Abs(F.MaxAbs(this.actualPitchInput * this.leanFactor.x,
                                                this.vp.rollInput * this.leanFactor.z))));
      } else {
        this.targetLean = this.vp.upDir;
      }

      //Transform targetLean to world space
      this.targetLeanActual = Vector3.Lerp(this.targetLeanActual,
                                           this.vp.norm.TransformDirection(this.targetLean),
                                           (1 - this.leanSmoothness)
                                           * Time.timeScale
                                           * TimeMaster.inverseFixedTimeFactor).normalized;
      Debug.DrawRay(this.tr.position, this.targetLeanActual, Color.black);

      //Apply pitch
      this.rb.AddTorque(this.vp.norm.right
                        * -(Vector3.Dot(this.vp.forwardDir, this.targetLeanActual) * 20
                            - this.vp.localAngularVel.x)
                        * 100
                        * (this.vp.wheels.Length == 1
                               ? 1
                               : this.leanPitchCurve.Evaluate(Mathf.Abs(this.actualPitchInput))),
                        ForceMode.Acceleration);

      //Apply yaw
      this.rb.AddTorque(this.vp.norm.forward
                        * (this.vp.groundedWheels == 1
                               ? this.vp.steerInput * this.leanFactor.y
                                 - this.vp.norm.InverseTransformDirection(this.rb.angularVelocity).z
                               : 0)
                        * 100
                        * this.leanYawCurve.Evaluate(Mathf.Abs(this.vp.steerInput)),
                        ForceMode.Acceleration);

      //Apply roll
      this.rb.AddTorque(this.vp.norm.up
                        * (-Vector3.Dot(this.vp.rightDir, this.targetLeanActual) * 20
                           - this.vp.localAngularVel.z)
                        * 100,
                        ForceMode.Acceleration);

      //Turn vehicle during wheelies
      if (this.vp.groundedWheels == 1 && this.leanFactor.y > 0) {
        this.rb.AddTorque(this.vp.norm.TransformDirection(new Vector3(0,
                                                                      0,
                                                                      this.vp.steerInput * this.leanFactor.y
                                                                      - this
                                                                        .vp.norm
                                                                        .InverseTransformDirection(this
                                                                                                   .rb
                                                                                                   .angularVelocity)
                                                                        .z)),
                          ForceMode.Acceleration);
      }
    }
  }
}
