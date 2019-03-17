using System;
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Ground;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Static;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace AAV.CarChase.Scripts.Effects {
  [RequireComponent(typeof(Wheel))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Effects/Tire Mark Creator", 0)]

  //Class for creating tire marks
  public class TireMarkCreate : MonoBehaviour {
    Transform tr;
    Wheel w;
    Mesh mesh;
    int[] tris;
    Vector3[] verts;
    Vector2[] uvs;
    Color[] colors;

    Vector3 leftPoint;
    Vector3 rightPoint;
    Vector3 leftPointPrev;
    Vector3 rightPointPrev;

    bool creatingMark;
    bool continueMark; //Continue making mark after current one ends
    GameObject curMark; //Current mark
    Transform curMarkTr;
    int curEdge;
    float gapDelay; //Gap between segments

    int curSurface = -1; //Current surface type
    int prevSurface = -1; //Previous surface type

    bool popped;
    bool poppedPrev;

    [Tooltip("How much the tire must slip before marks are created")]
    public float slipThreshold;

    float alwaysScrape;

    public bool calculateTangents = true;

    [Tooltip("Materials in array correspond to indices in surface types in GroundSurfaceMaster")]
    public Material[] tireMarkMaterials;

    [Tooltip("Materials in array correspond to indices in surface types in GroundSurfaceMaster")]
    public Material[] rimMarkMaterials;

    [Tooltip("Particles in array correspond to indices in surface types in GroundSurfaceMaster")]
    public ParticleSystem[] debrisParticles;

    public ParticleSystem sparks;
    float[] initialEmissionRates;
    ParticleSystem.MinMaxCurve zeroEmission = new ParticleSystem.MinMaxCurve(0);

    void Start() {
      this.tr = this.transform;
      this.w = this.GetComponent<Wheel>();

      this.initialEmissionRates = new float[this.debrisParticles.Length + 1];
      for (var i = 0; i < this.debrisParticles.Length; i++) {
        this.initialEmissionRates[i] = this.debrisParticles[i].emission.rateOverTime.constantMax;
      }

      if (this.sparks) {
        this.initialEmissionRates[this.debrisParticles.Length] =
            this.sparks.emission.rateOverTime.constantMax;
      }
    }

    void Update() {
      //Check for continuous marking
      if (this.w.grounded) {
        this.alwaysScrape =
            GroundSurfaceMaster.surfaceTypesStatic[this.w.contactPoint.surfaceType].alwaysScrape
                ? this.slipThreshold + Mathf.Min(0.5f, Mathf.Abs(this.w.rawRPM * 0.001f))
                : 0;
      } else {
        this.alwaysScrape = 0;
      }

      //Create mark
      if (this.w.grounded
          && (Mathf.Abs(F.MaxAbs(this.w.sidewaysSlip, this.w.forwardSlip)) > this.slipThreshold
              || this.alwaysScrape > 0)
          && this.w.connected) {
        this.prevSurface = this.curSurface;
        this.curSurface = this.w.grounded ? this.w.contactPoint.surfaceType : -1;

        this.poppedPrev = this.popped;
        this.popped = this.w.popped;

        if (!this.creatingMark) {
          this.prevSurface = this.curSurface;
          this.StartMark();
        } else if (this.curSurface != this.prevSurface || this.popped != this.poppedPrev) {
          this.EndMark();
        }

        //Calculate segment points
        if (this.curMark) {
          var pointDir = Quaternion.AngleAxis(90, this.w.contactPoint.normal)
                         * this.tr.right
                         * (this.w.popped ? this.w.rimWidth : this.w.tireWidth);
          this.leftPoint = this.curMarkTr.InverseTransformPoint(this.w.contactPoint.point
                                                                + pointDir
                                                                * this.w.suspensionParent.flippedSideFactor
                                                                * Mathf.Sign(this.w.rawRPM)
                                                                + this.w.contactPoint.normal
                                                                * GlobalControl.tireMarkHeightStatic);
          this.rightPoint = this.curMarkTr.InverseTransformPoint(this.w.contactPoint.point
                                                                 - pointDir
                                                                 * this.w.suspensionParent.flippedSideFactor
                                                                 * Mathf.Sign(this.w.rawRPM)
                                                                 + this.w.contactPoint.normal
                                                                 * GlobalControl.tireMarkHeightStatic);
        }
      } else if (this.creatingMark) {
        this.EndMark();
      }

      //Update mark if it's short enough, otherwise end it
      if (this.curEdge < GlobalControl.tireMarkLengthStatic && this.creatingMark) {
        this.UpdateMark();
      } else if (this.creatingMark) {
        this.EndMark();
      }

      //Set particle emission rates
      ParticleSystem.EmissionModule em;
      for (var i = 0; i < this.debrisParticles.Length; i++) {
        if (this.w.connected) {
          if (i == this.w.contactPoint.surfaceType) {
            if (GroundSurfaceMaster.surfaceTypesStatic[this.w.contactPoint.surfaceType].leaveSparks
                && this.w.popped) {
              em = this.debrisParticles[i].emission;
              em.rateOverTime = this.zeroEmission;

              if (this.sparks) {
                em = this.sparks.emission;
                em.rateOverTime =
                    new ParticleSystem.MinMaxCurve(this.initialEmissionRates[this.debrisParticles.Length]
                                                   * Mathf.Clamp01(Mathf.Abs(F.MaxAbs(this.w.sidewaysSlip,
                                                                                      this.w.forwardSlip,
                                                                                      this.alwaysScrape))
                                                                   - this.slipThreshold));
              }
            } else {
              em = this.debrisParticles[i].emission;
              em.rateOverTime =
                  new ParticleSystem.MinMaxCurve(this.initialEmissionRates[i]
                                                 * Mathf.Clamp01(Mathf.Abs(F.MaxAbs(this.w.sidewaysSlip,
                                                                                    this.w.forwardSlip,
                                                                                    this.alwaysScrape))
                                                                 - this.slipThreshold));

              if (this.sparks) {
                em = this.sparks.emission;
                em.rateOverTime = this.zeroEmission;
              }
            }
          } else {
            em = this.debrisParticles[i].emission;
            em.rateOverTime = this.zeroEmission;
          }
        } else {
          em = this.debrisParticles[i].emission;
          em.rateOverTime = this.zeroEmission;

          if (this.sparks) {
            em = this.sparks.emission;
            em.rateOverTime = this.zeroEmission;
          }
        }
      }
    }

    void StartMark() {
      this.creatingMark = true;
      this.curMark = new GameObject("Tire Mark");
      this.curMarkTr = this.curMark.transform;
      this.curMarkTr.parent = this.w.contactPoint.col.transform;
      this.curMark.AddComponent<TireMark>();
      var tempRend = this.curMark.AddComponent<MeshRenderer>();

      //Set material based on whether the tire is popped
      if (this.w.popped) {
        tempRend.material =
            this.rimMarkMaterials[Mathf.Min(this.w.contactPoint.surfaceType,
                                            this.rimMarkMaterials.Length - 1)];
      } else {
        tempRend.material =
            this.tireMarkMaterials[Mathf.Min(this.w.contactPoint.surfaceType,
                                             this.tireMarkMaterials.Length - 1)];
      }

      tempRend.shadowCastingMode = ShadowCastingMode.Off;
      this.mesh = this.curMark.AddComponent<MeshFilter>().mesh;
      this.verts = new Vector3[GlobalControl.tireMarkLengthStatic * 2];
      this.tris = new int[GlobalControl.tireMarkLengthStatic * 3];

      if (this.continueMark) {
        this.verts[0] = this.leftPointPrev;
        this.verts[1] = this.rightPointPrev;

        this.tris[0] = 0;
        this.tris[1] = 3;
        this.tris[2] = 1;
        this.tris[3] = 0;
        this.tris[4] = 2;
        this.tris[5] = 3;
      }

      this.uvs = new Vector2[this.verts.Length];
      this.uvs[0] = new Vector2(0, 0);
      this.uvs[1] = new Vector2(1, 0);
      this.uvs[2] = new Vector2(0, 1);
      this.uvs[3] = new Vector2(1, 1);

      this.colors = new Color[this.verts.Length];
      this.colors[0].a = 0;
      this.colors[1].a = 0;

      this.curEdge = 2;
      this.gapDelay = GlobalControl.tireMarkGapStatic;
    }

    void UpdateMark() {
      if (this.gapDelay == 0) {
        var alpha = (this.curEdge < GlobalControl.tireMarkLengthStatic - 2 && this.curEdge > 5 ? 1 : 0)
                    * Random.Range(Mathf.Clamp01(Mathf.Abs(F.MaxAbs(this.w.sidewaysSlip,
                                                                    this.w.forwardSlip,
                                                                    this.alwaysScrape))
                                                 - this.slipThreshold)
                                   * 0.9f,
                                   Mathf.Clamp01(Mathf.Abs(F.MaxAbs(this.w.sidewaysSlip,
                                                                    this.w.forwardSlip,
                                                                    this.alwaysScrape))
                                                 - this.slipThreshold));
        this.gapDelay = GlobalControl.tireMarkGapStatic;
        this.curEdge += 2;

        this.verts[this.curEdge] = this.leftPoint;
        this.verts[this.curEdge + 1] = this.rightPoint;

        for (var i = this.curEdge + 2; i < this.verts.Length; i++) {
          this.verts[i] = Mathf.Approximately(i * 0.5f, Mathf.Round(i * 0.5f))
                              ? this.leftPoint
                              : this.rightPoint;
          this.colors[i].a = 0;
        }

        this.tris[this.curEdge * 3 - 3] = this.curEdge;
        this.tris[this.curEdge * 3 - 2] = this.curEdge + 3;
        this.tris[this.curEdge * 3 - 1] = this.curEdge + 1;
        this.tris[Mathf.Min(this.curEdge * 3, this.tris.Length - 1)] = this.curEdge;
        this.tris[Mathf.Min(this.curEdge * 3 + 1, this.tris.Length - 1)] = this.curEdge + 2;
        this.tris[Mathf.Min(this.curEdge * 3 + 2, this.tris.Length - 1)] = this.curEdge + 3;

        this.uvs[this.curEdge] = new Vector2(0, this.curEdge * 0.5f);
        this.uvs[this.curEdge + 1] = new Vector2(1, this.curEdge * 0.5f);

        this.colors[this.curEdge] = new Color(1, 1, 1, alpha);
        this.colors[this.curEdge + 1] = this.colors[this.curEdge];

        this.mesh.vertices = this.verts;
        this.mesh.triangles = this.tris;
        this.mesh.uv = this.uvs;
        this.mesh.colors = this.colors;
        this.mesh.RecalculateBounds();
        this.mesh.RecalculateNormals();
      } else {
        this.gapDelay = Mathf.Max(0, this.gapDelay - Time.deltaTime);
        this.verts[this.curEdge] = this.leftPoint;
        this.verts[this.curEdge + 1] = this.rightPoint;

        for (var i = this.curEdge + 2; i < this.verts.Length; i++) {
          this.verts[i] = Mathf.Approximately(i * 0.5f, Mathf.Round(i * 0.5f))
                              ? this.leftPoint
                              : this.rightPoint;
          this.colors[i].a = 0;
        }

        this.mesh.vertices = this.verts;
        this.mesh.RecalculateBounds();
      }

      if (this.calculateTangents) {
        this.mesh.RecalculateTangents();
      }
    }

    void EndMark() {
      this.creatingMark = false;
      this.leftPointPrev = this.verts[Mathf.RoundToInt(this.verts.Length * 0.5f)];
      this.rightPointPrev = this.verts[Mathf.RoundToInt(this.verts.Length * 0.5f + 1)];
      this.continueMark = this.w.grounded;

      this.curMark.GetComponent<TireMark>().fadeTime = GlobalControl.tireFadeTimeStatic;
      this.curMark.GetComponent<TireMark>().mesh = this.mesh;
      this.curMark.GetComponent<TireMark>().colors = this.colors;
      this.curMark = null;
      this.curMarkTr = null;
      this.mesh = null;
    }

    void OnDestroy() {
      if (this.creatingMark && this.curMark) {
        this.EndMark();
      } else if (this.mesh != null) {
        Destroy(this.mesh);
      }
    }
  }

  //Class for tire mark instances
  public class TireMark : MonoBehaviour {
    [NonSerialized] public float fadeTime = -1;
    bool fading;
    float alpha = 1;
    [NonSerialized] public Mesh mesh;
    [NonSerialized] public Color[] colors;

    //Fade the tire mark and then destroy it
    void Update() {
      if (this.fading) {
        if (this.alpha <= 0) {
          Destroy(this.mesh);
          Destroy(this.gameObject);
        } else {
          this.alpha -= Time.deltaTime;

          for (var i = 0; i < this.colors.Length; i++) {
            this.colors[i].a -= Time.deltaTime;
          }

          this.mesh.colors = this.colors;
        }
      } else {
        if (this.fadeTime > 0) {
          this.fadeTime = Mathf.Max(0, this.fadeTime - Time.deltaTime);
        } else if (this.fadeTime == 0) {
          this.fading = true;
        }
      }
    }
  }
}
