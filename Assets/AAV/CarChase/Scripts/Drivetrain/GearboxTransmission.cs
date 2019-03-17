using System;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Drivetrain {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Drivetrain/Transmission/Gearbox Transmission", 0)]

  //Transmission subclass for gearboxes
  public class GearboxTransmission : Transmission {
    public Gear[] gears;
    public int startGear;
    [NonSerialized] public int currentGear;
    int firstGear;
    [NonSerialized] public float curGearRatio; //Ratio of the current gear

    public bool skipNeutral;

    [Tooltip("Calculate the RPM ranges of the gears in play mode.  This will overwrite the current values")]
    public bool autoCalculateRpmRanges = true;

    [Tooltip("Number of physics steps a shift should last")]
    public float shiftDelay;

    [NonSerialized] public float shiftTime;

    Gear upperGear; //Next gear above current
    Gear lowerGear; //Next gear below current
    float upshiftDifference; //RPM difference between current gear and upper gear
    float downshiftDifference; //RPM difference between current gear and lower gear

    [Tooltip("Multiplier for comparisons in automatic shifting calculations, should be 2 in most cases")]
    public float shiftThreshold;

    public override void Start() {
      base.Start();

      this.currentGear = Mathf.Clamp(this.startGear, 0, this.gears.Length - 1);

      //Get gear number 1 (first one above neutral)
      this.GetFirstGear();
    }

    void Update() {
      //Check for manual shift button presses
      if (!this.automatic) {
        if (this.vp.upshiftPressed && this.currentGear < this.gears.Length - 1) {
          this.Shift(1);
        }

        if (this.vp.downshiftPressed && this.currentGear > 0) {
          this.Shift(-1);
        }
      }
    }

    void FixedUpdate() {
      this.health = Mathf.Clamp01(this.health);
      this.shiftTime = Mathf.Max(0, this.shiftTime - Time.timeScale * TimeMaster.inverseFixedTimeFactor);
      this.curGearRatio = this.gears[this.currentGear].ratio;

      //Calculate upperGear and lowerGear
      var actualFeedbackRPM = this.targetDrive.feedbackRPM / Mathf.Abs(this.curGearRatio);
      var upGearOffset = 1;
      var downGearOffset = 1;

      while ((this.skipNeutral || this.automatic)
             && this.gears[Mathf.Clamp(this.currentGear + upGearOffset, 0, this.gears.Length - 1)].ratio == 0
             && this.currentGear + upGearOffset != 0
             && this.currentGear + upGearOffset != this.gears.Length - 1) {
        upGearOffset++;
      }

      while ((this.skipNeutral || this.automatic)
             && this.gears[Mathf.Clamp(this.currentGear - downGearOffset, 0, this.gears.Length - 1)].ratio
             == 0
             && this.currentGear - downGearOffset != 0
             && this.currentGear - downGearOffset != 0) {
        downGearOffset++;
      }

      this.upperGear = this.gears[Mathf.Min(this.gears.Length - 1, this.currentGear + upGearOffset)];
      this.lowerGear = this.gears[Mathf.Max(0, this.currentGear - downGearOffset)];

      //Perform RPM calculations
      if (this.maxRPM == -1) {
        this.maxRPM = this.targetDrive.curve.keys[this.targetDrive.curve.length - 1].time;

        if (this.autoCalculateRpmRanges) {
          this.CalculateRpmRanges();
        }
      }

      //Set RPMs and torque of output
      this.newDrive.curve = this.targetDrive.curve;

      if (this.curGearRatio == 0 || this.shiftTime > 0) {
        this.newDrive.rpm = 0;
        this.newDrive.torque = 0;
      } else {
        this.newDrive.rpm = (this.automatic && this.skidSteerDrive
                                 ? Mathf.Abs(this.targetDrive.rpm)
                                   * Mathf.Sign(this.vp.accelInput
                                                - (this.vp.brakeIsReverse
                                                       ? this.vp.brakeInput * (1 - this.vp.burnout)
                                                       : 0))
                                 : this.targetDrive.rpm)
                            / this.curGearRatio;
        this.newDrive.torque = Mathf.Abs(this.curGearRatio) * this.targetDrive.torque;
      }

      //Perform automatic shifting
      this.upshiftDifference = this.gears[this.currentGear].maxRPM - this.upperGear.minRPM;
      this.downshiftDifference = this.lowerGear.maxRPM - this.gears[this.currentGear].minRPM;

      if (this.automatic && this.shiftTime == 0 && this.vp.groundedWheels > 0) {
        if (!this.skidSteerDrive && this.vp.burnout == 0) {
          if (Mathf.Abs(this.vp.localVelocity.z) > 1
              || this.vp.accelInput > 0
              || (this.vp.brakeInput > 0 && this.vp.brakeIsReverse)) {
            if (this.currentGear < this.gears.Length - 1
                && (this.upperGear.minRPM
                    + this.upshiftDifference
                    * (this.curGearRatio < 0 ? Mathf.Min(1, this.shiftThreshold) : this.shiftThreshold)
                    - actualFeedbackRPM
                    <= 0
                    || (this.curGearRatio <= 0
                        && this.upperGear.ratio > 0
                        && (!this.vp.reversing
                            || (this.vp.accelInput > 0 && this.vp.localVelocity.z > this.curGearRatio * 10))))
                && !(this.vp.brakeInput > 0 && this.vp.brakeIsReverse && this.upperGear.ratio >= 0)
                && !(this.vp.localVelocity.z < 0 && this.vp.accelInput == 0)) {
              this.Shift(1);
            } else if (this.currentGear > 0
                       && (actualFeedbackRPM
                           - (this.lowerGear.maxRPM - this.downshiftDifference * this.shiftThreshold)
                           <= 0
                           || (this.curGearRatio >= 0
                               && this.lowerGear.ratio < 0
                               && (this.vp.reversing
                                   || ((this.vp.accelInput < 0
                                        || (this.vp.brakeInput > 0 && this.vp.brakeIsReverse))
                                       && this.vp.localVelocity.z < this.curGearRatio * 10))))
                       && !(this.vp.accelInput > 0 && this.lowerGear.ratio <= 0)
                       && (this.lowerGear.ratio > 0 || this.vp.localVelocity.z < 1)) {
              this.Shift(-1);
            }
          }
        } else if (this.currentGear != this.firstGear) {
          //Shift into first gear if skid steering or burning out
          this.ShiftToGear(this.firstGear);
        }
      }

      this.SetOutputDrives(this.curGearRatio);
    }

    //Shift gears by the number entered
    public void Shift(int dir) {
      if (this.health > 0) {
        this.shiftTime = this.shiftDelay;
        this.currentGear += dir;

        while ((this.skipNeutral || this.automatic)
               && this.gears[Mathf.Clamp(this.currentGear, 0, this.gears.Length - 1)].ratio == 0
               && this.currentGear != 0
               && this.currentGear != this.gears.Length - 1) {
          this.currentGear += dir;
        }

        this.currentGear = Mathf.Clamp(this.currentGear, 0, this.gears.Length - 1);
      }
    }

    //Shift straight to the gear specified
    public void ShiftToGear(int gear) {
      if (this.health > 0) {
        this.shiftTime = this.shiftDelay;
        this.currentGear = Mathf.Clamp(gear, 0, this.gears.Length - 1);
      }
    }

    //Caculate ideal RPM ranges for each gear (works most of the time)
    public void CalculateRpmRanges() {
      var cantCalc = false;
      if (!Application.isPlaying) {
        var engine = F.GetTopmostParentComponent<VehicleParent>(this.transform)
                      .GetComponentInChildren<GasMotor>();

        if (engine) {
          this.maxRPM = engine.torqueCurve.keys[engine.torqueCurve.length - 1].time;
        } else {
          Debug.LogError("There is no <GasMotor> in the vehicle to get RPM info from.", this);
          cantCalc = true;
        }
      }

      if (!cantCalc) {
        float prevGearRatio;
        float nextGearRatio;
        var actualMaxRPM = this.maxRPM * 1000;

        for (var i = 0; i < this.gears.Length; i++) {
          prevGearRatio = this.gears[Mathf.Max(i - 1, 0)].ratio;
          nextGearRatio = this.gears[Mathf.Min(i + 1, this.gears.Length - 1)].ratio;

          if (this.gears[i].ratio < 0) {
            this.gears[i].minRPM = actualMaxRPM / this.gears[i].ratio;

            if (nextGearRatio == 0) {
              this.gears[i].maxRPM = 0;
            } else {
              this.gears[i].maxRPM = actualMaxRPM / nextGearRatio
                                     + (actualMaxRPM / nextGearRatio - this.gears[i].minRPM) * 0.5f;
            }
          } else if (this.gears[i].ratio > 0) {
            this.gears[i].maxRPM = actualMaxRPM / this.gears[i].ratio;

            if (prevGearRatio == 0) {
              this.gears[i].minRPM = 0;
            } else {
              this.gears[i].minRPM = actualMaxRPM / prevGearRatio
                                     - (this.gears[i].maxRPM - actualMaxRPM / prevGearRatio) * 0.5f;
            }
          } else {
            this.gears[i].minRPM = 0;
            this.gears[i].maxRPM = 0;
          }

          this.gears[i].minRPM *= 0.55f;
          this.gears[i].maxRPM *= 0.55f;
        }
      }
    }

    public void GetFirstGear() {
      for (var i = 0; i < this.gears.Length; i++) {
        if (this.gears[i].ratio == 0) {
          this.firstGear = i + 1;
          break;
        }
      }
    }
  }

  //Gear class
  [Serializable]
  public class Gear {
    public float ratio;
    public float minRPM;
    public float maxRPM;
  }
}
