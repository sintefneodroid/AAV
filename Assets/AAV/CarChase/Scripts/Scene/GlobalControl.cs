using UnityEngine;
using UnityEngine.SceneManagement;

namespace AAV.CarChase.Scripts.Scene {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Scene Controllers/Global Control", 0)]

  //Global controller class
  public class GlobalControl : MonoBehaviour {
    [Tooltip("Reload the scene with the 'Restart' button in the input manager")]
    public bool quickRestart = true;

    float initialFixedTime;

    [Tooltip("Mask for what the wheels collide with")]
    public LayerMask wheelCastMask;

    public static LayerMask wheelCastMaskStatic;

    [Tooltip("Mask for objects which vehicles check against if they are rolled over")]
    public LayerMask groundMask;

    public static LayerMask groundMaskStatic;

    [Tooltip("Mask for objects that cause damage to vehicles")]
    public LayerMask damageMask;

    public static LayerMask damageMaskStatic;

    public static int ignoreWheelCastLayer;

    [Tooltip("Frictionless physic material")]
    public PhysicMaterial frictionlessMat;

    public static PhysicMaterial frictionlessMatStatic;

    public static Vector3 worldUpDir; //Global up direction, opposite of normalized gravity direction

    [Tooltip("Maximum segments per tire mark")]
    public int tireMarkLength;

    public static int tireMarkLengthStatic;

    [Tooltip("Gap between tire mark segments")]
    public float tireMarkGap;

    public static float tireMarkGapStatic;

    [Tooltip("Tire mark height above ground")]
    public float tireMarkHeight;

    public static float tireMarkHeightStatic;

    [Tooltip("Lifetime of tire marks")] public float tireFadeTime;
    public static float tireFadeTimeStatic;

    void Start() {
      this.initialFixedTime = Time.fixedDeltaTime;
      //Set static variables
      wheelCastMaskStatic = this.wheelCastMask;
      groundMaskStatic = this.groundMask;
      damageMaskStatic = this.damageMask;
      ignoreWheelCastLayer = LayerMask.NameToLayer("Ignore Wheel Cast");
      frictionlessMatStatic = this.frictionlessMat;
      tireMarkLengthStatic = Mathf.Max(this.tireMarkLength, 2);
      tireMarkGapStatic = this.tireMarkGap;
      tireMarkHeightStatic = this.tireMarkHeight;
      tireFadeTimeStatic = this.tireFadeTime;
    }

    void Update() {
      if (this.quickRestart) {
        if (UnityEngine.Input.GetButtonDown("Restart")) {
          SceneManager.LoadScene(SceneManager.GetActiveScene().name);
          Time.timeScale = 1;
          Time.fixedDeltaTime = this.initialFixedTime;
        }
      }
    }

    void FixedUpdate() {
      worldUpDir = Physics.gravity.sqrMagnitude == 0 ? Vector3.up : -Physics.gravity.normalized;
    }
  }
}
