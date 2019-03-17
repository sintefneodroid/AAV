using System;
using System.Collections.Generic;
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Suspension {
  [RequireComponent(typeof(DriveForce))]
  [ExecuteInEditMode]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Suspension/Suspension", 0)]

  //Class for the suspensions
  public class Suspension : MonoBehaviour {
    [NonSerialized] public Transform tr;
    Rigidbody rb;
    VehicleParent vp;

    //Variables for inverting certain values on opposite sides of the vehicle
    [NonSerialized] public bool flippedSide;
    [NonSerialized] public float flippedSideFactor;
    [NonSerialized] public Quaternion initialRotation;

    public Wheel wheel;
    CapsuleCollider compressCol; //The hard collider

    [Tooltip("Generate a capsule collider for hard compressions")]
    public bool generateHardCollider = true;

    [Tooltip("Multiplier for the radius of the hard collider")]
    public float hardColliderRadiusFactor = 1;

    float hardColliderRadiusFactorPrev;
    float setHardColliderRadiusFactor;
    Transform compressTr; //Transform component of the hard collider

    [Header("Brakes and Steering")] public float brakeForce;
    public float ebrakeForce;

    [Range(-180, 180)] public float steerRangeMin;
    [Range(-180, 180)] public float steerRangeMax;

    [Tooltip("How much the wheel is steered")]
    public float steerFactor = 1;

    [Range(-1, 1)] public float steerAngle;
    [NonSerialized] public float steerDegrees;

    [Tooltip("Effect of Ackermann steering geometry")]
    public float ackermannFactor;

    [Tooltip("The camber of the wheel as it travels, x-axis = compression, y-axis = angle")]
    public AnimationCurve camberCurve = AnimationCurve.Linear(0, 0, 1, 0);

    [Range(-89.999f, 89.999f)] public float camberOffset;
    [NonSerialized] public float camberAngle;

    [Tooltip("Adjust the camber as if it was connected to a solid axle, opposite wheel must be set")]
    public bool solidAxleCamber;

    public Suspension oppositeWheel;

    [Tooltip("Angle at which the suspension points out to the side")]
    [Range(-89.999f, 89.999f)]
    public float sideAngle;

    [Range(-89.999f, 89.999f)] public float casterAngle;
    [Range(-89.999f, 89.999f)] public float toeAngle;

    [Tooltip("Wheel offset from its pivot point")]
    public float pivotOffset;

    [NonSerialized] public List<SuspensionPart> movingParts = new List<SuspensionPart>();

    [Header("Spring")] public float suspensionDistance;
    [NonSerialized] public float compression;

    [Tooltip("Should be left at 1 unless testing suspension travel")]
    [Range(0, 1)]
    public float targetCompression;

    [NonSerialized] public float penetration; //How deep the ground is interesecting with the wheel's tire
    public float springForce;

    [Tooltip("Force of the curve depending on it's compression, x-axis = compression, y-axis = force")]
    public AnimationCurve springForceCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Exponent for spring force based on compression")]
    public float springExponent = 1;

    public float springDampening;

    [Tooltip("How quickly the suspension extends if it's not grounded")]
    public float extendSpeed = 20;

    [Tooltip(
        "Apply forces to prevent the wheel from intersecting with the ground, not necessary if generating a hard collider")]
    public bool applyHardContactForce = true;

    public float hardContactForce = 50;
    public float hardContactSensitivity = 2;

    [Tooltip("Apply suspension forces at ground point")]
    public bool applyForceAtGroundContact = true;

    [Tooltip("Apply suspension forces along local up direction instead of ground normal")]
    public bool leaningForce;

    [NonSerialized]
    public Vector3 maxCompressPoint; //Position of the wheel when the suspension is compressed all the way

    [NonSerialized] public Vector3 springDirection;
    [NonSerialized] public Vector3 upDir; //Local up direction
    [NonSerialized] public Vector3 forwardDir; //Local forward direction

    [NonSerialized] public DriveForce targetDrive; //The drive being passed into the wheel

    [NonSerialized] public SuspensionPropertyToggle properties; //Property toggler
    [NonSerialized] public bool steerEnabled = true;
    [NonSerialized] public bool steerInverted;
    [NonSerialized] public bool driveEnabled = true;
    [NonSerialized] public bool driveInverted;
    [NonSerialized] public bool ebrakeEnabled = true;
    [NonSerialized] public bool skidSteerBrake;

    [Header("Damage")]
    [Tooltip("Point around which the suspension pivots when damaged")]
    public Vector3 damagePivot;

    [Tooltip("Compression amount to remain at when wheel is detached")]
    [Range(0, 1)]
    public float detachedCompression = 0.5f;

    public float jamForce = Mathf.Infinity;
    [NonSerialized] public bool jammed;

    void Start() {
      this.tr = this.transform;
      this.rb = (Rigidbody)F.GetTopmostParentComponent<Rigidbody>(this.tr);
      this.vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(this.tr);
      this.targetDrive = this.GetComponent<DriveForce>();
      this.flippedSide = Vector3.Dot(this.tr.forward, this.vp.transform.right) < 0;
      this.flippedSideFactor = this.flippedSide ? -1 : 1;
      this.initialRotation = this.tr.localRotation;

      if (Application.isPlaying) {
        this.GetCamber();

        //Generate the hard collider
        if (this.generateHardCollider) {
          var cap = new GameObject("Compress Collider");
          cap.layer = GlobalControl.ignoreWheelCastLayer;
          this.compressTr = cap.transform;
          this.compressTr.parent = this.tr;
          this.compressTr.localPosition = Vector3.zero;
          this.compressTr.localEulerAngles =
              new Vector3(this.camberAngle, 0, -this.casterAngle * this.flippedSideFactor);
          this.compressCol = cap.AddComponent<CapsuleCollider>();
          this.compressCol.direction = 1;
          this.setHardColliderRadiusFactor = this.hardColliderRadiusFactor;
          this.hardColliderRadiusFactorPrev = this.setHardColliderRadiusFactor;
          this.compressCol.radius = this.wheel.rimWidth * this.hardColliderRadiusFactor;
          this.compressCol.height = (this.wheel.popped
                                         ? this.wheel.rimRadius
                                         : Mathf.Lerp(this.wheel.rimRadius,
                                                      this.wheel.tireRadius,
                                                      this.wheel.tirePressure))
                                    * 2;
          this.compressCol.material = GlobalControl.frictionlessMatStatic;
        }

        this.steerRangeMax = Mathf.Max(this.steerRangeMin, this.steerRangeMax);

        this.properties = this.GetComponent<SuspensionPropertyToggle>();
        if (this.properties) {
          this.UpdateProperties();
        }
      }
    }

    void FixedUpdate() {
      this.upDir = this.tr.up;
      this.forwardDir = this.tr.forward;
      this.targetCompression = 1;

      this.GetCamber();

      this.GetSpringVectors();

      if (this.wheel.connected) {
        this.compression = Mathf.Min(this.targetCompression,
                                     this.suspensionDistance > 0
                                         ? Mathf.Clamp01(this.wheel.contactPoint.distance
                                                         / this.suspensionDistance)
                                         : 0);
        this.penetration = Mathf.Min(0, this.wheel.contactPoint.distance);
      } else {
        this.compression = this.detachedCompression;
        this.penetration = 0;
      }

      if (this.targetCompression > 0) {
        this.ApplySuspensionForce();
      }

      //Set hard collider size if it is changed during play mode
      if (this.generateHardCollider) {
        this.setHardColliderRadiusFactor = this.hardColliderRadiusFactor;

        if (this.hardColliderRadiusFactorPrev != this.setHardColliderRadiusFactor
            || this.wheel.updatedSize
            || this.wheel.updatedPopped) {
          if (this.wheel.rimWidth > this.wheel.actualRadius) {
            this.compressCol.direction = 2;
            this.compressCol.radius = this.wheel.actualRadius * this.hardColliderRadiusFactor;
            this.compressCol.height = this.wheel.rimWidth * 2;
          } else {
            this.compressCol.direction = 1;
            this.compressCol.radius = this.wheel.rimWidth * this.hardColliderRadiusFactor;
            this.compressCol.height = this.wheel.actualRadius * 2;
          }
        }

        this.hardColliderRadiusFactorPrev = this.setHardColliderRadiusFactor;
      }

      //Set the drive of the wheel
      if (this.wheel.connected) {
        if (this.wheel.targetDrive) {
          this.targetDrive.active = this.driveEnabled;
          this.targetDrive.feedbackRPM = this.wheel.targetDrive.feedbackRPM;
          this.wheel.targetDrive.SetDrive(this.targetDrive);
        }
      } else {
        this.targetDrive.feedbackRPM = this.targetDrive.rpm;
      }
    }

    void Update() {
      this.GetCamber();

      if (!Application.isPlaying) {
        this.GetSpringVectors();
      }

      //Set steer angle for the wheel
      this.steerDegrees = Mathf.Abs(this.steerAngle)
                          * (this.steerAngle > 0 ? this.steerRangeMax : this.steerRangeMin);
    }

    void ApplySuspensionForce() {
      if (this.wheel.grounded && this.wheel.connected) {
        //Get the local vertical velocity
        var travelVel = this.vp.norm.InverseTransformDirection(this.rb.GetPointVelocity(this.tr.position)).z;

        //Apply the suspension force
        if (this.suspensionDistance > 0 && this.targetCompression > 0) {
          var appliedSuspensionForce =
              (this.leaningForce
                   ? Vector3.Lerp(this.upDir,
                                  this.vp.norm.forward,
                                  Mathf.Abs(Mathf.Pow(Vector3.Dot(this.vp.norm.forward, this.vp.upDir), 5)))
                   : this.vp.norm.forward)
              * this.springForce
              * (Mathf.Pow(this.springForceCurve.Evaluate(1 - this.compression),
                           Mathf.Max(1, this.springExponent))
                 - (1 - this.targetCompression)
                 - this.springDampening * Mathf.Clamp(travelVel, -1, 1));

          this.rb.AddForceAtPosition(appliedSuspensionForce,
                                     this.applyForceAtGroundContact
                                         ? this.wheel.contactPoint.point
                                         : this.wheel.tr.position,
                                     this.vp.suspensionForceMode);

          //If wheel is resting on a rigidbody, apply opposing force to it
          if (this.wheel.contactPoint.col.attachedRigidbody) {
            this.wheel.contactPoint.col.attachedRigidbody.AddForceAtPosition(-appliedSuspensionForce,
                                                                             this.wheel.contactPoint.point,
                                                                             this.vp.suspensionForceMode);
          }
        }

        //Apply hard contact force
        if (this.compression == 0 && !this.generateHardCollider && this.applyHardContactForce) {
          this.rb.AddForceAtPosition(-this.vp.norm.TransformDirection(0,
                                                                      0,
                                                                      Mathf.Clamp(travelVel,
                                                                                  -this.hardContactSensitivity
                                                                                  * TimeMaster
                                                                                      .fixedTimeFactor,
                                                                                  0)
                                                                      + this.penetration)
                                     * this.hardContactForce
                                     * Mathf.Clamp01(TimeMaster.fixedTimeFactor),
                                     this.applyForceAtGroundContact
                                         ? this.wheel.contactPoint.point
                                         : this.wheel.tr.position,
                                     this.vp.suspensionForceMode);
        }
      }
    }

    void GetSpringVectors() {
      if (!Application.isPlaying) {
        this.tr = this.transform;
        this.flippedSide = Vector3.Dot(this.tr.forward, this.vp.transform.right) < 0;
        this.flippedSideFactor = this.flippedSide ? -1 : 1;
      }

      this.maxCompressPoint = this.tr.position;

      var casterDir = -Mathf.Sin(this.casterAngle * Mathf.Deg2Rad) * this.flippedSideFactor;
      var sideDir = -Mathf.Sin(this.sideAngle * Mathf.Deg2Rad);

      this.springDirection =
          this.tr.TransformDirection(casterDir,
                                     Mathf.Max(Mathf.Abs(casterDir), Mathf.Abs(sideDir)) - 1,
                                     sideDir).normalized;
    }

    void GetCamber() {
      if (this.solidAxleCamber && this.oppositeWheel && this.wheel.connected) {
        if (this.oppositeWheel.wheel.rim && this.wheel.rim) {
          var axleDir =
              this.tr.InverseTransformDirection((this.oppositeWheel.wheel.rim.position
                                                 - this.wheel.rim.position).normalized);
          this.camberAngle = Mathf.Atan2(axleDir.z, axleDir.y) * Mathf.Rad2Deg + 90 + this.camberOffset;
        }
      } else {
        this.camberAngle =
            this.camberCurve.Evaluate((Application.isPlaying && this.wheel.connected
                                           ? this.wheel.travelDist
                                           : this.targetCompression))
            + this.camberOffset;
      }
    }

    //Update the toggleable properties
    public void UpdateProperties() {
      if (this.properties) {
        foreach (var curProperty in this.properties.properties) {
          switch ((int)curProperty.property) {
            case 0:
              this.steerEnabled = curProperty.toggled;
              break;
            case 1:
              this.steerInverted = curProperty.toggled;
              break;
            case 2:
              this.driveEnabled = curProperty.toggled;
              break;
            case 3:
              this.driveInverted = curProperty.toggled;
              break;
            case 4:
              this.ebrakeEnabled = curProperty.toggled;
              break;
            case 5:
              this.skidSteerBrake = curProperty.toggled;
              break;
          }
        }
      }
    }

    //Visualize steer range
    void OnDrawGizmosSelected() {
      if (!this.tr) {
        this.tr = this.transform;
      }

      if (this.wheel) {
        if (this.wheel.rim) {
          var wheelPoint = this.wheel.rim.position;

          var camberSin = -Mathf.Sin(this.camberAngle * Mathf.Deg2Rad);
          var steerSin =
              Mathf.Sin(Mathf.Lerp(this.steerRangeMin, this.steerRangeMax, (this.steerAngle + 1) * 0.5f)
                        * Mathf.Deg2Rad);
          var minSteerSin = Mathf.Sin(this.steerRangeMin * Mathf.Deg2Rad);
          var maxSteerSin = Mathf.Sin(this.steerRangeMax * Mathf.Deg2Rad);

          Gizmos.color = Color.magenta;

          Gizmos.DrawWireSphere(wheelPoint, 0.05f);

          Gizmos.DrawLine(wheelPoint,
                          wheelPoint
                          + this.tr.TransformDirection(minSteerSin,
                                                       camberSin * (1 - Mathf.Abs(minSteerSin)),
                                                       Mathf.Cos(this.steerRangeMin * Mathf.Deg2Rad)
                                                       * (1 - Mathf.Abs(camberSin))).normalized);

          Gizmos.DrawLine(wheelPoint,
                          wheelPoint
                          + this.tr.TransformDirection(maxSteerSin,
                                                       camberSin * (1 - Mathf.Abs(maxSteerSin)),
                                                       Mathf.Cos(this.steerRangeMax * Mathf.Deg2Rad)
                                                       * (1 - Mathf.Abs(camberSin))).normalized);

          Gizmos.DrawLine(wheelPoint
                          + this.tr.TransformDirection(minSteerSin,
                                                       camberSin * (1 - Mathf.Abs(minSteerSin)),
                                                       Mathf.Cos(this.steerRangeMin * Mathf.Deg2Rad)
                                                       * (1 - Mathf.Abs(camberSin))).normalized
                          * 0.9f,
                          wheelPoint
                          + this.tr.TransformDirection(maxSteerSin,
                                                       camberSin * (1 - Mathf.Abs(maxSteerSin)),
                                                       Mathf.Cos(this.steerRangeMax * Mathf.Deg2Rad)
                                                       * (1 - Mathf.Abs(camberSin))).normalized
                          * 0.9f);

          Gizmos.DrawLine(wheelPoint,
                          wheelPoint
                          + this.tr.TransformDirection(steerSin,
                                                       camberSin * (1 - Mathf.Abs(steerSin)),
                                                       Mathf.Cos(this.steerRangeMin * Mathf.Deg2Rad)
                                                       * (1 - Mathf.Abs(camberSin))).normalized);
        }
      }

      Gizmos.color = Color.red;

      Gizmos.DrawWireSphere(this.tr.TransformPoint(this.damagePivot), 0.05f);
    }
  }
}
