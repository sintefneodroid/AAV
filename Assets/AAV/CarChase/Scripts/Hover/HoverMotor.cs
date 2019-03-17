using AAV.CarChase.Scripts.Drivetrain;
using UnityEngine;

namespace AAV.CarChase.Scripts.Hover {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Hover/Hover Motor", 0)]

  //Motor subclass for hovering vehicles
  public class HoverMotor : Motor {
    [Header("Performance")]
    [Tooltip(
        "Curve which calculates the driving force based on the speed of the vehicle, x-axis = speed, y-axis = force")]
    public AnimationCurve forceCurve = AnimationCurve.EaseInOut(0, 1, 50, 0);

    public HoverWheel[] wheels;

    public override void FixedUpdate() {
      base.FixedUpdate();

      //Get proper input
      var actualAccel = this.vp.brakeIsReverse ? this.vp.accelInput - this.vp.brakeInput : this.vp.accelInput;
      this.actualInput = this.inputCurve.Evaluate(Mathf.Abs(actualAccel)) * Mathf.Sign(actualAccel);

      //Set hover wheel speeds and forces
      foreach (var curWheel in this.wheels) {
        if (this.ignition) {
          var boostEval = this.boostPowerCurve.Evaluate(Mathf.Abs(this.vp.localVelocity.z));
          curWheel.targetSpeed = this.actualInput
                                 * this.forceCurve.keys[this.forceCurve.keys.Length - 1].time
                                 * (this.boosting ? 1 + boostEval : 1);
          curWheel.targetForce = Mathf.Abs(this.actualInput)
                                 * this.forceCurve.Evaluate(Mathf.Abs(this.vp.localVelocity.z)
                                                            - (this.boosting ? boostEval : 0))
                                 * this.power
                                 * (this.boosting ? 1 + boostEval : 1)
                                 * this.health;
        } else {
          curWheel.targetSpeed = 0;
          curWheel.targetForce = 0;
        }

        curWheel.doFloat = this.ignition && this.health > 0;
      }
    }

    public override void Update() {
      //Set engine pitch
      if (this.snd && this.ignition) {
        this.targetPitch = Mathf.Max(Mathf.Abs(this.actualInput), Mathf.Abs(this.vp.steerInput) * 0.5f)
                           * (1 - this.forceCurve.Evaluate(Mathf.Abs(this.vp.localVelocity.z)));
      }

      base.Update();
    }
  }
}
