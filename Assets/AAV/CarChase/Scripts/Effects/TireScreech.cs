using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Ground;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Effects {
  [RequireComponent(typeof(AudioSource))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Effects/Tire Screech Audio", 1)]

  //Class for playing tire screech sounds
  public class TireScreech : MonoBehaviour {
    AudioSource snd;
    VehicleParent vp;
    Wheel[] wheels;
    float slipThreshold;
    GroundSurface surfaceType;

    void Start() {
      this.snd = this.GetComponent<AudioSource>();
      this.vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(this.transform);
      this.wheels = new Wheel[this.vp.wheels.Length];

      //Get wheels and average slip threshold
      for (var i = 0; i < this.vp.wheels.Length; i++) {
        this.wheels[i] = this.vp.wheels[i];
        if (this.vp.wheels[i].GetComponent<TireMarkCreate>()) {
          var newThreshold = this.vp.wheels[i].GetComponent<TireMarkCreate>().slipThreshold;
          this.slipThreshold = i == 0 ? newThreshold : (this.slipThreshold + newThreshold) * 0.5f;
        }
      }
    }

    void Update() {
      float screechAmount = 0;
      var allPopped = true;
      var nonePopped = true;
      float alwaysScrape = 0;

      for (var i = 0; i < this.vp.wheels.Length; i++) {
        if (this.wheels[i].connected) {
          if (Mathf.Abs(F.MaxAbs(this.wheels[i].sidewaysSlip, this.wheels[i].forwardSlip, alwaysScrape))
              - this.slipThreshold
              > 0) {
            if (this.wheels[i].popped) {
              nonePopped = false;
            } else {
              allPopped = false;
            }
          }

          if (this.wheels[i].grounded) {
            this.surfaceType =
                GroundSurfaceMaster.surfaceTypesStatic[this.wheels[i].contactPoint.surfaceType];

            if (this.surfaceType.alwaysScrape) {
              alwaysScrape = this.slipThreshold + Mathf.Min(0.5f, Mathf.Abs(this.wheels[i].rawRPM * 0.001f));
            }
          }

          screechAmount = Mathf.Max(screechAmount,
                                    Mathf.Pow(Mathf.Clamp01(Mathf.Abs(F.MaxAbs(this.wheels[i].sidewaysSlip,
                                                                               this.wheels[i].forwardSlip,
                                                                               alwaysScrape))
                                                            - this.slipThreshold),
                                              2));
        }
      }

      //Set audio clip based on number of wheels popped
      if (this.surfaceType != null) {
        this.snd.clip = allPopped
                            ? this.surfaceType.rimSnd
                            : (nonePopped ? this.surfaceType.tireSnd : this.surfaceType.tireRimSnd);
      }

      //Set sound volume and pitch
      if (screechAmount > 0) {
        if (!this.snd.isPlaying) {
          this.snd.Play();
          this.snd.volume = 0;
        } else {
          this.snd.volume = Mathf.Lerp(this.snd.volume,
                                       screechAmount
                                       * ((this.vp.groundedWheels * 1.0f) / (this.wheels.Length * 1.0f)),
                                       2 * Time.deltaTime);
          this.snd.pitch = Mathf.Lerp(this.snd.pitch, 0.5f + screechAmount * 0.9f, 2 * Time.deltaTime);
        }
      } else if (this.snd.isPlaying) {
        this.snd.volume = 0;
        this.snd.Stop();
      }
    }
  }
}
