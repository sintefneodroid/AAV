using System;
using AAV.CarChase.Scripts.Ground;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AAV.CarChase.Scripts.Drivetrain {
  [RequireComponent(typeof(DriveForce))]
  [ExecuteInEditMode]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Drivetrain/Wheel", 1)]

  //Class for the wheel
  public class Wheel : MonoBehaviour {
    [NonSerialized] public Transform tr;
    Rigidbody rb;
    [NonSerialized] public VehicleParent vp;
    [NonSerialized] public Suspension.Suspension suspensionParent;
    [NonSerialized] public Transform rim;
    Transform tire;
    Vector3 localVel;

    [Tooltip("Generate a sphere collider to represent the wheel for side collisions")]
    public bool generateHardCollider = true;

    SphereCollider sphereCol; //Hard collider
    Transform sphereColTr; //Hard collider transform

    [Header("Rotation")]
    [Tooltip("Bias for feedback RPM lerp between target RPM and raw RPM")]
    [Range(0, 1)]
    public float feedbackRpmBias;

    [Tooltip(
        "Curve for setting final RPM of wheel based on driving torque/brake force, x-axis = torque/brake force, y-axis = lerp between raw RPM and target RPM")]
    public AnimationCurve rpmBiasCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip(
        "As the RPM of the wheel approaches this value, the RPM bias curve is interpolated with the default linear curve")]
    public float rpmBiasCurveLimit = Mathf.Infinity;

    [Range(0, 10)] public float axleFriction;

    [Header("Friction")] [Range(0, 1)] public float frictionSmoothness = 0.5f;
    public float forwardFriction = 1;
    public float sidewaysFriction = 1;
    public float forwardRimFriction = 0.5f;
    public float sidewaysRimFriction = 0.5f;
    public float forwardCurveStretch = 1;
    public float sidewaysCurveStretch = 1;
    Vector3 frictionForce = Vector3.zero;

    [Tooltip("X-axis = slip, y-axis = friction")]
    public AnimationCurve forwardFrictionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("X-axis = slip, y-axis = friction")]
    public AnimationCurve sidewaysFrictionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [NonSerialized] public float forwardSlip;
    [NonSerialized] public float sidewaysSlip;

    public enum SlipDependenceMode {
      dependent,
      forward,
      sideways,
      independent
    }

    public SlipDependenceMode slipDependence = SlipDependenceMode.sideways;
    [Range(0, 2)] public float forwardSlipDependence = 2;
    [Range(0, 2)] public float sidewaysSlipDependence = 2;

    [Tooltip(
        "Adjusts how much friction the wheel has based on the normal of the ground surface. X-axis = normal dot product, y-axis = friction multiplier")]
    public AnimationCurve normalFrictionCurve = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("How much the suspension compression affects the wheel friction")]
    [Range(0, 1)]
    public float compressionFrictionFactor = 0.5f;

    [Header("Size")] public float tireRadius;
    public float rimRadius;
    public float tireWidth;
    public float rimWidth;

    [NonSerialized] public float setTireWidth;
    [NonSerialized] public float tireWidthPrev;
    [NonSerialized] public float setTireRadius;
    [NonSerialized] public float tireRadiusPrev;

    [NonSerialized] public float setRimWidth;
    [NonSerialized] public float rimWidthPrev;
    [NonSerialized] public float setRimRadius;
    [NonSerialized] public float rimRadiusPrev;

    [NonSerialized] public float actualRadius;

    [Header("Tire")] [Range(0, 1)] public float tirePressure = 1;
    [NonSerialized] public float setTirePressure;
    [NonSerialized] public float tirePressurePrev;
    float initialTirePressure;
    public bool popped;
    [NonSerialized] public bool setPopped;
    [NonSerialized] public bool poppedPrev;
    public bool canPop;

    [Tooltip("Requires deform shader")] public float deformAmount;
    Material rimMat;
    Material tireMat;
    float airLeakTime = -1;

    [Range(0, 1)] public float rimGlow;
    float glowAmount;
    Color glowColor;

    [NonSerialized] public bool updatedSize;
    [NonSerialized] public bool updatedPopped;

    float currentRPM;
    [NonSerialized] public DriveForce targetDrive;
    [NonSerialized] public float rawRPM; //RPM based purely on velocity
    [NonSerialized] public WheelContact contactPoint = new WheelContact();
    [NonSerialized] public bool getContact = true; //Should the wheel try to get contact info?
    [NonSerialized] public bool grounded;
    float airTime;
    [NonSerialized] public float travelDist;
    Vector3 upDir; //Up direction
    float circumference;

    [NonSerialized] public Vector3 contactVelocity; //Velocity of contact point
    float actualEbrake;
    float actualTargetRPM;
    float actualTorque;

    [NonSerialized] public Vector3 forceApplicationPoint; //Point at which friction forces are applied

    [Tooltip("Apply friction forces at ground point")]
    public bool applyForceAtGroundContact;

    [Header("Audio")] public AudioSource impactSnd;
    public AudioClip[] tireHitClips;
    public AudioClip rimHitClip;
    public AudioClip tireAirClip;
    public AudioClip tirePopClip;

    [Header("Damage")] public float detachForce = Mathf.Infinity;
    [NonSerialized] public float damage;
    public float mass = 0.05f;
    [NonSerialized] public bool canDetach;
    [NonSerialized] public bool connected = true;

    public Mesh tireMeshLoose; //Tire mesh for detached wheel collider
    public Mesh rimMeshLoose; //Rim mesh for detached wheel collider
    GameObject detachedWheel;
    GameObject detachedTire;
    MeshCollider detachedCol;
    Rigidbody detachedBody;
    MeshFilter detachFilter;
    MeshFilter detachTireFilter;
    public PhysicMaterial detachedTireMaterial;
    public PhysicMaterial detachedRimMaterial;

    void Start() {
      this.tr = this.transform;
      this.rb = (Rigidbody)F.GetTopmostParentComponent<Rigidbody>(this.tr);
      this.vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(this.tr);
      this.suspensionParent = this.tr.parent.GetComponent<Suspension.Suspension>();
      this.travelDist = this.suspensionParent.targetCompression;
      this.canDetach = this.detachForce < Mathf.Infinity && Application.isPlaying;
      this.initialTirePressure = this.tirePressure;

      if (this.tr.childCount > 0) {
        //Get rim
        this.rim = this.tr.GetChild(0);

        //Set up rim glow material
        if (this.rimGlow > 0 && Application.isPlaying) {
          this.rimMat = new Material(this.rim.GetComponent<MeshRenderer>().sharedMaterial);
          this.rimMat.EnableKeyword("_EMISSION");
          this.rim.GetComponent<MeshRenderer>().material = this.rimMat;
        }

        //Create detached wheel
        if (this.canDetach) {
          this.detachedWheel = new GameObject(this.vp.transform.name + "'s Detached Wheel");
          this.detachedWheel.layer = LayerMask.NameToLayer("Detachable Part");
          this.detachFilter = this.detachedWheel.AddComponent<MeshFilter>();
          this.detachFilter.sharedMesh = this.rim.GetComponent<MeshFilter>().sharedMesh;
          var detachRend = this.detachedWheel.AddComponent<MeshRenderer>();
          detachRend.sharedMaterial = this.rim.GetComponent<MeshRenderer>().sharedMaterial;
          this.detachedCol = this.detachedWheel.AddComponent<MeshCollider>();
          this.detachedCol.convex = true;
          this.detachedBody = this.detachedWheel.AddComponent<Rigidbody>();
          this.detachedBody.mass = this.mass;
        }

        //Get tire
        if (this.rim.childCount > 0) {
          this.tire = this.rim.GetChild(0);
          if (this.deformAmount > 0 && Application.isPlaying) {
            this.tireMat = new Material(this.tire.GetComponent<MeshRenderer>().sharedMaterial);
            this.tire.GetComponent<MeshRenderer>().material = this.tireMat;
          }

          //Create detached tire
          if (this.canDetach) {
            this.detachedTire = new GameObject("Detached Tire");
            this.detachedTire.transform.parent = this.detachedWheel.transform;
            this.detachedTire.transform.localPosition = Vector3.zero;
            this.detachedTire.transform.localRotation = Quaternion.identity;
            this.detachTireFilter = this.detachedTire.AddComponent<MeshFilter>();
            this.detachTireFilter.sharedMesh = this.tire.GetComponent<MeshFilter>().sharedMesh;
            var detachTireRend = this.detachedTire.AddComponent<MeshRenderer>();
            detachTireRend.sharedMaterial = this.tireMat
                                                ? this.tireMat
                                                : this.tire.GetComponent<MeshRenderer>().sharedMaterial;
          }
        }

        if (Application.isPlaying) {
          //Generate hard collider
          if (this.generateHardCollider) {
            var sphereColNew = new GameObject("Rim Collider");
            sphereColNew.layer = GlobalControl.ignoreWheelCastLayer;
            this.sphereColTr = sphereColNew.transform;
            this.sphereCol = sphereColNew.AddComponent<SphereCollider>();
            this.sphereColTr.parent = this.tr;
            this.sphereColTr.localPosition = Vector3.zero;
            this.sphereColTr.localRotation = Quaternion.identity;
            this.sphereCol.radius = Mathf.Min(this.rimWidth * 0.5f, this.rimRadius * 0.5f);
            this.sphereCol.material = GlobalControl.frictionlessMatStatic;
          }

          if (this.canDetach) {
            this.detachedWheel.SetActive(false);
          }
        }
      }

      this.targetDrive = this.GetComponent<DriveForce>();
      this.currentRPM = 0;
    }

    void FixedUpdate() {
      this.upDir = this.tr.up;
      this.actualRadius = this.popped
                              ? this.rimRadius
                              : Mathf.Lerp(this.rimRadius, this.tireRadius, this.tirePressure);
      this.circumference = Mathf.PI * this.actualRadius * 2;
      this.localVel = this.rb.GetPointVelocity(this.forceApplicationPoint);

      //Get proper inputs
      this.actualEbrake = this.suspensionParent.ebrakeEnabled ? this.suspensionParent.ebrakeForce : 0;
      this.actualTargetRPM = this.targetDrive.rpm * (this.suspensionParent.driveInverted ? -1 : 1);
      this.actualTorque = this.suspensionParent.driveEnabled
                              ? Mathf.Lerp(this.targetDrive.torque,
                                           Mathf.Abs(this.vp.accelInput),
                                           this.vp.burnout)
                              : 0;

      if (this.getContact) {
        this.GetWheelContact();
      } else if (this.grounded) {
        this.contactPoint.point += this.localVel * Time.fixedDeltaTime;
      }

      this.airTime = this.grounded ? 0 : this.airTime + Time.fixedDeltaTime;
      this.forceApplicationPoint =
          this.applyForceAtGroundContact ? this.contactPoint.point : this.tr.position;

      if (this.connected) {
        this.GetRawRPM();
        this.ApplyDrive();
      } else {
        this.rawRPM = 0;
        this.currentRPM = 0;
        this.targetDrive.feedbackRPM = 0;
      }

      //Get travel distance
      this.travelDist = this.suspensionParent.compression < this.travelDist || this.grounded
                            ? this.suspensionParent.compression
                            : Mathf.Lerp(this.travelDist,
                                         this.suspensionParent.compression,
                                         this.suspensionParent.extendSpeed * Time.fixedDeltaTime);

      this.PositionWheel();

      if (this.connected) {
        //Update hard collider size upon changed radius or width
        if (this.generateHardCollider) {
          this.setRimWidth = this.rimWidth;
          this.setRimRadius = this.rimRadius;
          this.setTireWidth = this.tireWidth;
          this.setTireRadius = this.tireRadius;
          this.setTirePressure = this.tirePressure;

          if (this.rimWidthPrev != this.setRimWidth || this.rimRadiusPrev != this.setRimRadius) {
            this.sphereCol.radius = Mathf.Min(this.rimWidth * 0.5f, this.rimRadius * 0.5f);
            this.updatedSize = true;
          } else if (this.tireWidthPrev != this.setTireWidth
                     || this.tireRadiusPrev != this.setTireRadius
                     || this.tirePressurePrev != this.setTirePressure) {
            this.updatedSize = true;
          } else {
            this.updatedSize = false;
          }

          this.rimWidthPrev = this.setRimWidth;
          this.rimRadiusPrev = this.setRimRadius;
          this.tireWidthPrev = this.setTireWidth;
          this.tireRadiusPrev = this.setTireRadius;
          this.tirePressurePrev = this.setTirePressure;
        }

        this.GetSlip();
        this.ApplyFriction();

        //Burnout spinning
        if (this.vp.burnout > 0
            && this.targetDrive.rpm != 0
            && this.actualEbrake * this.vp.ebrakeInput == 0
            && this.connected
            && this.grounded) {
          this.rb.AddForceAtPosition(this.suspensionParent.forwardDir
                                     * -this.suspensionParent.flippedSideFactor
                                     * (this.vp.steerInput
                                        * this.vp.burnoutSpin
                                        * this.currentRPM
                                        * Mathf.Min(0.1f, this.targetDrive.torque)
                                        * 0.001f)
                                     * this.vp.burnout
                                     * (this.popped ? 0.5f : 1)
                                     * this.contactPoint.surfaceFriction,
                                     this.suspensionParent.tr.position,
                                     this.vp.wheelForceMode);
        }

        //Popping logic
        this.setPopped = this.popped;

        if (this.poppedPrev != this.setPopped) {
          if (this.tire) {
            this.tire.gameObject.SetActive(!this.popped);
          }

          this.updatedPopped = true;
        } else {
          this.updatedPopped = false;
        }

        this.poppedPrev = this.setPopped;

        //Air leak logic
        if (this.airLeakTime >= 0) {
          this.tirePressure = Mathf.Clamp01(this.tirePressure - Time.fixedDeltaTime * 0.5f);

          if (this.grounded) {
            this.airLeakTime += Mathf.Max(Mathf.Abs(this.currentRPM) * 0.001f, this.localVel.magnitude * 0.1f)
                                * Time.timeScale
                                * TimeMaster.inverseFixedTimeFactor;

            if (this.airLeakTime > 1000 && this.tirePressure == 0) {
              this.popped = true;
              this.airLeakTime = -1;

              if (this.impactSnd && this.tirePopClip) {
                this.impactSnd.PlayOneShot(this.tirePopClip);
                this.impactSnd.pitch = 1;
              }
            }
          }
        }
      }
    }

    void Update() {
      this.RotateWheel();

      if (!Application.isPlaying) {
        this.PositionWheel();
      } else {
        if (this.deformAmount > 0 && this.tireMat && this.connected) {
          if (this.tireMat.HasProperty("_DeformNormal")) {
            //Deform tire (requires deform shader)
            var deformNormal = this.grounded
                                   ? this.contactPoint.normal
                                     * Mathf.Max(-this.suspensionParent.penetration
                                                 * (1 - this.suspensionParent.compression)
                                                 * 10,
                                                 1 - this.tirePressure)
                                     * this.deformAmount
                                   : Vector3.zero;
            this.tireMat.SetVector("_DeformNormal",
                                   new Vector4(deformNormal.x, deformNormal.y, deformNormal.z, 0));
          }
        }

        if (this.rimMat) {
          if (this.rimMat.HasProperty("_EmissionColor")) {
            //Make the rim glow
            var targetGlow =
                this.connected
                && GroundSurfaceMaster.surfaceTypesStatic[this.contactPoint.surfaceType].leaveSparks
                    ? Mathf.Abs(F.MaxAbs(this.forwardSlip, this.sidewaysSlip))
                    : 0;
            this.glowAmount = this.popped
                                  ? Mathf.Lerp(this.glowAmount,
                                               targetGlow,
                                               (targetGlow > this.glowAmount ? 2 : 0.2f) * Time.deltaTime)
                                  : 0;
            this.glowColor = new Color(this.glowAmount, this.glowAmount * 0.5f, 0);
            this.rimMat.SetColor("_EmissionColor",
                                 this.popped
                                     ? Color.Lerp(Color.black, this.glowColor, this.glowAmount * this.rimGlow)
                                     : Color.black);
          }
        }
      }
    }

    void GetWheelContact() {
      var castDist =
          Mathf.Max(this.suspensionParent.suspensionDistance
                    * Mathf.Max(0.001f, this.suspensionParent.targetCompression)
                    + this.actualRadius,
                    0.001f);
      var wheelHits = Physics.RaycastAll(this.suspensionParent.maxCompressPoint,
                                         this.suspensionParent.springDirection,
                                         castDist,
                                         GlobalControl.wheelCastMaskStatic);
      RaycastHit hit;
      var hitIndex = 0;
      var validHit = false;
      var hitDist = Mathf.Infinity;

      if (this.connected) {
        //Loop through raycast hits to find closest one
        for (var i = 0; i < wheelHits.Length; i++) {
          if (!wheelHits[i].transform.IsChildOf(this.vp.tr) && wheelHits[i].distance < hitDist) {
            hitIndex = i;
            hitDist = wheelHits[i].distance;
            validHit = true;
          }
        }
      } else {
        validHit = false;
      }

      //Set contact point variables
      if (validHit) {
        hit = wheelHits[hitIndex];

        if (!this.grounded
            && this.impactSnd
            && ((this.tireHitClips.Length > 0 && !this.popped) || (this.rimHitClip && this.popped))) {
          this.impactSnd.PlayOneShot(this.popped
                                         ? this.rimHitClip
                                         : this.tireHitClips[Mathf.RoundToInt(Random.Range(0,
                                                                                           this.tireHitClips
                                                                                               .Length
                                                                                           - 1))],
                                     Mathf.Clamp01(this.airTime * this.airTime));
          this.impactSnd.pitch = Mathf.Clamp(this.airTime * 0.2f + 0.8f, 0.8f, 1);
        }

        this.grounded = true;
        this.contactPoint.distance = hit.distance - this.actualRadius;
        this.contactPoint.point = hit.point + this.localVel * Time.fixedDeltaTime;
        this.contactPoint.grounded = true;
        this.contactPoint.normal = hit.normal;
        this.contactPoint.relativeVelocity = this.tr.InverseTransformDirection(this.localVel);
        this.contactPoint.col = hit.collider;

        if (hit.collider.attachedRigidbody) {
          this.contactVelocity = hit.collider.attachedRigidbody.GetPointVelocity(this.contactPoint.point);
          this.contactPoint.relativeVelocity -= this.tr.InverseTransformDirection(this.contactVelocity);
        } else {
          this.contactVelocity = Vector3.zero;
        }

        var curSurface = hit.collider.GetComponent<GroundSurfaceInstance>();
        var curTerrain = hit.collider.GetComponent<TerrainSurface>();

        if (curSurface) {
          this.contactPoint.surfaceFriction = curSurface.friction;
          this.contactPoint.surfaceType = curSurface.surfaceType;
        } else if (curTerrain) {
          this.contactPoint.surfaceType = curTerrain.GetDominantSurfaceTypeAtPoint(this.contactPoint.point);
          this.contactPoint.surfaceFriction = curTerrain.GetFriction(this.contactPoint.surfaceType);
        } else {
          this.contactPoint.surfaceFriction = hit.collider.material.dynamicFriction * 2;
          this.contactPoint.surfaceType = 0;
        }

        if (this.contactPoint.col.CompareTag("Pop Tire")
            && this.canPop
            && this.airLeakTime == -1
            && !this.popped) {
          this.Deflate();
        }
      } else {
        this.grounded = false;
        this.contactPoint.distance = this.suspensionParent.suspensionDistance;
        this.contactPoint.point = Vector3.zero;
        this.contactPoint.grounded = false;
        this.contactPoint.normal = this.upDir;
        this.contactPoint.relativeVelocity = Vector3.zero;
        this.contactPoint.col = null;
        this.contactVelocity = Vector3.zero;
        this.contactPoint.surfaceFriction = 0;
        this.contactPoint.surfaceType = 0;
      }
    }

    void GetRawRPM() {
      if (this.grounded) {
        this.rawRPM = (this.contactPoint.relativeVelocity.x / this.circumference)
                      * (Mathf.PI * 100)
                      * -this.suspensionParent.flippedSideFactor;
      } else {
        this.rawRPM = Mathf.Lerp(this.rawRPM,
                                 this.actualTargetRPM,
                                 (this.actualTorque
                                  + this.suspensionParent.brakeForce * this.vp.brakeInput
                                  + this.actualEbrake * this.vp.ebrakeInput)
                                 * Time.timeScale);
      }
    }

    void GetSlip() {
      if (this.grounded) {
        this.sidewaysSlip = (this.contactPoint.relativeVelocity.z * 0.1f) / this.sidewaysCurveStretch;
        this.forwardSlip = (0.01f * (this.rawRPM - this.currentRPM)) / this.forwardCurveStretch;
      } else {
        this.sidewaysSlip = 0;
        this.forwardSlip = 0;
      }
    }

    void ApplyFriction() {
      if (this.grounded) {
        var forwardSlipFactor = (int)this.slipDependence == 0 || (int)this.slipDependence == 1
                                    ? this.forwardSlip - this.sidewaysSlip
                                    : this.forwardSlip;
        var sidewaysSlipFactor = (int)this.slipDependence == 0 || (int)this.slipDependence == 2
                                     ? this.sidewaysSlip - this.forwardSlip
                                     : this.sidewaysSlip;
        var forwardSlipDependenceFactor =
            Mathf.Clamp01(this.forwardSlipDependence - Mathf.Clamp01(Mathf.Abs(this.sidewaysSlip)));
        var sidewaysSlipDependenceFactor =
            Mathf.Clamp01(this.sidewaysSlipDependence - Mathf.Clamp01(Mathf.Abs(this.forwardSlip)));

        this.frictionForce = Vector3.Lerp(this.frictionForce,
                                          this.tr.TransformDirection(this.forwardFrictionCurve
                                                                         .Evaluate(Mathf
                                                                                       .Abs(forwardSlipFactor))
                                                                     * -Math.Sign(this.forwardSlip)
                                                                     * (this.popped
                                                                            ? this.forwardRimFriction
                                                                            : this.forwardFriction)
                                                                     * forwardSlipDependenceFactor
                                                                     * -this.suspensionParent
                                                                            .flippedSideFactor,
                                                                     0,
                                                                     this.sidewaysFrictionCurve
                                                                         .Evaluate(Mathf
                                                                                       .Abs(sidewaysSlipFactor))
                                                                     * -Math.Sign(this.sidewaysSlip)
                                                                     * (this.popped
                                                                            ? this.sidewaysRimFriction
                                                                            : this.sidewaysFriction)
                                                                     * sidewaysSlipDependenceFactor
                                                                     * this.normalFrictionCurve
                                                                           .Evaluate(Mathf.Clamp01(Vector3
                                                                                                       .Dot(this
                                                                                                            .contactPoint
                                                                                                            .normal,
                                                                                                            GlobalControl
                                                                                                                .worldUpDir)))
                                                                     * (this.vp.burnout > 0
                                                                        && Mathf.Abs(this.targetDrive.rpm)
                                                                        != 0
                                                                        && this.actualEbrake
                                                                        * this.vp.ebrakeInput
                                                                        == 0
                                                                        && this.grounded
                                                                            ? (1 - this.vp.burnout)
                                                                              * (1
                                                                                 - Mathf
                                                                                     .Abs(this.vp.accelInput))
                                                                            : 1))
                                          * ((1 - this.compressionFrictionFactor)
                                             + (1 - this.suspensionParent.compression)
                                             * this.compressionFrictionFactor
                                             * Mathf.Clamp01(Mathf.Abs(this.suspensionParent.tr
                                                                           .InverseTransformDirection(this
                                                                                                          .localVel)
                                                                           .z)
                                                             * 10))
                                          * this.contactPoint.surfaceFriction,
                                          1 - this.frictionSmoothness);

        this.rb.AddForceAtPosition(this.frictionForce, this.forceApplicationPoint, this.vp.wheelForceMode);

        //If resting on a rigidbody, apply opposing force to it
        if (this.contactPoint.col.attachedRigidbody) {
          this.contactPoint.col.attachedRigidbody.AddForceAtPosition(-this.frictionForce,
                                                                     this.contactPoint.point,
                                                                     this.vp.wheelForceMode);
        }
      }
    }

    void ApplyDrive() {
      float brakeForce = 0;
      var brakeCheckValue = this.suspensionParent.skidSteerBrake
                                ? this.vp.localAngularVel.y
                                : this.vp.localVelocity.z;

      //Set brake force
      if (this.vp.brakeIsReverse) {
        if (brakeCheckValue > 0) {
          brakeForce = this.suspensionParent.brakeForce * this.vp.brakeInput;
        } else if (brakeCheckValue <= 0) {
          brakeForce = this.suspensionParent.brakeForce * Mathf.Clamp01(this.vp.accelInput);
        }
      } else {
        brakeForce = this.suspensionParent.brakeForce * this.vp.brakeInput;
      }

      brakeForce += this.axleFriction * 0.1f * (Mathf.Approximately(this.actualTorque, 0) ? 1 : 0);

      if (this.targetDrive.rpm != 0) {
        brakeForce *= (1 - this.vp.burnout);
      }

      //Set final RPM
      if (!this.suspensionParent.jammed && this.connected) {
        var validTorque =
            (!(Mathf.Approximately(this.actualTorque, 0) && Mathf.Abs(this.actualTargetRPM) < 0.01f)
             && !Mathf.Approximately(this.actualTargetRPM, 0))
            || brakeForce + this.actualEbrake * this.vp.ebrakeInput > 0;

        this.currentRPM = Mathf.Lerp(this.rawRPM,
                                     Mathf.Lerp(Mathf.Lerp(this.rawRPM,
                                                           this.actualTargetRPM,
                                                           validTorque
                                                               ? this.EvaluateTorque(this.actualTorque)
                                                               : this.actualTorque),
                                                0,
                                                Mathf.Max(brakeForce,
                                                          this.actualEbrake * this.vp.ebrakeInput)),
                                     validTorque
                                         ? this.EvaluateTorque(this.actualTorque
                                                               + brakeForce
                                                               + this.actualEbrake * this.vp.ebrakeInput)
                                         : this.actualTorque
                                           + brakeForce
                                           + this.actualEbrake * this.vp.ebrakeInput);

        this.targetDrive.feedbackRPM = Mathf.Lerp(this.currentRPM, this.rawRPM, this.feedbackRpmBias);
      } else {
        this.currentRPM = 0;
        this.targetDrive.feedbackRPM = 0;
      }
    }

    //Extra method for evaluating torque to make the ApplyDrive method more readable
    float EvaluateTorque(float t) {
      var torque = Mathf.Lerp(this.rpmBiasCurve.Evaluate(t),
                              t,
                              this.rawRPM / (this.rpmBiasCurveLimit * Mathf.Sign(this.actualTargetRPM)));
      return torque;
    }

    void PositionWheel() {
      if (this.suspensionParent) {
        this.rim.position = this.suspensionParent.maxCompressPoint
                            + this.suspensionParent.springDirection
                            * this.suspensionParent.suspensionDistance
                            * (Application.isPlaying
                                   ? this.travelDist
                                   : this.suspensionParent.targetCompression)
                            + this.suspensionParent.upDir
                            * Mathf.Pow(Mathf.Max(Mathf.Abs(Mathf.Sin(this.suspensionParent.sideAngle
                                                                      * Mathf.Deg2Rad)),
                                                  Mathf.Abs(Mathf.Sin(this.suspensionParent.casterAngle
                                                                      * Mathf.Deg2Rad))),
                                        2)
                            * this.actualRadius
                            + this.suspensionParent.pivotOffset
                            * this.suspensionParent.tr.TransformDirection(Mathf.Sin(this.tr.localEulerAngles.y
                                                                                    * Mathf.Deg2Rad),
                                                                          0,
                                                                          Mathf.Cos(this.tr.localEulerAngles.y
                                                                                    * Mathf.Deg2Rad))
                            - this.suspensionParent.pivotOffset
                            * (Application.isPlaying
                                   ? this.suspensionParent.forwardDir
                                   : this.suspensionParent.tr.forward);
      }

      if (Application.isPlaying && this.generateHardCollider && this.connected) {
        this.sphereColTr.position = this.rim.position;
      }
    }

    void RotateWheel() {
      if (this.tr && this.suspensionParent) {
        var ackermannVal =
            Mathf.Sign(this.suspensionParent.steerAngle) == this.suspensionParent.flippedSideFactor
                ? 1 + this.suspensionParent.ackermannFactor
                : 1 - this.suspensionParent.ackermannFactor;
        this.tr.localEulerAngles =
            new Vector3(this.suspensionParent.camberAngle
                        + this.suspensionParent.casterAngle
                        * this.suspensionParent.steerAngle
                        * this.suspensionParent.flippedSideFactor,
                        -this.suspensionParent.toeAngle * this.suspensionParent.flippedSideFactor
                        + this.suspensionParent.steerDegrees * ackermannVal,
                        0);
      }

      if (Application.isPlaying) {
        this.rim.Rotate(Vector3.forward,
                        this.currentRPM * this.suspensionParent.flippedSideFactor * Time.deltaTime);

        if (this.damage > 0) {
          this.rim.localEulerAngles =
              new Vector3(Mathf.Sin(-this.rim.localEulerAngles.z * Mathf.Deg2Rad)
                          * Mathf.Clamp(this.damage, 0, 10),
                          Mathf.Cos(-this.rim.localEulerAngles.z * Mathf.Deg2Rad)
                          * Mathf.Clamp(this.damage, 0, 10),
                          this.rim.localEulerAngles.z);
        } else if (this.rim.localEulerAngles.x != 0 || this.rim.localEulerAngles.y != 0) {
          this.rim.localEulerAngles = new Vector3(0, 0, this.rim.localEulerAngles.z);
        }
      }
    }

    public void Deflate() {
      this.airLeakTime = 0;

      if (this.impactSnd && this.tireAirClip) {
        this.impactSnd.PlayOneShot(this.tireAirClip);
        this.impactSnd.pitch = 1;
      }
    }

    public void FixTire() {
      this.popped = false;
      this.tirePressure = this.initialTirePressure;
      this.airLeakTime = -1;
    }

    public void Detach() {
      if (this.connected && this.canDetach) {
        this.connected = false;
        this.detachedWheel.SetActive(true);
        this.detachedWheel.transform.position = this.rim.position;
        this.detachedWheel.transform.rotation = this.rim.rotation;
        this.detachedCol.sharedMaterial = this.popped ? this.detachedRimMaterial : this.detachedTireMaterial;

        if (this.tire) {
          this.detachedTire.SetActive(!this.popped);
          this.detachedCol.sharedMesh = this.airLeakTime >= 0 || this.popped
                                            ? (this.rimMeshLoose
                                                   ? this.rimMeshLoose
                                                   : this.detachFilter.sharedMesh)
                                            : (this.tireMeshLoose
                                                   ? this.tireMeshLoose
                                                   : this.detachTireFilter.sharedMesh);
        } else {
          this.detachedCol.sharedMesh = this.rimMeshLoose ? this.rimMeshLoose : this.detachFilter.sharedMesh;
        }

        this.rb.mass -= this.mass;
        this.detachedBody.velocity = this.rb.GetPointVelocity(this.rim.position);
        this.detachedBody.angularVelocity = this.rb.angularVelocity;

        this.rim.gameObject.SetActive(false);

        if (this.sphereColTr) {
          this.sphereColTr.gameObject.SetActive(false);
        }
      }
    }

    //Automatically sets wheel dimensions based on rim/tire meshes
    public void GetWheelDimensions(float radiusMargin, float widthMargin) {
      Mesh rimMesh = null;
      Mesh tireMesh = null;
      Mesh checker;
      var scaler = this.transform;

      if (this.transform.childCount > 0) {
        if (this.transform.GetChild(0).GetComponent<MeshFilter>()) {
          rimMesh = this.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh;
          scaler = this.transform.GetChild(0);
        }

        if (this.transform.GetChild(0).childCount > 0) {
          if (this.transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>()) {
            tireMesh = this.transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>().sharedMesh;
          }
        }

        checker = tireMesh ? tireMesh : rimMesh;

        if (checker) {
          float maxWidth = 0;
          float maxRadius = 0;

          foreach (var curVert in checker.vertices) {
            if (new Vector2(curVert.x * scaler.localScale.x, curVert.y * scaler.localScale.y).magnitude
                > maxRadius) {
              maxRadius = new Vector2(curVert.x * scaler.localScale.x, curVert.y * scaler.localScale.y)
                  .magnitude;
            }

            if (Mathf.Abs(curVert.z * scaler.localScale.z) > maxWidth) {
              maxWidth = Mathf.Abs(curVert.z * scaler.localScale.z);
            }
          }

          this.tireRadius = maxRadius + radiusMargin;
          this.tireWidth = maxWidth + widthMargin;

          if (tireMesh && rimMesh) {
            maxWidth = 0;
            maxRadius = 0;

            foreach (var curVert in rimMesh.vertices) {
              if (new Vector2(curVert.x * scaler.localScale.x, curVert.y * scaler.localScale.y).magnitude
                  > maxRadius) {
                maxRadius = new Vector2(curVert.x * scaler.localScale.x, curVert.y * scaler.localScale.y)
                    .magnitude;
              }

              if (Mathf.Abs(curVert.z * scaler.localScale.z) > maxWidth) {
                maxWidth = Mathf.Abs(curVert.z * scaler.localScale.z);
              }
            }

            this.rimRadius = maxRadius + radiusMargin;
            this.rimWidth = maxWidth + widthMargin;
          } else {
            this.rimRadius = maxRadius * 0.5f + radiusMargin;
            this.rimWidth = maxWidth * 0.5f + widthMargin;
          }
        } else {
          Debug.LogError("No rim or tire meshes found for getting wheel dimensions.", this);
        }
      }
    }

    public void Reattach() {
      if (!this.connected) {
        this.connected = true;
        this.detachedWheel.SetActive(false);
        this.rb.mass += this.mass;
        this.rim.gameObject.SetActive(true);

        if (this.sphereColTr) {
          this.sphereColTr.gameObject.SetActive(true);
        }
      }
    }

    //visualize wheel
    void OnDrawGizmosSelected() {
      this.tr = this.transform;

      if (this.tr.childCount > 0) {
        this.rim = this.tr.GetChild(0);

        if (this.rim.childCount > 0) {
          this.tire = this.rim.GetChild(0);
        }
      }

      var tireActualRadius = Mathf.Lerp(this.rimRadius, this.tireRadius, this.tirePressure);

      if (this.tirePressure < 1 && this.tirePressure > 0) {
        Gizmos.color = new Color(1, 1, 0, this.popped ? 0.5f : 1);
        GizmosExtra.DrawWireCylinder(this.rim.position,
                                     this.rim.forward,
                                     tireActualRadius,
                                     this.tireWidth * 2);
      }

      Gizmos.color = Color.white;
      GizmosExtra.DrawWireCylinder(this.rim.position, this.rim.forward, this.tireRadius, this.tireWidth * 2);

      Gizmos.color = this.tirePressure == 0 || this.popped ? Color.green : Color.cyan;
      GizmosExtra.DrawWireCylinder(this.rim.position, this.rim.forward, this.rimRadius, this.rimWidth * 2);

      Gizmos.color = new Color(1, 1, 1, this.tirePressure < 1 ? 0.5f : 1);
      GizmosExtra.DrawWireCylinder(this.rim.position, this.rim.forward, this.tireRadius, this.tireWidth * 2);

      Gizmos.color = this.tirePressure == 0 || this.popped ? Color.green : Color.cyan;
      GizmosExtra.DrawWireCylinder(this.rim.position, this.rim.forward, this.rimRadius, this.rimWidth * 2);
    }

    //Destroy detached wheel
    void OnDestroy() {
      if (Application.isPlaying) {
        if (this.detachedWheel) {
          Destroy(this.detachedWheel);
        }
      }
    }
  }

  //Contact point class
  public class WheelContact {
    public bool grounded; //Is the contact point grounded?
    public Collider col; //The collider of the contact point
    public Vector3 point; //The position of the contact point
    public Vector3 normal; //The normal of the contact point
    public Vector3 relativeVelocity; //Relative velocity between the wheel and the contact point object
    public float distance; //Distance from the suspension to the contact point minus the wheel radius
    public float surfaceFriction; //Friction of the contact surface
    public int surfaceType; //The surface type identified by the surface types array of GroundSurfaceMaster
  }
}
