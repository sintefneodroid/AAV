using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Ground {
  [RequireComponent(typeof(Collider))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Ground Surface/Ground Surface Instance", 1)]

  //Class for instances of surface types
  public class GroundSurfaceInstance : MonoBehaviour {
    [Tooltip("Which surface type to use from the GroundSurfaceMaster list of surface types")]
    public int surfaceType;

    [NonSerialized] public float friction;

    void Start() {
      //Set friction
      if (GroundSurfaceMaster.surfaceTypesStatic[this.surfaceType].useColliderFriction) {
        this.friction = this.GetComponent<Collider>().material.dynamicFriction * 2;
      } else {
        this.friction = GroundSurfaceMaster.surfaceTypesStatic[this.surfaceType].friction;
      }
    }
  }
}
