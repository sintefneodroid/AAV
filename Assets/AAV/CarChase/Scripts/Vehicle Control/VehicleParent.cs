using System;
using System.Collections;
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Hover;
using AAV.CarChase.Scripts.Scene;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AAV.CarChase.Scripts.Vehicle_Control {
  [RequireComponent(typeof(Rigidbody))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Vehicle Controllers/Vehicle Parent", 0)]

  //Vehicle root class
  public class VehicleParent : MonoBehaviour {
    [NonSerialized] public Rigidbody rb;
    [NonSerialized] public Transform tr;
    [NonSerialized] public Transform norm; //Normal orientation object

    [NonSerialized] public float accelInput;
    [NonSerialized] public float brakeInput;
    [NonSerialized] public float steerInput;
    [NonSerialized] public float ebrakeInput;
    [NonSerialized] public bool boostButton;
    [NonSerialized] public bool upshiftPressed;
    [NonSerialized] public bool downshiftPressed;
    [NonSerialized] public float upshiftHold;
    [NonSerialized] public float downshiftHold;
    [NonSerialized] public float pitchInput;
    [NonSerialized] public float yawInput;
    [NonSerialized] public float rollInput;

    [Tooltip("Accel axis is used for brake input")]
    public bool accelAxisIsBrake;

    [Tooltip("Brake input will act as reverse input")]
    public bool brakeIsReverse;

    [Tooltip("Automatically hold ebrake if it's pressed while parked")]
    public bool holdEbrakePark;

    public float burnoutThreshold = 0.9f;
    [NonSerialized] public float burnout;
    public float burnoutSpin = 5;
    [Range(0, 0.9f)] public float burnoutSmoothness = 0.5f;
    public Motor engine;

    bool stopUpshift;
    bool stopDownShift;

    [NonSerialized] public Vector3 localVelocity; //Local space velocity
    [NonSerialized] public Vector3 localAngularVel; //Local space angular velocity
    [NonSerialized] public Vector3 forwardDir; //Forward direction
    [NonSerialized] public Vector3 rightDir; //Right direction
    [NonSerialized] public Vector3 upDir; //Up direction
    [NonSerialized] public float forwardDot; //Dot product between forwardDir and GlobalControl.worldUpDir
    [NonSerialized] public float rightDot; //Dot product between rightDir and GlobalControl.worldUpDir
    [NonSerialized] public float upDot; //Dot product between upDir and GlobalControl.worldUpDir
    [NonSerialized] public float velMag; //Velocity magnitude
    [NonSerialized] public float sqrVelMag; //Velocity squared magnitude

    [NonSerialized] public bool reversing;

    public Wheel[] wheels;
    public HoverWheel[] hoverWheels;
    public WheelCheckGroup[] wheelGroups;
    bool wheelLoopDone;
    public bool hover;
    [NonSerialized] public int groundedWheels; //Number of wheels grounded
    [NonSerialized] public Vector3 wheelNormalAverage; //Average normal of the wheel contact points
    Vector3 wheelContactsVelocity; //Average velocity of wheel contact points

    [Tooltip("Lower center of mass by suspension height")]
    public bool suspensionCenterOfMass;

    public Vector3 centerOfMassOffset;

    public ForceMode wheelForceMode = ForceMode.Acceleration;
    public ForceMode suspensionForceMode = ForceMode.Acceleration;

    [Tooltip("Tow vehicle to instantiate")]
    public GameObject towVehicle;

    GameObject newTow;
    [NonSerialized] public VehicleParent inputInherit; //Vehicle which to inherit input from

    [NonSerialized] public bool crashing;

    [Header("Crashing")] public bool canCrash = true;
    public AudioSource crashSnd;
    public AudioClip[] crashClips;
    [NonSerialized] public bool playCrashSounds = true;
    public ParticleSystem sparks;
    [NonSerialized] public bool playCrashSparks = true;

    [Header("Camera")] public float cameraDistanceChange;
    public float cameraHeightChange;

    void Start() {
      this.tr = this.transform;
      this.rb = this.GetComponent<Rigidbody>();

      //Create normal orientation object
      var normTemp = new GameObject(this.tr.name + "'s Normal Orientation");
      this.norm = normTemp.transform;

      this.SetCenterOfMass();

      //Instantiate tow vehicle
      if (this.towVehicle) {
        this.newTow = Instantiate(this.towVehicle, Vector3.zero, this.tr.rotation);
        this.newTow.SetActive(false);
        this.newTow.transform.position =
            this.tr.TransformPoint(this.newTow.GetComponent<Joint>().connectedAnchor
                                   - this.newTow.GetComponent<Joint>().anchor);
        this.newTow.GetComponent<Joint>().connectedBody = this.rb;
        this.newTow.SetActive(true);
        this.newTow.GetComponent<VehicleParent>().inputInherit = this;
      }

      if (this.sparks) {
        this.sparks.transform.parent = null;
      }

      if (this.wheelGroups.Length > 0) {
        this.StartCoroutine(this.WheelCheckLoop());
      }
    }

    void Update() {
      //Shift single frame pressing logic
      if (this.stopUpshift) {
        this.upshiftPressed = false;
        this.stopUpshift = false;
      }

      if (this.stopDownShift) {
        this.downshiftPressed = false;
        this.stopDownShift = false;
      }

      if (this.upshiftPressed) {
        this.stopUpshift = true;
      }

      if (this.downshiftPressed) {
        this.stopDownShift = true;
      }

      if (this.inputInherit) {
        this.InheritInputOneShot();
      }

      //Norm orientation visualizing
      //Debug.DrawRay(norm.position, norm.forward, Color.blue);
      //Debug.DrawRay(norm.position, norm.up, Color.green);
      //Debug.DrawRay(norm.position, norm.right, Color.red);
    }

    void FixedUpdate() {
      if (this.inputInherit) {
        this.InheritInput();
      }

      if (this.wheelLoopDone && this.wheelGroups.Length > 0) {
        this.wheelLoopDone = false;
        this.StartCoroutine(this.WheelCheckLoop());
      }

      this.GetGroundedWheels();

      if (this.groundedWheels > 0) {
        this.crashing = false;
      }

      this.localVelocity = this.tr.InverseTransformDirection(this.rb.velocity - this.wheelContactsVelocity);
      this.localAngularVel = this.tr.InverseTransformDirection(this.rb.angularVelocity);
      this.velMag = this.rb.velocity.magnitude;
      this.sqrVelMag = this.rb.velocity.sqrMagnitude;
      this.forwardDir = this.tr.forward;
      this.rightDir = this.tr.right;
      this.upDir = this.tr.up;
      this.forwardDot = Vector3.Dot(this.forwardDir, GlobalControl.worldUpDir);
      this.rightDot = Vector3.Dot(this.rightDir, GlobalControl.worldUpDir);
      this.upDot = Vector3.Dot(this.upDir, GlobalControl.worldUpDir);
      this.norm.transform.position = this.tr.position;
      this.norm.transform.rotation =
          Quaternion.LookRotation(this.groundedWheels == 0 ? this.upDir : this.wheelNormalAverage,
                                  this.forwardDir);

      //Check if performing a burnout
      if (this.groundedWheels > 0
          && !this.hover
          && !this.accelAxisIsBrake
          && this.burnoutThreshold >= 0
          && this.accelInput > this.burnoutThreshold
          && this.brakeInput > this.burnoutThreshold) {
        this.burnout = Mathf.Lerp(this.burnout,
                                  ((5 - Mathf.Min(5, Mathf.Abs(this.localVelocity.z))) / 5)
                                  * Mathf.Abs(this.accelInput),
                                  Time.fixedDeltaTime * (1 - this.burnoutSmoothness) * 10);
      } else if (this.burnout > 0.01f) {
        this.burnout = Mathf.Lerp(this.burnout, 0, Time.fixedDeltaTime * (1 - this.burnoutSmoothness) * 10);
      } else {
        this.burnout = 0;
      }

      if (this.engine) {
        this.burnout *= this.engine.health;
      }

      //Check if reversing
      if (this.brakeIsReverse && this.brakeInput > 0 && this.localVelocity.z < 1 && this.burnout == 0) {
        this.reversing = true;
      } else if (this.localVelocity.z >= 0 || this.burnout > 0) {
        this.reversing = false;
      }
    }

    public void SetAccel(float f) {
      f = Mathf.Clamp(f, -1, 1);
      this.accelInput = f;
    }

    public void SetBrake(float f) {
      this.brakeInput = this.accelAxisIsBrake ? -Mathf.Clamp(this.accelInput, -1, 0) : Mathf.Clamp(f, -1, 1);
    }

    public void SetSteer(float f) { this.steerInput = Mathf.Clamp(f, -1, 1); }

    public void SetEbrake(float f) {
      if ((f > 0 || this.ebrakeInput > 0)
          && this.holdEbrakePark
          && this.velMag < 1
          && this.accelInput == 0
          && (this.brakeInput == 0 || !this.brakeIsReverse)) {
        this.ebrakeInput = 1;
      } else {
        this.ebrakeInput = Mathf.Clamp01(f);
      }
    }

    public void SetBoost(bool b) { this.boostButton = b; }

    public void SetPitch(float f) { this.pitchInput = Mathf.Clamp(f, -1, 1); }

    public void SetYaw(float f) { this.yawInput = Mathf.Clamp(f, -1, 1); }

    public void SetRoll(float f) { this.rollInput = Mathf.Clamp(f, -1, 1); }

    public void PressUpshift() { this.upshiftPressed = true; }

    public void PressDownshift() { this.downshiftPressed = true; }

    public void SetUpshift(float f) { this.upshiftHold = f; }

    public void SetDownshift(float f) { this.downshiftHold = f; }

    void InheritInput() {
      this.accelInput = this.inputInherit.accelInput;
      this.brakeInput = this.inputInherit.brakeInput;
      this.steerInput = this.inputInherit.steerInput;
      this.ebrakeInput = this.inputInherit.ebrakeInput;
      this.pitchInput = this.inputInherit.pitchInput;
      this.yawInput = this.inputInherit.yawInput;
      this.rollInput = this.inputInherit.rollInput;
    }

    void InheritInputOneShot() {
      this.upshiftPressed = this.inputInherit.upshiftPressed;
      this.downshiftPressed = this.inputInherit.downshiftPressed;
    }

    void SetCenterOfMass() {
      float susAverage = 0;

      //Get average suspension height
      if (this.suspensionCenterOfMass) {
        if (this.hover) {
          for (var i = 0; i < this.hoverWheels.Length; i++) {
            susAverage = i == 0
                             ? this.hoverWheels[i].hoverDistance
                             : (susAverage + this.hoverWheels[i].hoverDistance) * 0.5f;
          }
        } else {
          for (var i = 0; i < this.wheels.Length; i++) {
            var newSusDist = this.wheels[i].transform.parent.GetComponent<Suspension.Suspension>()
                                 .suspensionDistance;
            susAverage = i == 0 ? newSusDist : (susAverage + newSusDist) * 0.5f;
          }
        }
      }

      this.rb.centerOfMass = this.centerOfMassOffset + new Vector3(0, -susAverage, 0);
      this.rb.inertiaTensor =
          this.rb.inertiaTensor; //This is required due to decoupling of inertia tensor from center of mass in Unity 5.3
    }

    void GetGroundedWheels() {
      this.groundedWheels = 0;
      this.wheelContactsVelocity = Vector3.zero;

      if (this.hover) {
        for (var i = 0; i < this.hoverWheels.Length; i++) {
          if (this.hoverWheels[i].grounded) {
            this.wheelNormalAverage = i == 0
                                          ? this.hoverWheels[i].contactPoint.normal
                                          : (this.wheelNormalAverage
                                             + this.hoverWheels[i].contactPoint.normal).normalized;
          }

          if (this.hoverWheels[i].grounded) {
            this.groundedWheels++;
          }
        }
      } else {
        for (var i = 0; i < this.wheels.Length; i++) {
          if (this.wheels[i].grounded) {
            this.wheelContactsVelocity = i == 0
                                             ? this.wheels[i].contactVelocity
                                             : (this.wheelContactsVelocity + this.wheels[i].contactVelocity)
                                               * 0.5f;
            this.wheelNormalAverage = i == 0
                                          ? this.wheels[i].contactPoint.normal
                                          : (this.wheelNormalAverage + this.wheels[i].contactPoint.normal)
                                          .normalized;
          }

          if (this.wheels[i].grounded) {
            this.groundedWheels++;
          }
        }
      }
    }

    //Check for crashes and play collision sounds
    void OnCollisionEnter(Collision col) {
      if (col.contacts.Length > 0 && this.groundedWheels == 0) {
        foreach (var curCol in col.contacts) {
          if (!curCol.thisCollider.CompareTag("Underside")
              && curCol.thisCollider.gameObject.layer != GlobalControl.ignoreWheelCastLayer) {
            if (Vector3.Dot(curCol.normal, col.relativeVelocity.normalized) > 0.2f
                && col.relativeVelocity.sqrMagnitude > 20) {
              var checkTow = true;
              if (this.newTow) {
                checkTow = !curCol.otherCollider.transform.IsChildOf(this.newTow.transform);
              }

              if (checkTow) {
                this.crashing = this.canCrash;

                if (this.crashSnd && this.crashClips.Length > 0 && this.playCrashSounds) {
                  this.crashSnd.PlayOneShot(this.crashClips[Random.Range(0, this.crashClips.Length)],
                                            Mathf.Clamp01(col.relativeVelocity.magnitude * 0.1f));
                }

                if (this.sparks && this.playCrashSparks) {
                  this.sparks.transform.position = curCol.point;
                  this.sparks.transform.rotation =
                      Quaternion.LookRotation(col.relativeVelocity.normalized, curCol.normal);
                  this.sparks.Play();
                }
              }
            }
          }
        }
      }
    }

    void OnCollisionStay(Collision col) {
      if (col.contacts.Length > 0 && this.groundedWheels == 0) {
        foreach (var curCol in col.contacts) {
          if (!curCol.thisCollider.CompareTag("Underside")
              && curCol.thisCollider.gameObject.layer != GlobalControl.ignoreWheelCastLayer) {
            if (col.relativeVelocity.sqrMagnitude < 5) {
              var checkTow = true;

              if (this.newTow) {
                checkTow = !curCol.otherCollider.transform.IsChildOf(this.newTow.transform);
              }

              if (checkTow) {
                this.crashing = this.canCrash;
              }
            }
          }
        }
      }
    }

    void OnDestroy() {
      if (this.norm) {
        Destroy(this.norm.gameObject);
      }

      if (this.sparks) {
        Destroy(this.sparks.gameObject);
      }
    }

    //Loop through all wheel groups to check for wheel contacts
    IEnumerator WheelCheckLoop() {
      for (var i = 0; i < this.wheelGroups.Length; i++) {
        this.wheelGroups[i].Activate();
        this.wheelGroups[i == 0 ? this.wheelGroups.Length - 1 : i - 1].Deactivate();
        yield return new WaitForFixedUpdate();
      }

      this.wheelLoopDone = true;
    }
  }

  //Class for groups of wheels to check each FixedUpdate
  [Serializable]
  public class WheelCheckGroup {
    public Wheel[] wheels;
    public HoverWheel[] hoverWheels;

    public void Activate() {
      foreach (var curWheel in this.wheels) {
        curWheel.getContact = true;
      }

      foreach (var curHover in this.hoverWheels) {
        curHover.getContact = true;
      }
    }

    public void Deactivate() {
      foreach (var curWheel in this.wheels) {
        curWheel.getContact = false;
      }

      foreach (var curHover in this.hoverWheels) {
        curHover.getContact = false;
      }
    }
  }
}
