using System;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Hover {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Hover/Hover Wheel", 1)]

  //Class for hover vehicle wheels
  public class HoverWheel : MonoBehaviour {
    Transform tr;
    Rigidbody rb;
    VehicleParent vp;

    [NonSerialized] public HoverContact contactPoint = new HoverContact(); //Contact points of the wheels
    [NonSerialized] public bool getContact = true; //Should the wheel try to get contact info?
    [NonSerialized] public bool grounded;
    public float hoverDistance;

    [Tooltip(
        "If the distance to the ground is less than this, extra hovering force will be applied based on the buffer float force")]
    public float bufferDistance;

    Vector3 upDir; //Local up direction

    [NonSerialized] public bool doFloat; //Is the wheel turned on?
    public float floatForce = 1;
    public float bufferFloatForce = 2;

    [Tooltip(
        "Strength of the suspension depending on how compressed it is, x-axis = compression, y-axis = force")]
    public AnimationCurve floatForceCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public float floatExponent = 1;
    public float floatDampening;
    float compression; //How compressed the suspension is

    [NonSerialized] public float targetSpeed;
    [NonSerialized] public float targetForce;
    float flippedSideFactor; //Multiplier for inverting the forces on opposite sides
    public float brakeForce = 1;
    public float ebrakeForce = 2;
    [NonSerialized] public float steerRate;

    [Tooltip("How much the wheel steers")] public float steerFactor;
    public float sideFriction;

    [Header("Visual Wheel")] public Transform visualWheel;
    public float visualTiltRate = 10;
    public float visualTiltAmount = 0.5f;

    GameObject detachedWheel;
    MeshCollider detachedCol;
    Rigidbody detachedBody;
    MeshFilter detachFilter;

    [Header("Damage")] public float detachForce = Mathf.Infinity;
    public float mass = 0.05f;
    [NonSerialized] public bool connected = true;
    [NonSerialized] public bool canDetach;
    public Mesh wheelMeshLoose; //Mesh for detached wheel collider
    public PhysicMaterial detachedWheelMaterial;

    void Start() {
      this.tr = this.transform;
      this.rb = (Rigidbody)F.GetTopmostParentComponent<Rigidbody>(this.tr);
      this.vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(this.tr);
      this.flippedSideFactor = Vector3.Dot(this.tr.forward, this.vp.transform.right) < 0 ? 1 : -1;
      this.canDetach = this.detachForce < Mathf.Infinity && Application.isPlaying;
      this.bufferDistance = Mathf.Min(this.hoverDistance, this.bufferDistance);

      if (this.canDetach) {
        this.detachedWheel = new GameObject(this.vp.transform.name + "'s Detached Wheel");
        this.detachedWheel.layer = LayerMask.NameToLayer("Detachable Part");
        this.detachFilter = this.detachedWheel.AddComponent<MeshFilter>();
        this.detachFilter.sharedMesh = this.visualWheel.GetComponent<MeshFilter>().sharedMesh;
        var detachRend = this.detachedWheel.AddComponent<MeshRenderer>();
        detachRend.sharedMaterial = this.visualWheel.GetComponent<MeshRenderer>().sharedMaterial;
        this.detachedCol = this.detachedWheel.AddComponent<MeshCollider>();
        this.detachedCol.convex = true;
        this.detachedBody = this.detachedWheel.AddComponent<Rigidbody>();
        this.detachedBody.mass = this.mass;
        this.detachedWheel.SetActive(false);
      }
    }

    void Update() {
      //Tilt the visual wheel
      if (this.visualWheel && this.connected) {
        this.TiltWheel();
      }
    }

    void FixedUpdate() {
      this.upDir = this.tr.up;

      if (this.getContact) {
        this.GetWheelContact();
      } else if (this.grounded) {
        this.contactPoint.point += this.rb.GetPointVelocity(this.tr.position) * Time.fixedDeltaTime;
      }

      this.compression = Mathf.Clamp01(this.contactPoint.distance / (this.hoverDistance));

      if (this.grounded && this.doFloat && this.connected) {
        this.ApplyFloat();
        this.ApplyFloatDrive();
      }
    }

    //Get the contact point of the wheel
    void GetWheelContact() {
      var hit = new RaycastHit();
      var localVel = this.rb.GetPointVelocity(this.tr.position);
      var wheelHits = Physics.RaycastAll(this.tr.position,
                                         -this.upDir,
                                         this.hoverDistance,
                                         GlobalControl.wheelCastMaskStatic);
      var validHit = false;
      var hitDist = Mathf.Infinity;

      //Loop through contact points to get the closest one
      foreach (var curHit in wheelHits) {
        if (!curHit.transform.IsChildOf(this.vp.tr) && curHit.distance < hitDist) {
          hit = curHit;
          hitDist = curHit.distance;
          validHit = true;
        }
      }

      //Set contact point variables
      if (validHit) {
        if (!hit.collider.transform.IsChildOf(this.vp.tr)) {
          this.grounded = true;
          this.contactPoint.distance = hit.distance;
          this.contactPoint.point = hit.point + localVel * Time.fixedDeltaTime;
          this.contactPoint.grounded = true;
          this.contactPoint.normal = hit.normal;
          this.contactPoint.relativeVelocity = this.tr.InverseTransformDirection(localVel);
          this.contactPoint.col = hit.collider;
        }
      } else {
        this.grounded = false;
        this.contactPoint.distance = this.hoverDistance;
        this.contactPoint.point = Vector3.zero;
        this.contactPoint.grounded = false;
        this.contactPoint.normal = this.upDir;
        this.contactPoint.relativeVelocity = Vector3.zero;
        this.contactPoint.col = null;
      }
    }

    //Make the vehicle hover
    void ApplyFloat() {
      if (this.grounded) {
        //Get the vertical speed of the wheel
        var travelVel = this.vp.norm.InverseTransformDirection(this.rb.GetPointVelocity(this.tr.position)).z;

        this.rb.AddForceAtPosition(this.upDir
                                   * this.floatForce
                                   * (Mathf.Pow(this.floatForceCurve.Evaluate(1 - this.compression),
                                                Mathf.Max(1, this.floatExponent))
                                      - this.floatDampening * Mathf.Clamp(travelVel, -1, 1)),
                                   this.tr.position,
                                   this.vp.suspensionForceMode);

        if (this.contactPoint.distance < this.bufferDistance) {
          this.rb.AddForceAtPosition(-this.upDir
                                     * this.bufferFloatForce
                                     * this.floatForceCurve.Evaluate(this.contactPoint.distance
                                                                     / this.bufferDistance)
                                     * Mathf.Clamp(travelVel, -1, 0),
                                     this.tr.position,
                                     this.vp.suspensionForceMode);
        }
      }
    }

    //Drive the vehicle
    void ApplyFloatDrive() {
      //Get proper brake force
      var actualBrake =
          (this.vp.localVelocity.z > 0 ? this.vp.brakeInput : Mathf.Clamp01(this.vp.accelInput))
          * this.brakeForce
          + this.vp.ebrakeInput * this.ebrakeForce;

      this.rb.AddForceAtPosition(this.tr.TransformDirection((Mathf.Clamp(this.targetSpeed, -1, 1)
                                                             * this.targetForce
                                                             - actualBrake
                                                             * Mathf.Max(5,
                                                                         Mathf.Abs(this.contactPoint
                                                                                       .relativeVelocity.x))
                                                             * Mathf.Sign(this.contactPoint.relativeVelocity
                                                                              .x)
                                                             * this.flippedSideFactor)
                                                            * this.flippedSideFactor,
                                                            0,
                                                            -this.steerRate
                                                            * this.steerFactor
                                                            * this.flippedSideFactor
                                                            - this.contactPoint.relativeVelocity.z
                                                            * this.sideFriction)
                                 * (1 - this.compression),
                                 this.tr.position,
                                 this.vp.wheelForceMode);
    }

    //Tilt the visual wheel
    void TiltWheel() {
      var sideTilt = Mathf.Clamp(-this.steerRate * this.steerFactor * this.flippedSideFactor
                                 - Mathf.Clamp(this.contactPoint.relativeVelocity.z * 0.1f, -1, 1)
                                 * this.sideFriction,
                                 -1,
                                 1);
      var actualBrake =
          (this.vp.localVelocity.z > 0 ? this.vp.brakeInput : Mathf.Clamp01(this.vp.accelInput))
          * this.brakeForce
          + this.vp.ebrakeInput * this.ebrakeForce;
      var forwardTilt =
          Mathf.Clamp((Mathf.Clamp(this.targetSpeed, -1, 1) * this.targetForce
                       - actualBrake
                       * Mathf.Clamp(this.contactPoint.relativeVelocity.x * 0.1f, -1, 1)
                       * this.flippedSideFactor)
                      * this.flippedSideFactor,
                      -1,
                      1);

      this.visualWheel.localRotation = Quaternion.Lerp(this.visualWheel.localRotation,
                                                       Quaternion.LookRotation(new Vector3(-forwardTilt
                                                                                           * this
                                                                                               .visualTiltAmount,
                                                                                           -1
                                                                                           + Mathf
                                                                                               .Abs(F.MaxAbs(sideTilt,
                                                                                                             forwardTilt))
                                                                                           * this
                                                                                               .visualTiltAmount,
                                                                                           -sideTilt
                                                                                           * this
                                                                                               .visualTiltAmount)
                                                                                   .normalized,
                                                                               Vector3.forward),
                                                       this.visualTiltRate * Time.deltaTime);
    }

    public void Detach() {
      if (this.connected && this.canDetach) {
        this.connected = false;
        this.detachedWheel.SetActive(true);
        this.detachedWheel.transform.position = this.visualWheel.position;
        this.detachedWheel.transform.rotation = this.visualWheel.rotation;
        this.detachedCol.sharedMaterial = this.detachedWheelMaterial;
        this.detachedCol.sharedMesh =
            this.wheelMeshLoose ? this.wheelMeshLoose : this.detachFilter.sharedMesh;

        this.rb.mass -= this.mass;
        this.detachedBody.velocity = this.rb.GetPointVelocity(this.visualWheel.position);
        this.detachedBody.angularVelocity = this.rb.angularVelocity;

        this.visualWheel.gameObject.SetActive(false);
      }
    }

    public void Reattach() {
      if (!this.connected) {
        this.connected = true;
        this.detachedWheel.SetActive(false);
        this.rb.mass += this.mass;
        this.visualWheel.gameObject.SetActive(true);
      }
    }

    void OnDrawGizmosSelected() {
      this.tr = this.transform;
      //Draw a ray to show the distance of the "suspension"
      Gizmos.color = Color.white;
      Gizmos.DrawRay(this.tr.position, -this.tr.up * this.hoverDistance);
      Gizmos.color = Color.red;
      Gizmos.DrawRay(this.tr.position, -this.tr.up * this.bufferDistance);
    }

    //Destroy detached wheel
    void OnDestroy() {
      if (this.detachedWheel) {
        Destroy(this.detachedWheel);
      }
    }
  }

  //Class for the contact point
  public class HoverContact {
    public bool grounded; //Is it grounded?
    public Collider col; //Collider of the contact point
    public Vector3 point; //Position of the contact point
    public Vector3 normal; //Normal of the contact point
    public Vector3 relativeVelocity; //Velocity of the wheel relative to the contact point
    public float distance; //Distance from the wheel to the contact point
  }
}
