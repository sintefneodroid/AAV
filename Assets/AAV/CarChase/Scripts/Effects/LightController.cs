using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Effects {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Effects/Light Controller", 2)]

  //Class for controlling vehicle lights
  public class LightController : MonoBehaviour {
    VehicleParent vp;

    public bool headlightsOn;
    public bool highBeams;
    public bool brakelightsOn;
    public bool rightBlinkersOn;
    public bool leftBlinkersOn;
    public float blinkerInterval = 0.3f;
    bool blinkerIntervalOn;
    float blinkerSwitchTime;
    public bool reverseLightsOn;

    public Transmission transmission;
    GearboxTransmission gearTrans;
    ContinuousTransmission conTrans;

    public VehicleLight[] headlights;
    public VehicleLight[] brakeLights;
    public VehicleLight[] RightBlinkers;
    public VehicleLight[] LeftBlinkers;
    public VehicleLight[] ReverseLights;

    void Start() {
      this.vp = this.GetComponent<VehicleParent>();

      //Get transmission for using reverse lights
      if (this.transmission) {
        if (this.transmission is GearboxTransmission) {
          this.gearTrans = this.transmission as GearboxTransmission;
        } else if (this.transmission is ContinuousTransmission) {
          this.conTrans = this.transmission as ContinuousTransmission;
        }
      }
    }

    void Update() {
      //Activate blinkers
      if (this.leftBlinkersOn || this.rightBlinkersOn) {
        if (this.blinkerSwitchTime == 0) {
          this.blinkerIntervalOn = !this.blinkerIntervalOn;
          this.blinkerSwitchTime = this.blinkerInterval;
        } else {
          this.blinkerSwitchTime = Mathf.Max(0, this.blinkerSwitchTime - Time.deltaTime);
        }
      } else {
        this.blinkerIntervalOn = false;
        this.blinkerSwitchTime = 0;
      }

      //Activate reverse lights
      if (this.gearTrans) {
        this.reverseLightsOn = this.gearTrans.curGearRatio < 0;
      } else if (this.conTrans) {
        this.reverseLightsOn = this.conTrans.reversing;
      }

      //Activate brake lights
      if (this.vp.accelAxisIsBrake) {
        this.brakelightsOn = this.vp.accelInput != 0
                             && Mathf.Sign(this.vp.accelInput) != Mathf.Sign(this.vp.localVelocity.z)
                             && Mathf.Abs(this.vp.localVelocity.z) > 1;
      } else {
        if (!this.vp.brakeIsReverse) {
          this.brakelightsOn = (this.vp.burnout > 0 && this.vp.brakeInput > 0) || this.vp.brakeInput > 0;
        } else {
          this.brakelightsOn = (this.vp.burnout > 0 && this.vp.brakeInput > 0)
                               || ((this.vp.brakeInput > 0 && this.vp.localVelocity.z > 1)
                                   || (this.vp.accelInput > 0 && this.vp.localVelocity.z < -1));
        }
      }

      this.SetLights(this.headlights, this.highBeams, this.headlightsOn);
      this.SetLights(this.brakeLights, this.headlightsOn || this.highBeams, this.brakelightsOn);
      this.SetLights(this.RightBlinkers, this.rightBlinkersOn && this.blinkerIntervalOn);
      this.SetLights(this.LeftBlinkers, this.leftBlinkersOn && this.blinkerIntervalOn);
      this.SetLights(this.ReverseLights, this.reverseLightsOn);
    }

    //Set if lights are on or off based on the condition
    void SetLights(VehicleLight[] lights, bool condition) {
      foreach (var curLight in lights) {
        curLight.on = condition;
      }
    }

    void SetLights(VehicleLight[] lights, bool condition, bool halfCondition) {
      foreach (var curLight in lights) {
        curLight.on = condition;
        curLight.halfOn = halfCondition;
      }
    }
  }
}
