using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Stunt {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Stunt/Stunt Manager", 0)]

  //Class for managing stunts
  public class StuntManager : MonoBehaviour {
    public float driftScoreRate;
    public static float driftScoreRateStatic;

    [Tooltip("Maximum time gap between connected drifts")]
    public float driftConnectDelay;

    public static float driftConnectDelayStatic;

    public float driftBoostAdd;
    public static float driftBoostAddStatic;

    public float jumpScoreRate;
    public static float jumpScoreRateStatic;

    public float jumpBoostAdd;
    public static float jumpBoostAddStatic;

    public Stunt[] stunts;
    public static Stunt[] stuntsStatic;

    void Start() {
      //Set static variables
      driftScoreRateStatic = this.driftScoreRate;
      driftConnectDelayStatic = this.driftConnectDelay;
      driftBoostAddStatic = this.driftBoostAdd;
      jumpScoreRateStatic = this.jumpScoreRate;
      jumpBoostAddStatic = this.jumpBoostAdd;
      stuntsStatic = this.stunts;
    }
  }

  //Stunt class
  [Serializable]
  public class Stunt {
    public string name;
    public Vector3 rotationAxis; //Local rotation axis of the stunt

    [Range(0, 1)]
    public float precision = 0.8f; //Limit for the dot product between the rotation axis and the stunt axis

    public float scoreRate;
    public float multiplier = 1; //Multiplier for when the stunt is performed more than once in the same jump
    public float angleThreshold;
    [NonSerialized] public float progress; //How much rotation has happened during the stunt in radians?
    public float boostAdd;

    //Use this to duplicate a stunt
    public Stunt(Stunt oldStunt) {
      this.name = oldStunt.name;
      this.rotationAxis = oldStunt.rotationAxis;
      this.precision = oldStunt.precision;
      this.scoreRate = oldStunt.scoreRate;
      this.angleThreshold = oldStunt.angleThreshold;
      this.multiplier = oldStunt.multiplier;
      this.boostAdd = oldStunt.boostAdd;
    }
  }
}
