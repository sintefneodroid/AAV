using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Camera {
  [RequireComponent(typeof(UnityEngine.Camera))]
  [RequireComponent(typeof(AudioListener))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Camera/Camera Control", 0)]

  //Class for controlling the camera
  public class CameraControl : MonoBehaviour {
    Transform tr;
    UnityEngine.Camera cam;
    VehicleParent vp;
    public Transform target; //The target vehicle
    Rigidbody targetBody;

    public float height;
    public float distance;

    float xInput;
    float yInput;

    Vector3 lookDir;
    float smoothYRot;
    Transform lookObj;
    Vector3 forwardLook;
    Vector3 upLook;
    Vector3 targetForward;
    Vector3 targetUp;

    [Tooltip("Should the camera stay flat? (Local y-axis always points up)")]
    public bool stayFlat;

    [Tooltip("Mask for which objects will be checked in between the camera and target vehicle")]
    public LayerMask castMask;

    void Start() {
      this.tr = this.transform;
      this.cam = this.GetComponent<UnityEngine.Camera>();
      this.Initialize();
    }

    public void Initialize() {
      //lookObj is an object used to help position and rotate the camera
      if (!this.lookObj) {
        var lookTemp = new GameObject("Camera Looker");
        this.lookObj = lookTemp.transform;
      }

      //Set variables based on target vehicle's properties
      if (this.target) {
        this.vp = this.target.GetComponent<VehicleParent>();
        this.distance += this.vp.cameraDistanceChange;
        this.height += this.vp.cameraHeightChange;
        this.forwardLook = this.target.forward;
        this.upLook = this.target.up;
        this.targetBody = this.target.GetComponent<Rigidbody>();
      }

      //Set the audio listener update mode to fixed, because the camera moves in FixedUpdate
      //This is necessary for doppler effects to sound correct
      this.GetComponent<AudioListener>().velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
    }

    void FixedUpdate() {
      if (this.target && this.targetBody && this.target.gameObject.activeSelf) {
        if (this.vp.groundedWheels > 0) {
          this.targetForward =
              this.stayFlat ? new Vector3(this.vp.norm.up.x, 0, this.vp.norm.up.z) : this.vp.norm.up;
        }

        this.targetUp = this.stayFlat ? GlobalControl.worldUpDir : this.vp.norm.forward;
        this.lookDir = Vector3.Slerp(this.lookDir,
                                     (this.xInput == 0 && this.yInput == 0
                                          ? Vector3.forward
                                          : new Vector3(this.xInput, 0, this.yInput).normalized),
                                     0.1f * TimeMaster.inverseFixedTimeFactor);
        this.smoothYRot = Mathf.Lerp(this.smoothYRot,
                                     this.targetBody.angularVelocity.y,
                                     0.02f * TimeMaster.inverseFixedTimeFactor);

        //Determine the upwards direction of the camera
        if (Physics.Raycast(this.target.position, -this.targetUp, out var hit, 1, this.castMask)
            && !this.stayFlat) {
          this.upLook = Vector3.Lerp(this.upLook,
                                     (Vector3.Dot(hit.normal, this.targetUp) > 0.5
                                          ? hit.normal
                                          : this.targetUp),
                                     0.05f * TimeMaster.inverseFixedTimeFactor);
        } else {
          this.upLook = Vector3.Lerp(this.upLook, this.targetUp, 0.05f * TimeMaster.inverseFixedTimeFactor);
        }

        //Calculate rotation and position variables
        this.forwardLook = Vector3.Lerp(this.forwardLook,
                                        this.targetForward,
                                        0.05f * TimeMaster.inverseFixedTimeFactor);
        this.lookObj.rotation = Quaternion.LookRotation(this.forwardLook, this.upLook);
        this.lookObj.position = this.target.position;
        var lookDirActual =
            (this.lookDir
             - new Vector3(Mathf.Sin(this.smoothYRot), 0, Mathf.Cos(this.smoothYRot))
             * Mathf.Abs(this.smoothYRot)
             * 0.2f).normalized;
        var forwardDir = this.lookObj.TransformDirection(lookDirActual);
        var localOffset = this.lookObj.TransformPoint(-lookDirActual * this.distance
                                                      - lookDirActual
                                                      * Mathf.Min(this.targetBody.velocity.magnitude * 0.05f,
                                                                  2)
                                                      + new Vector3(0, this.height, 0));

        //Check if there is an object between the camera and target vehicle and move the camera in front of it
        if (Physics.Linecast(this.target.position, localOffset, out hit, this.castMask)) {
          this.tr.position = hit.point
                             + (this.target.position - localOffset).normalized
                             * (this.cam.nearClipPlane + 0.1f);
        } else {
          this.tr.position = localOffset;
        }

        this.tr.rotation = Quaternion.LookRotation(forwardDir, this.lookObj.up);
      }
    }

    //function for setting the rotation input of the camera
    public void SetInput(float x, float y) {
      this.xInput = x;
      this.yInput = y;
    }

    //Destroy lookObj
    void OnDestroy() {
      if (this.lookObj) {
        Destroy(this.lookObj.gameObject);
      }
    }
  }
}
