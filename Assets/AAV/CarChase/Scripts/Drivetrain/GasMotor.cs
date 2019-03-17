using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Drivetrain {
  [RequireComponent(typeof(DriveForce))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Drivetrain/Gas Motor", 0)]

  //Motor subclass for internal combustion engines
  public class GasMotor : Motor {
    [Header("Performance")]
    [Tooltip("X-axis = RPM in thousands, y-axis = torque.  The rightmost key represents the maximum RPM")]
    public AnimationCurve torqueCurve = AnimationCurve.EaseInOut(0, 0, 8, 1);

    [Range(0, 0.99f)]
    [Tooltip("How quickly the engine adjusts its RPMs")]
    public float inertia;

    [Tooltip("Can the engine turn backwards?")]
    public bool canReverse;

    DriveForce targetDrive;
    [NonSerialized] public float maxRPM;

    public DriveForce[] outputDrives;

    [Tooltip("Exponent for torque output on each wheel")]
    public float driveDividePower = 3;

    float actualAccel;

    [Header("Transmission")] public GearboxTransmission transmission;
    [NonSerialized] public bool shifting;

    [Tooltip("Increase sound pitch between shifts")]
    public bool pitchIncreaseBetweenShift;

    public override void Start() {
      base.Start();
      this.targetDrive = this.GetComponent<DriveForce>();
      //Get maximum possible RPM
      this.GetMaxRPM();
    }

    public override void FixedUpdate() {
      base.FixedUpdate();

      //Calculate proper input
      this.actualAccel =
          Mathf.Lerp(this.vp.brakeIsReverse && this.vp.reversing && this.vp.accelInput <= 0
                         ? this.vp.brakeInput
                         : this.vp.accelInput,
                     Mathf.Max(this.vp.accelInput, this.vp.burnout),
                     this.vp.burnout);
      var accelGet = this.canReverse ? this.actualAccel : Mathf.Clamp01(this.actualAccel);
      this.actualInput = this.inputCurve.Evaluate(Mathf.Abs(accelGet)) * Mathf.Sign(accelGet);
      this.targetDrive.curve = this.torqueCurve;

      if (this.ignition) {
        var boostEval = this.boostPowerCurve.Evaluate(Mathf.Abs(this.vp.localVelocity.z));
        //Set RPM
        this.targetDrive.rpm = Mathf.Lerp(this.targetDrive.rpm,
                                          this.actualInput
                                          * this.maxRPM
                                          * 1000
                                          * (this.boosting ? 1 + boostEval : 1),
                                          (1 - this.inertia) * Time.timeScale);
        //Set torque
        if (this.targetDrive.feedbackRPM > this.targetDrive.rpm) {
          this.targetDrive.torque = 0;
        } else {
          this.targetDrive.torque =
              this.torqueCurve.Evaluate(this.targetDrive.feedbackRPM * 0.001f
                                        - (this.boosting ? boostEval : 0))
              * Mathf.Lerp(this.targetDrive.torque,
                           this.power * Mathf.Abs(Math.Sign(this.actualInput)),
                           (1 - this.inertia) * Time.timeScale)
              * (this.boosting ? 1 + boostEval : 1)
              * this.health;
        }

        //Send RPM and torque through drivetrain
        if (this.outputDrives.Length > 0) {
          var torqueFactor = Mathf.Pow(1f / this.outputDrives.Length, this.driveDividePower);
          float tempRPM = 0;

          foreach (var curOutput in this.outputDrives) {
            tempRPM += curOutput.feedbackRPM;
            curOutput.SetDrive(this.targetDrive, torqueFactor);
          }

          this.targetDrive.feedbackRPM = tempRPM / this.outputDrives.Length;
        }

        if (this.transmission) {
          this.shifting = this.transmission.shiftTime > 0;
        } else {
          this.shifting = false;
        }
      } else {
        //If turned off, set RPM and torque to 0 and distribute it through drivetrain
        this.targetDrive.rpm = 0;
        this.targetDrive.torque = 0;
        this.targetDrive.feedbackRPM = 0;
        this.shifting = false;

        if (this.outputDrives.Length > 0) {
          foreach (var curOutput in this.outputDrives) {
            curOutput.SetDrive(this.targetDrive);
          }
        }
      }
    }

    public override void Update() {
      //Set audio pitch
      if (this.snd && this.ignition) {
        this.airPitch = this.vp.groundedWheels > 0 || this.actualAccel != 0
                            ? 1
                            : Mathf.Lerp(this.airPitch, 0, 0.5f * Time.deltaTime);
        this.pitchFactor = (this.actualAccel != 0 || this.vp.groundedWheels == 0 ? 1 : 0.5f)
                           * (this.shifting
                                  ? (this.pitchIncreaseBetweenShift
                                         ? Mathf.Sin((this.transmission.shiftTime
                                                      / this.transmission.shiftDelay)
                                                     * Mathf.PI)
                                         : Mathf.Min(this.transmission.shiftDelay,
                                                     Mathf.Pow(this.transmission.shiftTime, 2))
                                           / this.transmission.shiftDelay)
                                  : 1)
                           * this.airPitch;
        this.targetPitch =
            Mathf.Abs((this.targetDrive.feedbackRPM * 0.001f) / this.maxRPM) * this.pitchFactor;
      }

      base.Update();
    }

    public void GetMaxRPM() {
      this.maxRPM = this.torqueCurve.keys[this.torqueCurve.length - 1].time;

      if (this.outputDrives.Length > 0) {
        foreach (var curOutput in this.outputDrives) {
          curOutput.curve = this.targetDrive.curve;

          if (curOutput.GetComponent<Transmission>()) {
            curOutput.GetComponent<Transmission>().ResetMaxRPM();
          }
        }
      }
    }
  }
}
