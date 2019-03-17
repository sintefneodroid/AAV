using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Drivetrain {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Drivetrain/Transmission/Continuous Transmission", 1)]

  //Transmission subclass for continuously variable transmission
  public class ContinuousTransmission : Transmission {
    [Tooltip("Lerp value between min ratio and max ratio")]
    [Range(0, 1)]
    public float targetRatio;

    public float minRatio;
    public float maxRatio;
    [NonSerialized] public float currentRatio;
    public bool canReverse;
    [NonSerialized] public bool reversing;

    [Tooltip("How quickly the target ratio changes with manual shifting")]
    public float manualShiftRate = 0.5f;

    void FixedUpdate() {
      this.health = Mathf.Clamp01(this.health);

      //Set max RPM possible
      if (this.maxRPM == -1) {
        this.maxRPM = this.targetDrive.curve.keys[this.targetDrive.curve.length - 1].time * 1000;
      }

      if (this.health > 0) {
        if (this.automatic && this.vp.groundedWheels > 0) {
          //Automatically set the target ratio
          this.targetRatio = (1 - this.vp.burnout)
                             * Mathf.Clamp01(Mathf.Abs(this.targetDrive.feedbackRPM)
                                             / Mathf.Max(0.01f, this.maxRPM * Mathf.Abs(this.currentRatio)));
        } else if (!this.automatic) {
          //Manually set the target ratio
          this.targetRatio = Mathf.Clamp01(this.targetRatio
                                           + (this.vp.upshiftHold - this.vp.downshiftHold)
                                           * this.manualShiftRate
                                           * Time.deltaTime);
        }
      }

      this.reversing = this.canReverse
                       && this.vp.burnout == 0
                       && this.vp.localVelocity.z < 1
                       && (this.vp.accelInput < 0 || (this.vp.brakeIsReverse && this.vp.brakeInput > 0));
      this.currentRatio =
          Mathf.Lerp(this.minRatio, this.maxRatio, this.targetRatio) * (this.reversing ? -1 : 1);

      this.newDrive.curve = this.targetDrive.curve;
      this.newDrive.rpm = this.targetDrive.rpm / this.currentRatio;
      this.newDrive.torque = Mathf.Abs(this.currentRatio) * this.targetDrive.torque;
      this.SetOutputDrives(this.currentRatio);
    }
  }
}
