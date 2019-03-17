using System.Collections;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.AI {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/AI/Follow AI", 0)]

  //Class for following AI
  public class FollowAI : MonoBehaviour {
    Transform tr;
    Rigidbody rb;
    VehicleParent vp;
    VehicleAssist va;
    public Transform target;
    Transform targetPrev;
    Rigidbody targetBody;
    Vector3 targetPoint;
    bool targetVisible;
    bool targetIsWaypoint;
    VehicleWaypoint targetWaypoint;

    public float followDistance;
    bool close;

    [Tooltip("Percentage of maximum speed to drive at")]
    [Range(0, 1)]
    public float speed = 1;

    float initialSpeed;
    float prevSpeed;
    public float targetVelocity = -1;
    float speedLimit = 1;
    float brakeTime;

    [Tooltip("Mask for which objects can block the view of the target")]
    public LayerMask viewBlockMask;

    Vector3 dirToTarget; //Normalized direction to target
    float lookDot; //Dot product of forward direction and dirToTarget
    float steerDot; //Dot product of right direction and dirToTarget

    float stoppedTime;
    float reverseTime;

    [Tooltip("Time limit in seconds which the vehicle is stuck before attempting to reverse")]
    public float stopTimeReverse = 1;

    [Tooltip("Duration in seconds the vehicle will reverse after getting stuck")]
    public float reverseAttemptTime = 1;

    [Tooltip("How many times the vehicle will attempt reversing before resetting, -1 = no reset")]
    public int resetReverseCount = 1;

    int reverseAttempts;

    [Tooltip("Seconds a vehicle will be rolled over before resetting, -1 = no reset")]
    public float rollResetTime = 3;

    float rolledOverTime;

    void Start() {
      this.tr = this.transform;
      this.rb = this.GetComponent<Rigidbody>();
      this.vp = this.GetComponent<VehicleParent>();
      this.va = this.GetComponent<VehicleAssist>();
      this.initialSpeed = this.speed;

      this.InitializeTarget();
    }

    void FixedUpdate() {
      if (this.target) {
        if (this.target != this.targetPrev) {
          this.InitializeTarget();
        }

        this.targetPrev = this.target;

        //Is the target a waypoint?
        this.targetIsWaypoint = this.target.GetComponent<VehicleWaypoint>();
        //Can I see the target?
        this.targetVisible = !Physics.Linecast(this.tr.position, this.target.position, this.viewBlockMask);

        if (this.targetVisible || this.targetIsWaypoint) {
          this.targetPoint = this.targetBody
                                 ? this.target.position + this.targetBody.velocity
                                 : this.target.position;
        }

        if (this.targetIsWaypoint) {
          //if vehicle is close enough to target waypoint, switch to the next one
          if ((this.tr.position - this.target.position).sqrMagnitude
              <= this.targetWaypoint.radius * this.targetWaypoint.radius) {
            this.target = this.targetWaypoint.nextPoint.transform;
            this.targetWaypoint = this.targetWaypoint.nextPoint;
            this.prevSpeed = this.speed;
            this.speed = Mathf.Clamp01(this.targetWaypoint.speed * this.initialSpeed);
            this.brakeTime = this.prevSpeed / this.speed;

            if (this.brakeTime <= 1) {
              this.brakeTime = 0;
            }
          }
        }

        this.brakeTime = Mathf.Max(0, this.brakeTime - Time.fixedDeltaTime);
        //Is the distance to the target less than the follow distance?
        this.close =
            (this.tr.position - this.target.position).sqrMagnitude <= Mathf.Pow(this.followDistance, 2)
            && !this.targetIsWaypoint;
        this.dirToTarget = (this.targetPoint - this.tr.position).normalized;
        this.lookDot = Vector3.Dot(this.vp.forwardDir, this.dirToTarget);
        this.steerDot = Vector3.Dot(this.vp.rightDir, this.dirToTarget);

        //Attempt to reverse if vehicle is stuck
        this.stoppedTime = Mathf.Abs(this.vp.localVelocity.z) < 1 && !this.close && this.vp.groundedWheels > 0
                               ? this.stoppedTime + Time.fixedDeltaTime
                               : 0;

        if (this.stoppedTime > this.stopTimeReverse && this.reverseTime == 0) {
          this.reverseTime = this.reverseAttemptTime;
          this.reverseAttempts++;
        }

        //Reset if reversed too many times
        if (this.reverseAttempts > this.resetReverseCount && this.resetReverseCount >= 0) {
          this.StartCoroutine(this.ReverseReset());
        }

        this.reverseTime = Mathf.Max(0, this.reverseTime - Time.fixedDeltaTime);

        if (this.targetVelocity > 0) {
          this.speedLimit = Mathf.Clamp01(this.targetVelocity - this.vp.localVelocity.z);
        } else {
          this.speedLimit = 1;
        }

        //Set vehicle inputs
        this.vp.SetAccel(!this.close
                         && (this.lookDot > 0 || this.vp.localVelocity.z < 5)
                         && this.vp.groundedWheels > 0
                         && this.reverseTime == 0
                             ? this.speed * this.speedLimit
                             : 0);
        this.vp.SetBrake(this.reverseTime == 0
                         && this.brakeTime == 0
                         && !(this.close && this.vp.localVelocity.z > 0.1f)
                             ? (this.lookDot < 0.5f && this.lookDot > 0 && this.vp.localVelocity.z > 10
                                    ? 0.5f - this.lookDot
                                    : 0)
                             : (this.reverseTime > 0
                                    ? 1
                                    : (this.brakeTime > 0
                                           ? this.brakeTime * 0.2f
                                           : 1
                                             - Mathf.Clamp01(Vector3.Distance(this.tr.position,
                                                                              this.target.position)
                                                             / Mathf.Max(0.01f, this.followDistance)))));
        this.vp.SetSteer(this.reverseTime == 0
                             ? Mathf.Abs(Mathf.Pow(this.steerDot,
                                                   (this.tr.position - this.target.position).sqrMagnitude > 20
                                                       ? 1
                                                       : 2))
                               * Mathf.Sign(this.steerDot)
                             : -Mathf.Sign(this.steerDot) * (this.close ? 0 : 1));
        this.vp.SetEbrake((this.close && this.vp.localVelocity.z <= 0.1f)
                          || (this.lookDot <= 0 && this.vp.velMag > 20)
                              ? 1
                              : 0);
      }

      this.rolledOverTime = this.va.rolledOver ? this.rolledOverTime + Time.fixedDeltaTime : 0;

      //Reset if stuck rolled over
      if (this.rolledOverTime > this.rollResetTime && this.rollResetTime >= 0) {
        this.StartCoroutine(this.ResetRotation());
      }
    }

    IEnumerator ReverseReset() {
      this.reverseAttempts = 0;
      this.reverseTime = 0;
      yield return new WaitForFixedUpdate();
      this.tr.position = this.targetPoint;
      this.tr.rotation =
          Quaternion.LookRotation(this.targetIsWaypoint
                                      ? (this.targetWaypoint.nextPoint.transform.position - this.targetPoint)
                                      .normalized
                                      : Vector3.forward,
                                  GlobalControl.worldUpDir);
      this.rb.velocity = Vector3.zero;
      this.rb.angularVelocity = Vector3.zero;
    }

    IEnumerator ResetRotation() {
      yield return new WaitForFixedUpdate();
      this.tr.eulerAngles = new Vector3(0, this.transform.eulerAngles.y, 0);
      this.tr.Translate(Vector3.up, Space.World);
      this.rb.velocity = Vector3.zero;
      this.rb.angularVelocity = Vector3.zero;
    }

    public void InitializeTarget() {
      if (this.target) {
        //if target is a vehicle
        this.targetBody = (Rigidbody)F.GetTopmostParentComponent<Rigidbody>(this.target);

        //if target is a waypoint
        this.targetWaypoint = this.target.GetComponent<VehicleWaypoint>();
        if (this.targetWaypoint) {
          this.prevSpeed = this.targetWaypoint.speed;
        }
      }
    }
  }
}
