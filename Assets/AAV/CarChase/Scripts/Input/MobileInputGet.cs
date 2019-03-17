using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Input {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Input/Mobile Input Getter", 2)]

  //Class for getting mobile input
  public class MobileInputGet : MonoBehaviour {
    VehicleParent vp;
    MobileInput setter;
    public float steerFactor = 1;
    public float flipFactor = 1;
    public bool useAccelerometer = true;

    [Tooltip("Multiplier for input addition based on rate of change of input")]
    public float deltaFactor = 10;

    Vector3 accelerationPrev;
    Vector3 accelerationDelta;

    void Start() {
      this.vp = this.GetComponent<VehicleParent>();
      this.setter = FindObjectOfType<MobileInput>();
    }

    void FixedUpdate() {
      if (this.setter) {
        this.accelerationDelta = UnityEngine.Input.acceleration - this.accelerationPrev;
        this.accelerationPrev = UnityEngine.Input.acceleration;
        this.vp.SetAccel(this.setter.accel);
        this.vp.SetBrake(this.setter.brake);
        this.vp.SetEbrake(this.setter.ebrake);
        this.vp.SetBoost(this.setter.boost);

        if (this.useAccelerometer) {
          this.vp.SetSteer((UnityEngine.Input.acceleration.x + this.accelerationDelta.x * this.deltaFactor)
                           * this.steerFactor);
          this.vp.SetYaw(UnityEngine.Input.acceleration.x * this.flipFactor);
          this.vp.SetPitch(-UnityEngine.Input.acceleration.z * this.flipFactor);
        } else {
          this.vp.SetSteer(this.setter.steer);
        }
      }
    }
  }
}
