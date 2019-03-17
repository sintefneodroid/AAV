using System;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Drivetrain {
  //Class for engines
  public abstract class Motor : MonoBehaviour {
    protected VehicleParent vp;
    public bool ignition;
    public float power = 1;

    [Tooltip("Throttle curve, x-axis = input, y-axis = output")]
    public AnimationCurve inputCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    protected float actualInput; //Input after applying the input curve

    protected AudioSource snd;

    [Header("Engine Audio")] public float minPitch;
    public float maxPitch;
    [NonSerialized] public float targetPitch;
    protected float pitchFactor;
    protected float airPitch;

    [Header("Nitrous Boost")] public bool canBoost = true;
    [NonSerialized] public bool boosting;
    public float boost = 1;
    bool boostReleased;
    bool boostPrev;

    [Tooltip("X-axis = local z-velocity, y-axis = power")]
    public AnimationCurve boostPowerCurve = AnimationCurve.EaseInOut(0, 0.1f, 50, 0.2f);

    public float maxBoost = 1;
    public float boostBurnRate = 0.01f;
    public AudioSource boostLoopSnd;
    AudioSource boostSnd; //AudioSource for boostStart and boostEnd
    public AudioClip boostStart;
    public AudioClip boostEnd;
    public ParticleSystem[] boostParticles;

    [Header("Damage")] [Range(0, 1)] public float strength = 1;
    [NonSerialized] public float health = 1;
    public float damagePitchWiggle;
    public ParticleSystem smoke;
    float initialSmokeEmission;

    public virtual void Start() {
      this.vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(this.transform);

      //Get engine sound
      this.snd = this.GetComponent<AudioSource>();
      if (this.snd) {
        this.snd.pitch = this.minPitch;
      }

      //Get boost sound
      if (this.boostLoopSnd) {
        var newBoost = Instantiate(this.boostLoopSnd.gameObject,
                                   this.boostLoopSnd.transform.position,
                                   this.boostLoopSnd.transform.rotation);
        this.boostSnd = newBoost.GetComponent<AudioSource>();
        this.boostSnd.transform.parent = this.boostLoopSnd.transform;
        this.boostSnd.transform.localPosition = Vector3.zero;
        this.boostSnd.transform.localRotation = Quaternion.identity;
        this.boostSnd.loop = false;
      }

      if (this.smoke) {
        this.initialSmokeEmission = this.smoke.emission.rateOverTime.constantMax;
      }
    }

    public virtual void FixedUpdate() {
      this.health = Mathf.Clamp01(this.health);

      //Boost logic
      this.boost =
          Mathf.Clamp(this.boosting
                          ? this.boost
                            - this.boostBurnRate * Time.timeScale * 0.05f * TimeMaster.inverseFixedTimeFactor
                          : this.boost,
                      0,
                      this.maxBoost);
      this.boostPrev = this.boosting;

      if (this.canBoost
          && this.ignition
          && this.health > 0
          && !this.vp.crashing
          && this.boost > 0
          && (this.vp.hover
                  ? this.vp.accelInput != 0 || Mathf.Abs(this.vp.localVelocity.z) > 1
                  : this.vp.accelInput > 0 || this.vp.localVelocity.z > 1)) {
        if (((this.boostReleased && !this.boosting) || this.boosting) && this.vp.boostButton) {
          this.boosting = true;
          this.boostReleased = false;
        } else {
          this.boosting = false;
        }
      } else {
        this.boosting = false;
      }

      if (!this.vp.boostButton) {
        this.boostReleased = true;
      }

      if (this.boostLoopSnd && this.boostSnd) {
        if (this.boosting && !this.boostLoopSnd.isPlaying) {
          this.boostLoopSnd.Play();
        } else if (!this.boosting && this.boostLoopSnd.isPlaying) {
          this.boostLoopSnd.Stop();
        }

        if (this.boosting && !this.boostPrev) {
          this.boostSnd.clip = this.boostStart;
          this.boostSnd.Play();
        } else if (!this.boosting && this.boostPrev) {
          this.boostSnd.clip = this.boostEnd;
          this.boostSnd.Play();
        }
      }
    }

    public virtual void Update() {
      //Set engine sound properties
      if (!this.ignition) {
        this.targetPitch = 0;
      }

      if (this.snd) {
        if (this.ignition && this.health > 0) {
          this.snd.enabled = true;
          this.snd.pitch =
              Mathf.Lerp(this.snd.pitch,
                         Mathf.Lerp(this.minPitch, this.maxPitch, this.targetPitch),
                         20 * Time.deltaTime)
              + Mathf.Sin(Time.time * 200 * (1 - this.health))
              * (1 - this.health)
              * 0.1f
              * this.damagePitchWiggle;
          this.snd.volume = Mathf.Lerp(this.snd.volume, 0.3f + this.targetPitch * 0.7f, 20 * Time.deltaTime);
        } else {
          this.snd.enabled = false;
        }
      }

      //Play boost particles
      if (this.boostParticles.Length > 0) {
        foreach (var curBoost in this.boostParticles) {
          if (this.boosting && curBoost.isStopped) {
            curBoost.Play();
          } else if (!this.boosting && curBoost.isPlaying) {
            curBoost.Stop();
          }
        }
      }

      if (this.smoke) {
        var em = this.smoke.emission;
        em.rateOverTime =
            new ParticleSystem.MinMaxCurve(this.health < 0.7f
                                               ? this.initialSmokeEmission * (1 - this.health)
                                               : 0);
      }
    }
  }
}
