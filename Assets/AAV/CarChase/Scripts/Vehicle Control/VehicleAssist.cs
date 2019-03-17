using System;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using UnityEngine;

namespace AAV.CarChase.Scripts.Vehicle_Control {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Vehicle Controllers/Vehicle Assist", 1)]

  //Class for assisting vehicle performance
  public class VehicleAssist : MonoBehaviour {
    Transform tr;
    Rigidbody rb;
    VehicleParent vp;

    [Header("Drift")]
    [Tooltip(
        "Variables are multiplied based on the number of wheels grounded out of the total number of wheels")]
    public bool basedOnWheelsGrounded;

    float groundedFactor;

    [Tooltip("How much to assist with spinning while drifting")]
    public float driftSpinAssist;

    public float driftSpinSpeed;
    public float driftSpinExponent = 1;

    [Tooltip("Automatically adjust drift angle based on steer input magnitude")]
    public bool autoSteerDrift;

    public float maxDriftAngle = 70;
    float targetDriftAngle;

    [Tooltip("Adjusts the force based on drift speed, x-axis = speed, y-axis = force")]
    public AnimationCurve driftSpinCurve = AnimationCurve.Linear(0, 0, 10, 1);

    [Tooltip("How much to push the vehicle forward while drifting")]
    public float driftPush;

    [Tooltip("Straighten out the vehicle when sliding slightly")]
    public bool straightenAssist;

    [Header("Downforce")] public float downforce = 1;
    public bool invertDownforceInReverse;
    public bool applyDownforceInAir;

    [Tooltip("X-axis = speed, y-axis = force")]
    public AnimationCurve downforceCurve = AnimationCurve.Linear(0, 0, 20, 1);

    [Header("Roll Over")]
    [Tooltip("Automatically roll over when rolled over")]
    public bool autoRollOver;

    [Tooltip("Roll over with steer input")]
    public bool steerRollOver;

    [NonSerialized] public bool rolledOver;

    [Tooltip("Distance to check on sides to see if rolled over")]
    public float rollCheckDistance = 1;

    public float rollOverForce = 1;

    [Tooltip("Maximum speed at which vehicle can be rolled over with assists")]
    public float rollSpeedThreshold;

    [Header("Air")]
    [Tooltip("Increase angular drag immediately after jumping")]
    public bool angularDragOnJump;

    float initialAngularDrag;
    float angDragTime;

    public float fallSpeedLimit = Mathf.Infinity;
    public bool applyFallLimitUpwards;

    void Start() {
      this.tr = this.transform;
      this.rb = this.GetComponent<Rigidbody>();
      this.vp = this.GetComponent<VehicleParent>();
      this.initialAngularDrag = this.rb.angularDrag;
    }

    void FixedUpdate() {
      if (this.vp.groundedWheels > 0) {
        this.groundedFactor = this.basedOnWheelsGrounded
                                  ? this.vp.groundedWheels
                                    / (this.vp.hover ? this.vp.hoverWheels.Length : this.vp.wheels.Length)
                                  : 1;

        this.angDragTime = 20;
        this.rb.angularDrag = this.initialAngularDrag;

        if (this.driftSpinAssist > 0) {
          this.ApplySpinAssist();
        }

        if (this.driftPush > 0) {
          this.ApplyDriftPush();
        }
      } else {
        if (this.angularDragOnJump) {
          this.angDragTime =
              Mathf.Max(0, this.angDragTime - Time.timeScale * TimeMaster.inverseFixedTimeFactor);
          this.rb.angularDrag = this.angDragTime > 0 && this.vp.upDot > 0.5 ? 10 : this.initialAngularDrag;
        }
      }

      if (this.downforce > 0) {
        this.ApplyDownforce();
      }

      if (this.autoRollOver || this.steerRollOver) {
        this.RollOver();
      }

      if (Mathf.Abs(this.vp.localVelocity.y) > this.fallSpeedLimit
          && (this.vp.localVelocity.y < 0 || this.applyFallLimitUpwards)) {
        this.rb.AddRelativeForce(Vector3.down * this.vp.localVelocity.y, ForceMode.Acceleration);
      }
    }

