using System;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Stunt {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Stunt/Flip Control", 2)]

  //Class for in-air rotation of vehicles
  public class FlipControl : MonoBehaviour {
    Transform tr;
    Rigidbody rb;
    VehicleParent vp;

    public bool disableDuringCrash;
    public Vector3 flipPower;

    [Tooltip("Continue spinning if input is stopped")]
    public bool freeSpinFlip;

    [Tooltip("Stop spinning if input is stopped and vehicle is upright")]
    public bool stopFlip;

    [Tooltip("How quickly the vehicle will rotate upright in air")]
    public Vector3 rotationCorrection;

    Quaternion velDir;

    [Tooltip("Distance to check for ground for reference normal for rotation correction")]
    public float groundCheckDistance = 100;

    [Tooltip("Minimum dot product between ground normal and global up direction for rotation correction")]
    public float groundSteepnessLimit = 0.5f;

    [Tooltip("How quickly the vehicle will dive in the direction it's soaring")]
    public float diveFactor;

    void Start() {
      this.tr = this.transform;
      this.rb = this.GetComponent<Rigidbody>();
      this.vp = this.GetComponent<VehicleParent>();
    }

    void FixedUpdate() {
      if (this.vp.groundedWheels == 0
          && (!this.vp.crashing || (this.vp.crashing && !this.disableDuringCrash))) {
        this.velDir = Quaternion.LookRotation(GlobalControl.worldUpDir, this.rb.velocity);

        if (this.flipPower != Vector3.zero) {
          this.ApplyFlip();
        }

        if (this.stopFlip) {
          this.ApplyStopFlip();
        }

        if (this.rotationCorrection != Vector3.zero) {
          this.ApplyRotationCorrection();
        }

        if (this.diveFactor > 0) {
          this.Dive();
        }
      }
    }

    void ApplyFlip() {
      Vector3 flipTorque;

      if (this.freeSpinFlip) {
        flipTorque = new Vector3(this.vp.pitchInput * this.flipPower.x,
                                 this.vp.yawInput * this.flipPower.y,
                                 this.vp.rollInput * this.flipPower.z);
      } else {
        flipTorque =
            new
                Vector3(this.vp.pitchInput != 0
                        && Mathf.Abs(this.vp.localAngularVel.x) > 1
                        && Math.Sign(this.vp.pitchInput * Mathf.Sign(this.flipPower.x))
                        != Math.Sign(this.vp.localAngularVel.x)
                            ? -this.vp.localAngularVel.x * Mathf.Abs(this.flipPower.x)
                            : this.vp.pitchInput * this.flipPower.x
                              - this.vp.localAngularVel.x
                              * (1 - Mathf.Abs(this.vp.pitchInput))
                              * Mathf.Abs(this.flipPower.x),
                        this.vp.yawInput != 0
                        && Mathf.Abs(this.vp.localAngularVel.y) > 1
                        && Math.Sign(this.vp.yawInput * Mathf.Sign(this.flipPower.y))
                        != Math.Sign(this.vp.localAngularVel.y)
                            ? -this.vp.localAngularVel.y * Mathf.Abs(this.flipPower.y)
                            : this.vp.yawInput * this.flipPower.y
                              - this.vp.localAngularVel.y
                              * (1 - Mathf.Abs(this.vp.yawInput))
                              * Mathf.Abs(this.flipPower.y),
                        this.vp.rollInput != 0
                        && Mathf.Abs(this.vp.localAngularVel.z) > 1
                        && Math.Sign(this.vp.rollInput * Mathf.Sign(this.flipPower.z))
                        != Math.Sign(this.vp.localAngularVel.z)
                            ? -this.vp.localAngularVel.z * Mathf.Abs(this.flipPower.z)
                            : this.vp.rollInput * this.flipPower.z
                              - this.vp.localAngularVel.z
                              * (1 - Mathf.Abs(this.vp.rollInput))
                              * Mathf.Abs(this.flipPower.z));
      }

      this.rb.AddRelativeTorque(flipTorque, ForceMode.Acceleration);
    }

    void ApplyStopFlip() {
      var stopFlipFactor = Vector3.zero;

      stopFlipFactor.x = this.vp.pitchInput * this.flipPower.x == 0
                             ? Mathf.Pow(Mathf.Clamp01(this.vp.upDot),
                                         Mathf.Clamp(10 - Mathf.Abs(this.vp.localAngularVel.x), 2, 10))
                               * 10
                             : 0;
      stopFlipFactor.y = this.vp.yawInput * this.flipPower.y == 0 && this.vp.sqrVelMag > 5
                             ? Mathf.Pow(Mathf.Clamp01(Vector3.Dot(this.vp.forwardDir,
                                                                   this.velDir * Vector3.up)),
                                         Mathf.Clamp(10 - Mathf.Abs(this.vp.localAngularVel.y), 2, 10))
                               * 10
                             : 0;
      stopFlipFactor.z = this.vp.rollInput * this.flipPower.z == 0
                             ? Mathf.Pow(Mathf.Clamp01(this.vp.upDot),
                                         Mathf.Clamp(10 - Mathf.Abs(this.vp.localAngularVel.z), 2, 10))
                               * 10
                             : 0;

      this.rb.AddRelativeTorque(new Vector3(-this.vp.localAngularVel.x * stopFlipFactor.x,
                                            -this.vp.localAngularVel.y * stopFlipFactor.y,
                                            -this.vp.localAngularVel.z * stopFlipFactor.z),
                                ForceMode.Acceleration);
    }

    void ApplyRotationCorrection() {
      var actualForwardDot = this.vp.forwardDot;
      var actualRightDot = this.vp.rightDot;
      var actualUpDot = this.vp.upDot;

      if (this.groundCheckDistance > 0) {
        if (Physics.Raycast(this.tr.position,
                            (-GlobalControl.worldUpDir + this.rb.velocity).normalized,
                            out var groundHit,
                            this.groundCheckDistance,
                            GlobalControl.groundMaskStatic)) {
          if (Vector3.Dot(groundHit.normal, GlobalControl.worldUpDir) >= this.groundSteepnessLimit) {
            actualForwardDot = Vector3.Dot(this.vp.forwardDir, groundHit.normal);
            actualRightDot = Vector3.Dot(this.vp.rightDir, groundHit.normal);
            actualUpDot = Vector3.Dot(this.vp.upDir, groundHit.normal);
          }
        }
      }

      this.rb.AddRelativeTorque(new Vector3(this.vp.pitchInput * this.flipPower.x == 0
                                                ? actualForwardDot
                                                  * (1 - Mathf.Abs(actualRightDot))
                                                  * this.rotationCorrection.x
                                                  - this.vp.localAngularVel.x * Mathf.Pow(actualUpDot, 2) * 10
                                                : 0,
                                            this.vp.yawInput * this.flipPower.y == 0 && this.vp.sqrVelMag > 10
                                                ? Vector3.Dot(this.vp.forwardDir, this.velDir * Vector3.right)
                                                  * Mathf.Abs(actualUpDot)
                                                  * this.rotationCorrection.y
                                                  - this.vp.localAngularVel.y * Mathf.Pow(actualUpDot, 2) * 10
                                                : 0,
                                            this.vp.rollInput * this.flipPower.z == 0
                                                ? -actualRightDot
                                                  * (1 - Mathf.Abs(actualForwardDot))
                                                  * this.rotationCorrection.z
                                                  - this.vp.localAngularVel.z * Mathf.Pow(actualUpDot, 2) * 10
                                                : 0),
                                ForceMode.Acceleration);
    }

    void Dive() {
      this.rb.AddTorque(this.velDir
                        * Vector3.left
                        * Mathf.Clamp01(this.vp.velMag * 0.01f)
                        * Mathf.Clamp01(this.vp.upDot)
                        * this.diveFactor,
                        ForceMode.Acceleration);
    }
  }
}
