using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Ground {
  [RequireComponent(typeof(Terrain))]
  [ExecuteInEditMode]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Ground Surface/Terrain Surface", 2)]

  //Class for associating terrain textures with ground surface types
  public class TerrainSurface : MonoBehaviour {
    Transform tr;
    TerrainData terDat;
    float[,,] terrainAlphamap;
    public int[] surfaceTypes = new int[0];
    [NonSerialized] public float[] frictions;

    void Start() {
      this.tr = this.transform;
      if (this.GetComponent<Terrain>().terrainData) {
        this.terDat = this.GetComponent<Terrain>().terrainData;

        //Set frictions for each surface type
        if (Application.isPlaying) {
          this.UpdateAlphamaps();
          this.frictions = new float[this.surfaceTypes.Length];

          for (var i = 0; i < this.frictions.Length; i++) {
            if (GroundSurfaceMaster.surfaceTypesStatic[this.surfaceTypes[i]].useColliderFriction) {
              this.frictions[i] = this.GetComponent<Collider>().material.dynamicFriction * 2;
            } else {
              this.frictions[i] = GroundSurfaceMaster.surfaceTypesStatic[this.surfaceTypes[i]].friction;
            }
          }
        }
      }
    }

    void Update() {
      if (!Application.isPlaying) {
        if (this.terDat) {
          if (this.surfaceTypes.Length != this.terDat.alphamapLayers) {
            this.ChangeSurfaceTypesLength();
          }
        }
      }
    }

    public void UpdateAlphamaps() {
      this.terrainAlphamap =
          this.terDat.GetAlphamaps(0, 0, this.terDat.alphamapWidth, this.terDat.alphamapHeight);
    }

    void ChangeSurfaceTypesLength() {
      var tempVals = this.surfaceTypes;

      this.surfaceTypes = new int[this.terDat.alphamapLayers];

      for (var i = 0; i < this.surfaceTypes.Length; i++) {
        if (i >= tempVals.Length) {
          break;
        }

        this.surfaceTypes[i] = tempVals[i];
      }
    }

    //Returns index of dominant surface type at point on terrain, relative to surface types array in GroundSurfaceMaster
    public int GetDominantSurfaceTypeAtPoint(Vector3 pos) {
      var coord = new Vector2(Mathf.Clamp01((pos.z - this.tr.position.z) / this.terDat.size.z),
                              Mathf.Clamp01((pos.x - this.tr.position.x) / this.terDat.size.x));

      float maxVal = 0;
      var maxIndex = 0;
      float curVal = 0;

      for (var i = 0; i < this.terrainAlphamap.GetLength(2); i++) {
        curVal = this.terrainAlphamap[Mathf.FloorToInt(coord.x * (this.terDat.alphamapWidth - 1)),
                                      Mathf.FloorToInt(coord.y * (this.terDat.alphamapHeight - 1)),
                                      i];

        if (curVal > maxVal) {
          maxVal = curVal;
          maxIndex = i;
        }
      }

      return this.surfaceTypes[maxIndex];
    }

    //Gets the friction of the indicated surface type
    public float GetFriction(int sType) {
      float returnedFriction = 1;

      for (var i = 0; i < this.surfaceTypes.Length; i++) {
        if (sType == this.surfaceTypes[i]) {
          returnedFriction = this.frictions[i];
          break;
        }
      }

      return returnedFriction;
    }
  }
}