    void ApplySpinAssist() {
      //Get desired rotation speed
      float targetTurnSpeed = 0;

      //Auto steer drift
      if (this.autoSteerDrift) {
        var steerSign = 0;
        if (this.vp.steerInput != 0) {
          steerSign = (int)Mathf.Sign(this.vp.steerInput);
        }

        this.targetDriftAngle =
            (steerSign != Mathf.Sign(this.vp.localVelocity.x) ? this.vp.steerInput : steerSign)
            * -this.maxDriftAngle;
        var velDir = new Vector3(this.vp.localVelocity.x, 0, this.vp.localVelocity.z).normalized;
        var targetDir = new Vector3(Mathf.Sin(this.targetDriftAngle * Mathf.Deg2Rad),
                                    0,
                                    Mathf.Cos(this.targetDriftAngle * Mathf.Deg2Rad)).normalized;
        var driftTorqueTemp = velDir - targetDir;
        targetTurnSpeed =
            driftTorqueTemp.magnitude * Mathf.Sign(driftTorqueTemp.z) * steerSign * this.driftSpinSpeed
            - this.vp.localAngularVel.y * Mathf.Clamp01(Vector3.Dot(velDir, targetDir)) * 2;
      } else {
        targetTurnSpeed = this.vp.steerInput
                          * this.driftSpinSpeed
                          * (this.vp.localVelocity.z < 0
                                 ? (this.vp.accelAxisIsBrake
                                        ? Mathf.Sign(this.vp.accelInput)
                                        : Mathf.Sign(F.MaxAbs(this.vp.accelInput, -this.vp.brakeInput)))
                                 : 1);
      }

      this.rb.AddRelativeTorque(new Vector3(0,
                                            (targetTurnSpeed - this.vp.localAngularVel.y)
                                            * this.driftSpinAssist
                                            * this.driftSpinCurve
                                                  .Evaluate(Mathf.Abs(Mathf.Pow(this.vp.localVelocity.x,
                                                                                this.driftSpinExponent)))
                                            * this.groundedFactor,
                                            0),
                                ForceMode.Acceleration);

      var rightVelDot = Vector3.Dot(this.tr.right, this.rb.velocity.normalized);

      if (this.straightenAssist
          && this.vp.steerInput == 0
          && Mathf.Abs(rightVelDot) < 0.1f
          && this.vp.sqrVelMag > 5) {
        this.rb.AddRelativeTorque(new Vector3(0,
                                              rightVelDot
                                              * 100
                                              * Mathf.Sign(this.vp.localVelocity.z)
                                              * this.driftSpinAssist,
                                              0),
                                  ForceMode.Acceleration);
      }
    }

    void ApplyDownforce() {
      if (this.vp.groundedWheels > 0 || this.applyDownforceInAir) {
        this.rb.AddRelativeForce(new Vector3(0,
                                             this.downforceCurve.Evaluate(Mathf.Abs(this.vp.localVelocity.z))
                                             * -this.downforce
                                             * (this.applyDownforceInAir ? 1 : this.groundedFactor)
                                             * (this.invertDownforceInReverse
                                                    ? Mathf.Sign(this.vp.localVelocity.z)
                                                    : 1),
                                             0),
                                 ForceMode.Acceleration);

        //Reverse downforce
        if (this.invertDownforceInReverse && this.vp.localVelocity.z < 0) {
          this.rb.AddRelativeTorque(new Vector3(this.downforceCurve
                                                    .Evaluate(Mathf.Abs(this.vp.localVelocity.z))
                                                * this.downforce
                                                * (this.applyDownforceInAir ? 1 : this.groundedFactor),
                                                0,
                                                0),
                                    ForceMode.Acceleration);
        }
      }
    }

    void RollOver() {
      //Check if rolled over
      if (this.vp.groundedWheels == 0
          && this.vp.velMag < this.rollSpeedThreshold
          && this.vp.upDot < 0.8
          && this.rollCheckDistance > 0) {
        if (Physics.Raycast(this.tr.position,
                            this.vp.upDir,
                            out var rollHit,
                            this.rollCheckDistance,
                            GlobalControl.groundMaskStatic)
            || Physics.Raycast(this.tr.position,
                               this.vp.rightDir,
                               out rollHit,
                               this.rollCheckDistance,
                               GlobalControl.groundMaskStatic)
            || Physics.Raycast(this.tr.position,
                               -this.vp.rightDir,
                               out rollHit,
                               this.rollCheckDistance,
                               GlobalControl.groundMaskStatic)) {
          this.rolledOver = true;
        } else {
          this.rolledOver = false;
        }
      } else {
        this.rolledOver = false;
      }

      //Apply roll over force
      if (this.rolledOver) {
        if (this.steerRollOver && this.vp.steerInput != 0) {
          this.rb.AddRelativeTorque(new Vector3(0, 0, -this.vp.steerInput * this.rollOverForce),
                                    ForceMode.Acceleration);
        } else if (this.autoRollOver) {
          this.rb.AddRelativeTorque(new Vector3(0, 0, -Mathf.Sign(this.vp.rightDot) * this.rollOverForce),
                                    ForceMode.Acceleration);
        }
      }
    }

    void ApplyDriftPush() {
      var pushFactor =
          (this.vp.accelAxisIsBrake ? this.vp.accelInput : this.vp.accelInput - this.vp.brakeInput)
          * Mathf.Abs(this.vp.localVelocity.x)
          * this.driftPush
          * this.groundedFactor
          * (1 - Mathf.Abs(Vector3.Dot(this.vp.forwardDir, this.rb.velocity.normalized)));

      this.rb.AddForce(this.vp.norm.TransformDirection(new Vector3(Mathf.Abs(pushFactor)
                                                                   * Mathf.Sign(this.vp.localVelocity.x),
                                                                   Mathf.Abs(pushFactor)
                                                                   * Mathf.Sign(this.vp.localVelocity.z),
                                                                   0)),
                       ForceMode.Acceleration);
    }
  }
}
