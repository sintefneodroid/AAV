using System;
using AAV.CarChase.Scripts.Static;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AAV.CarChase.Scripts.Damage {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Damage/Detachable Part", 1)]

  //Class for parts that can detach
  public class DetachablePart : MonoBehaviour {
    Transform tr;
    Rigidbody rb;
    Rigidbody parentBody;
    Transform initialParent;
    Vector3 initialLocalPos;
    Quaternion initialLocalRot;

    [NonSerialized] public HingeJoint hinge;
    [NonSerialized] public bool detached;
    [NonSerialized] public Vector3 initialPos;
    public float mass = 0.1f;
    public float drag;
    public float angularDrag = 0.05f;
    public float looseForce = -1;
    public float breakForce = 25;

    [Tooltip("A hinge joint is randomly chosen from the list to use")]
    public PartJoint[] joints;

    Vector3 initialAnchor;
    [NonSerialized] public Vector3 displacedAnchor;

    void Start() {
      this.tr = this.transform;

      if (this.tr.parent) {
        this.initialParent = this.tr.parent;
        this.initialLocalPos = this.tr.localPosition;
        this.initialLocalRot = this.tr.localRotation;
      }

      this.parentBody = (Rigidbody)F.GetTopmostParentComponent<Rigidbody>(this.tr);
      this.initialPos = this.tr.localPosition;
    }

    void Update() {
      if (this.hinge) {
        //Destory hinge if displaced too far from original position
        if ((this.initialAnchor - this.displacedAnchor).sqrMagnitude > 0.1f) {
          Destroy(this.hinge);
        }
      }
    }

    public void Detach(bool makeJoint) {
      if (!this.detached) {
        this.detached = true;
        this.tr.parent = null;
        this.rb = this.gameObject.AddComponent<Rigidbody>();
        this.rb.mass = this.mass;
        this.rb.drag = this.drag;
        this.rb.angularDrag = this.angularDrag;

        if (this.parentBody) {
          this.parentBody.mass -= this.mass;
          this.rb.velocity = this.parentBody.GetPointVelocity(this.tr.position);
          this.rb.angularVelocity = this.parentBody.angularVelocity;

          //Pick a random hinge joint to use
          if (makeJoint && this.joints.Length > 0) {
            var chosenJoint = this.joints[Random.Range(0, this.joints.Length)];
            this.initialAnchor = chosenJoint.hingeAnchor;
            this.displacedAnchor = this.initialAnchor;

            this.hinge = this.gameObject.AddComponent<HingeJoint>();
            this.hinge.autoConfigureConnectedAnchor = false;
            this.hinge.connectedBody = this.parentBody;
            this.hinge.anchor = chosenJoint.hingeAnchor;
            this.hinge.axis = chosenJoint.hingeAxis;
            this.hinge.connectedAnchor = this.initialPos + chosenJoint.hingeAnchor;
            this.hinge.enableCollision = false;
            this.hinge.useLimits = chosenJoint.useLimits;

            var limits = new JointLimits();
            limits.min = chosenJoint.minLimit;
            limits.max = chosenJoint.maxLimit;
            limits.bounciness = chosenJoint.bounciness;
            this.hinge.limits = limits;
            this.hinge.useSpring = chosenJoint.useSpring;

            var spring = new JointSpring();
            spring.targetPosition = chosenJoint.springTargetPosition;
            spring.spring = chosenJoint.springForce;
            spring.damper = chosenJoint.springDamper;
            this.hinge.spring = spring;
            this.hinge.breakForce = this.breakForce;
            this.hinge.breakTorque = this.breakForce;
          }
        }
      }
    }

    public void Reattach() {
      if (this.detached) {
        this.detached = false;
        this.tr.parent = this.initialParent;
        this.tr.localPosition = this.initialLocalPos;
        this.tr.localRotation = this.initialLocalRot;

        if (this.parentBody) {
          this.parentBody.mass += this.mass;
        }

        if (this.hinge) {
          Destroy(this.hinge);
        }

        if (this.rb) {
          Destroy(this.rb);
        }
      }
    }

    //Draw joint gizmos
    void OnDrawGizmosSelected() {
      if (!this.tr) {
        this.tr = this.transform;
      }

      if (this.looseForce >= 0 && this.joints.Length > 0) {
        Gizmos.color = Color.red;
        foreach (var curJoint in this.joints) {
          Gizmos.DrawRay(this.tr.TransformPoint(curJoint.hingeAnchor),
                         this.tr.TransformDirection(curJoint.hingeAxis).normalized * 0.2f);
          Gizmos.DrawWireSphere(this.tr.TransformPoint(curJoint.hingeAnchor), 0.02f);
        }
      }
    }
  }

  //Class for storing hinge joint information in the joints list
  [Serializable]
  public class PartJoint {
    public Vector3 hingeAnchor;
    public Vector3 hingeAxis = Vector3.right;
    public bool useLimits;
    public float minLimit;
    public float maxLimit;
    public float bounciness;
    public bool useSpring;
    public float springTargetPosition;
    public float springForce;
    public float springDamper;
  }
}
