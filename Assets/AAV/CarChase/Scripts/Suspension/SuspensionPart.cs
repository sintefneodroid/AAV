using System;
using AAV.CarChase.Scripts.Drivetrain;
using UnityEngine;

namespace AAV.CarChase.Scripts.Suspension {
  [ExecuteInEditMode]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Suspension/Suspension Part", 1)]

  //Class for moving suspension parts
  public class SuspensionPart : MonoBehaviour {
    Transform tr;
    Wheel wheel;
    public Suspension suspension;
    public bool isHub;

    [Header("Connections")]
    [Tooltip("Object to point at")]
    public Transform connectObj;

    [Tooltip("Local space point to point at in connectObj")]
    public Vector3 connectPoint;

    [NonSerialized] public Vector3 initialConnectPoint;
    Vector3 localConnectPoint; //Transformed connect point

    [Tooltip("Rotate to point at target?")]
    public bool rotate = true;

    [Tooltip("Scale along local z-axis to reach target?")]
    public bool stretch;

    float initialDist;
    Vector3 initialScale;

    [Header("Solid Axle")] public bool solidAxle;
    public bool invertRotation;

    [Tooltip("Does this part connect to a solid axle?")]
    public bool solidAxleConnector;

    //Wheels for solid axles
    public Wheel wheel1;
    public Wheel wheel2;
    Vector3 wheelConnect1;
    Vector3 wheelConnect2;

    Vector3 parentUpDir; //parent's up direction

    void Start() {
      this.tr = this.transform;
      this.initialConnectPoint = this.connectPoint;

      //Get the wheel
      if (this.suspension) {
        this.suspension.movingParts.Add(this);

        if (this.suspension.wheel) {
          this.wheel = this.suspension.wheel;
        }
      }

      //Get the initial distance from the target to use when stretching
      if (this.connectObj && !this.isHub && Application.isPlaying) {
        this.initialDist =
            Mathf.Max(Vector3.Distance(this.tr.position, this.connectObj.TransformPoint(this.connectPoint)),
                      0.01f);
        this.initialScale = this.tr.localScale;
      }
    }

    void Update() {
      if (!Application.isPlaying) {
        this.tr = this.transform;

        //Get the wheel
        if (this.suspension) {
          if (this.suspension.wheel) {
            this.wheel = this.suspension.wheel;
          }
        }
      }

      if (this.tr) {
        if (!this.solidAxle && ((this.suspension && !this.solidAxleConnector) || this.solidAxleConnector)) {
          //Transformations for hubs
          if (this.isHub && this.wheel && !this.solidAxleConnector) {
            if (this.wheel.rim) {
              this.tr.position = this.wheel.rim.position;
              this.tr.rotation = Quaternion.LookRotation(this.wheel.rim.forward, this.suspension.upDir);
              this.tr.localEulerAngles = new Vector3(this.tr.localEulerAngles.x,
                                                     this.tr.localEulerAngles.y,
                                                     -this.suspension.casterAngle
                                                     * this.suspension.flippedSideFactor);
            }
          } else if (!this.isHub && this.connectObj) {
            this.localConnectPoint = this.connectObj.TransformPoint(this.connectPoint);

            //Rotate to look at connection point
            if (this.rotate) {
              this.tr.rotation =
                  Quaternion.LookRotation((this.localConnectPoint - this.tr.position).normalized,
                                          (this.solidAxleConnector
                                               ? this.tr.parent.forward
                                               : this.suspension.upDir));

              //Don't set localEulerAngles if connected to a solid axle
              if (!this.solidAxleConnector) {
                this.tr.localEulerAngles = new Vector3(this.tr.localEulerAngles.x,
                                                       this.tr.localEulerAngles.y,
                                                       -this.suspension.casterAngle
                                                       * this.suspension.flippedSideFactor);
              }
            }

            //Stretch like a spring if stretch is true
            if (this.stretch && Application.isPlaying) {
              this.tr.localScale = new Vector3(this.tr.localScale.x,
                                               this.tr.localScale.y,
                                               this.initialScale.z
                                               * (Vector3.Distance(this.tr.position, this.localConnectPoint)
                                                  / this.initialDist));
            }
          }
        } else if (this.solidAxle && this.wheel1 && this.wheel2) {
          //Transformations for solid axles
          if (this.wheel1.rim
              && this.wheel2.rim
              && this.wheel1.suspensionParent
              && this.wheel2.suspensionParent) {
            this.parentUpDir = this.tr.parent.up;
            this.wheelConnect1 =
                this.wheel1.rim.TransformPoint(0, 0, -this.wheel1.suspensionParent.pivotOffset);
            this.wheelConnect2 =
                this.wheel2.rim.TransformPoint(0, 0, -this.wheel2.suspensionParent.pivotOffset);
            this.tr.rotation =
                Quaternion.LookRotation((((this.wheelConnect1 + this.wheelConnect2) * 0.5f)
                                         - this.tr.position).normalized,
                                        this.parentUpDir);
            this.tr.localEulerAngles = new Vector3(this.tr.localEulerAngles.x,
                                                   this.tr.localEulerAngles.y,
                                                   Vector3.Angle((this.wheelConnect1 - this.wheelConnect2)
                                                                 .normalized,
                                                                 this.tr.parent.right)
                                                   * Mathf.Sign(Vector3.Dot((this.wheelConnect1
                                                                             - this.wheelConnect2).normalized,
                                                                            this.parentUpDir))
                                                   * Mathf.Sign(this.tr.localPosition.z)
                                                   * (this.invertRotation ? -1 : 1));
          }
        }
      }
    }

    void OnDrawGizmosSelected() {
      if (!this.tr) {
        this.tr = this.transform;
      }

      Gizmos.color = Color.green;

      //Visualize connections
      if (!this.isHub && this.connectObj && !this.solidAxle) {
        this.localConnectPoint = this.connectObj.TransformPoint(this.connectPoint);
        Gizmos.DrawLine(this.tr.position, this.localConnectPoint);
        Gizmos.DrawWireSphere(this.localConnectPoint, 0.01f);
      } else if (this.solidAxle && this.wheel1 && this.wheel2) {
        if (this.wheel1.rim
            && this.wheel2.rim
            && this.wheel1.suspensionParent
            && this.wheel2.suspensionParent) {
          this.wheelConnect1 =
              this.wheel1.rim.TransformPoint(0, 0, -this.wheel1.suspensionParent.pivotOffset);
          this.wheelConnect2 =
              this.wheel2.rim.TransformPoint(0, 0, -this.wheel2.suspensionParent.pivotOffset);
          Gizmos.DrawLine(this.wheelConnect1, this.wheelConnect2);
          Gizmos.DrawWireSphere(this.wheelConnect1, 0.01f);
          Gizmos.DrawWireSphere(this.wheelConnect2, 0.01f);
        }
      }
    }
  }
}
