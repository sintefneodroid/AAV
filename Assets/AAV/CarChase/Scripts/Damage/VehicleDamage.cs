using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Hover;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Suspension;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Damage {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Damage/Vehicle Damage", 0)]

  //Class for damaging vehicles
  public class VehicleDamage : MonoBehaviour {
    Transform tr;
    Rigidbody rb;
    VehicleParent vp;

    [Range(0, 1)] public float strength;
    public float damageFactor = 1;

    public float maxCollisionMagnitude = 100;

    [Tooltip("Maximum collision points to use when deforming, has large effect on performance")]
    public int maxCollisionPoints = 2;

    [Tooltip("Collisions underneath this local y-position will be ignored")]
    public float collisionIgnoreHeight;

    [Tooltip("If true, grounded wheels will not be damaged, but can still be displaced")]
    public bool ignoreGroundedWheels;

    [Tooltip("Minimum time in seconds between collisions")]
    public float collisionTimeGap = 0.1f;

    float hitTime;

    [Tooltip("Whether the edges of adjacent deforming parts should match")]
    public bool seamlessDeform;

    [Tooltip("Add some perlin noise to deformation")]
    public bool usePerlinNoise = true;

    [Tooltip("Recalculate normals of deformed meshes")]
    public bool calculateNormals = true;

    [Tooltip("Parts that are damaged")] public Transform[] damageParts;

    [Tooltip("Meshes that are deformed")] public MeshFilter[] deformMeshes;
    bool[] damagedMeshes;
    Mesh[] tempMeshes;
    meshVerts[] meshVertices;

    [Tooltip("Mesh colliders that are deformed (Poor performance, must be convex)")]
    public MeshCollider[] deformColliders;

    bool[] damagedCols;
    Mesh[] tempCols;
    meshVerts[] colVertices;

    [Tooltip("Parts that are displaced")] public Transform[] displaceParts;
    Vector3[] initialPartPositions;

    ContactPoint nullContact = new ContactPoint();

    void Start() {
      this.tr = this.transform;
      this.rb = this.GetComponent<Rigidbody>();
      this.vp = this.GetComponent<VehicleParent>();

      //Tell VehicleParent not to play crashing sounds because this script takes care of it
      this.vp.playCrashSounds = false;
      this.vp.playCrashSparks = false;

      //Set up mesh data
      this.tempMeshes = new Mesh[this.deformMeshes.Length];
      this.damagedMeshes = new bool[this.deformMeshes.Length];
      this.meshVertices = new meshVerts[this.deformMeshes.Length];
      for (var i = 0; i < this.deformMeshes.Length; i++) {
        this.tempMeshes[i] = this.deformMeshes[i].mesh;
        this.meshVertices[i] = new meshVerts();
        this.meshVertices[i].verts = this.deformMeshes[i].mesh.vertices;
        this.meshVertices[i].initialVerts = this.deformMeshes[i].mesh.vertices;
        this.damagedMeshes[i] = false;
      }

      //Set up mesh collider data
      this.tempCols = new Mesh[this.deformColliders.Length];
      this.damagedCols = new bool[this.deformColliders.Length];
      this.colVertices = new meshVerts[this.deformColliders.Length];
      for (var i = 0; i < this.deformColliders.Length; i++) {
        this.tempCols[i] = Instantiate(this.deformColliders[i].sharedMesh);
        this.colVertices[i] = new meshVerts();
        this.colVertices[i].verts = this.deformColliders[i].sharedMesh.vertices;
        this.colVertices[i].initialVerts = this.deformColliders[i].sharedMesh.vertices;
        this.damagedCols[i] = false;
      }

      //Set initial positions for displaced parts
      this.initialPartPositions = new Vector3[this.displaceParts.Length];
      for (var i = 0; i < this.displaceParts.Length; i++) {
        this.initialPartPositions[i] = this.displaceParts[i].localPosition;
      }
    }

    void FixedUpdate() {
      //Decrease timer for collisionTimeGap
      this.hitTime = Mathf.Max(0, this.hitTime - Time.fixedDeltaTime);
      //Make sure damageFactor is not negative
      this.damageFactor = Mathf.Max(0, this.damageFactor);
    }

    //Apply damage on collision
    void OnCollisionEnter(Collision col) {
      if (this.hitTime == 0
          && col.relativeVelocity.sqrMagnitude * this.damageFactor > 1
          && this.strength < 1) {
        var normalizedVel = col.relativeVelocity.normalized;
        var colsChecked = 0;
        var soundPlayed = false;
        var sparkPlayed = false;
        this.hitTime = this.collisionTimeGap;

        foreach (var curCol in col.contacts) {
          if (this.tr.InverseTransformPoint(curCol.point).y > this.collisionIgnoreHeight
              && GlobalControl.damageMaskStatic
              == (GlobalControl.damageMaskStatic | (1 << curCol.otherCollider.gameObject.layer))) {
            colsChecked++;

            //Play crash sound
            if (this.vp.crashSnd && this.vp.crashClips.Length > 0 && !soundPlayed) {
              this.vp.crashSnd.PlayOneShot(this.vp.crashClips[Random.Range(0, this.vp.crashClips.Length)],
                                           Mathf.Clamp01(col.relativeVelocity.magnitude * 0.1f));
              soundPlayed = true;
            }

            //Play crash sparks
            if (this.vp.sparks && !sparkPlayed) {
              this.vp.sparks.transform.position = curCol.point;
              this.vp.sparks.transform.rotation = Quaternion.LookRotation(normalizedVel, curCol.normal);
              this.vp.sparks.Play();
              sparkPlayed = true;
            }

            this.DamageApplication(curCol.point,
                                   col.relativeVelocity,
                                   this.maxCollisionMagnitude,
                                   curCol.normal,
                                   curCol,
                                   true);
          }

          //Stop checking collision points when limit reached
          if (colsChecked >= this.maxCollisionPoints) {
            break;
          }
        }

        this.FinalizeDamage();
      }
    }

    //Damage application from collision contact point
    public void ApplyDamage(ContactPoint colPoint, Vector3 colVel) {
      this.DamageApplication(colPoint.point, colVel, Mathf.Infinity, colPoint.normal, colPoint, true);
      this.FinalizeDamage();
    }

    //Same as above, but with extra float for clamping collision force
    public void ApplyDamage(ContactPoint colPoint, Vector3 colVel, float damageForceLimit) {
      this.DamageApplication(colPoint.point, colVel, damageForceLimit, colPoint.normal, colPoint, true);
      this.FinalizeDamage();
    }

    //Damage application from source other than collisions, e.g., an explosion
    public void ApplyDamage(Vector3 damagePoint, Vector3 damageForce) {
      this.DamageApplication(damagePoint,
                             damageForce,
                             Mathf.Infinity,
                             damageForce.normalized,
                             this.nullContact,
                             false);
      this.FinalizeDamage();
    }

    //Same as above, but with extra float for clamping damage force
    public void ApplyDamage(Vector3 damagePoint, Vector3 damageForce, float damageForceLimit) {
      this.DamageApplication(damagePoint,
                             damageForce,
                             damageForceLimit,
                             damageForce.normalized,
                             this.nullContact,
                             false);
      this.FinalizeDamage();
    }

    //Damage application from array of points
    public void ApplyDamage(Vector3[] damagePoints, Vector3 damageForce) {
      foreach (var curDamagePoint in damagePoints) {
        this.DamageApplication(curDamagePoint,
                               damageForce,
                               Mathf.Infinity,
                               damageForce.normalized,
                               this.nullContact,
                               false);
      }

      this.FinalizeDamage();
    }

    //Damage application from array of points, but with extra float for clamping damage force
    public void ApplyDamage(Vector3[] damagePoints, Vector3 damageForce, float damageForceLimit) {
      foreach (var curDamagePoint in damagePoints) {
        this.DamageApplication(curDamagePoint,
                               damageForce,
                               damageForceLimit,
                               damageForce.normalized,
                               this.nullContact,
                               false);
      }

      this.FinalizeDamage();
    }

    //Where the damage is actually applied
    void DamageApplication(Vector3 damagePoint,
                           Vector3 damageForce,
                           float damageForceLimit,
                           Vector3 surfaceNormal,
                           ContactPoint colPoint,
                           bool useContactPoint) {
      var colMag = Mathf.Min(damageForce.magnitude, this.maxCollisionMagnitude)
                   * (1 - this.strength)
                   * this.damageFactor; //Magnitude of collision
      var clampedColMag = Mathf.Pow(Mathf.Sqrt(colMag) * 0.5f, 1.5f); //Clamped magnitude of collision
      var clampedVel = Vector3.ClampMagnitude(damageForce, damageForceLimit); //Clamped velocity of collision
      var normalizedVel = damageForce.normalized;
      float surfaceDot; //Dot production of collision velocity and surface normal
      float massFactor = 1; //Multiplier for damage based on mass of other rigidbody
      Transform curDamagePart;
      float damagePartFactor;
      MeshFilter curDamageMesh;
      Transform curDisplacePart;
      Transform seamKeeper = null; //Transform for maintaining seams on shattered parts
      Vector3 seamLocalPoint;
      Vector3 vertProjection;
      Vector3 translation;
      Vector3 clampedTranslation;
      Vector3 localPos;
      float vertDist;
      float distClamp;
      DetachablePart detachedPart;
      Suspension.Suspension damagedSus;

      //Get mass factor for multiplying damage
      if (useContactPoint) {
        damagePoint = colPoint.point;
        surfaceNormal = colPoint.normal;

        if (colPoint.otherCollider.attachedRigidbody) {
          massFactor = Mathf.Clamp01(colPoint.otherCollider.attachedRigidbody.mass / this.rb.mass);
        }
      }

      surfaceDot = Mathf.Clamp01(Vector3.Dot(surfaceNormal, normalizedVel))
                   * (Vector3.Dot((this.tr.position - damagePoint).normalized, normalizedVel) + 1)
                   * 0.5f;

      //Damage damageable parts
      for (var i = 0; i < this.damageParts.Length; i++) {
        curDamagePart = this.damageParts[i];
        damagePartFactor = colMag
                           * surfaceDot
                           * massFactor
                           * Mathf.Min(clampedColMag * 0.01f,
                                       (clampedColMag * 0.001f)
                                       / Mathf.Pow(Vector3.Distance(curDamagePart.position, damagePoint),
                                                   clampedColMag));

        //Damage motors
        var damagedMotor = curDamagePart.GetComponent<Motor>();
        if (damagedMotor) {
          damagedMotor.health -= damagePartFactor * (1 - damagedMotor.strength);
        }

        //Damage transmissions
        var damagedTransmission = curDamagePart.GetComponent<Transmission>();
        if (damagedTransmission) {
          damagedTransmission.health -= damagePartFactor * (1 - damagedTransmission.strength);
        }
      }

      //Deform meshes
      for (var i = 0; i < this.deformMeshes.Length; i++) {
        curDamageMesh = this.deformMeshes[i];
        localPos = curDamageMesh.transform.InverseTransformPoint(damagePoint);
        translation = curDamageMesh.transform.InverseTransformDirection(clampedVel);
        clampedTranslation = Vector3.ClampMagnitude(translation, clampedColMag);

        //Shatter parts that can shatter
        var shattered = curDamageMesh.GetComponent<ShatterPart>();
        if (shattered) {
          seamKeeper = shattered.seamKeeper;
          if (Vector3.Distance(curDamageMesh.transform.position, damagePoint)
              < colMag * surfaceDot * 0.1f * massFactor
              && colMag * surfaceDot * massFactor > shattered.breakForce) {
            shattered.Shatter();
          }
        }

        //Actual deformation
        if (translation.sqrMagnitude > 0 && this.strength < 1) {
          for (var j = 0; j < this.meshVertices[i].verts.Length; j++) {
            vertDist = Vector3.Distance(this.meshVertices[i].verts[j], localPos);
            distClamp = (clampedColMag * 0.001f) / Mathf.Pow(vertDist, clampedColMag);

            if (distClamp > 0.001f) {
              this.damagedMeshes[i] = true;
              if (seamKeeper == null || this.seamlessDeform) {
                vertProjection = this.seamlessDeform
                                     ? Vector3.zero
                                     : Vector3.Project(normalizedVel, this.meshVertices[i].verts[j]);
                this.meshVertices[i].verts[j] +=
                    (clampedTranslation
                     - vertProjection
                     * (this.usePerlinNoise
                            ? 1
                              + Mathf.PerlinNoise(this.meshVertices[i].verts[j].x * 100,
                                                  this.meshVertices[i].verts[j].y * 100)
                            : 1))
                    * surfaceDot
                    * Mathf.Min(clampedColMag * 0.01f, distClamp)
                    * massFactor;
              } else {
                seamLocalPoint =
                    seamKeeper.InverseTransformPoint(curDamageMesh
                                                     .transform
                                                     .TransformPoint(this.meshVertices[i].verts[j]));
                this.meshVertices[i].verts[j] +=
                    (clampedTranslation
                     - Vector3.Project(normalizedVel, seamLocalPoint)
                     * (this.usePerlinNoise
                            ? 1 + Mathf.PerlinNoise(seamLocalPoint.x * 100, seamLocalPoint.y * 100)
                            : 1))
                    * surfaceDot
                    * Mathf.Min(clampedColMag * 0.01f, distClamp)
                    * massFactor;
              }
            }
          }
        }
      }

      seamKeeper = null;

      //Deform mesh colliders
      for (var i = 0; i < this.deformColliders.Length; i++) {
        localPos = this.deformColliders[i].transform.InverseTransformPoint(damagePoint);
        translation = this.deformColliders[i].transform.InverseTransformDirection(clampedVel);
        clampedTranslation = Vector3.ClampMagnitude(translation, clampedColMag);

        if (translation.sqrMagnitude > 0 && this.strength < 1) {
          for (var j = 0; j < this.colVertices[i].verts.Length; j++) {
            vertDist = Vector3.Distance(this.colVertices[i].verts[j], localPos);
            distClamp = (clampedColMag * 0.001f) / Mathf.Pow(vertDist, clampedColMag);

            if (distClamp > 0.001f) {
              this.damagedCols[i] = true;
              this.colVertices[i].verts[j] += clampedTranslation
                                              * surfaceDot
                                              * Mathf.Min(clampedColMag * 0.01f, distClamp)
                                              * massFactor;
            }
          }
        }
      }

      //Displace parts
      for (var i = 0; i < this.displaceParts.Length; i++) {
        curDisplacePart = this.displaceParts[i];
        translation = clampedVel;
        clampedTranslation = Vector3.ClampMagnitude(translation, clampedColMag);

        if (translation.sqrMagnitude > 0 && this.strength < 1) {
          vertDist = Vector3.Distance(curDisplacePart.position, damagePoint);
          distClamp = (clampedColMag * 0.001f) / Mathf.Pow(vertDist, clampedColMag);

          if (distClamp > 0.001f) {
            curDisplacePart.position += clampedTranslation
                                        * surfaceDot
                                        * Mathf.Min(clampedColMag * 0.01f, distClamp)
                                        * massFactor;

            //Detach detachable parts
            if (curDisplacePart.GetComponent<DetachablePart>()) {
              detachedPart = curDisplacePart.GetComponent<DetachablePart>();

              if (colMag * surfaceDot * massFactor > detachedPart.looseForce
                  && detachedPart.looseForce >= 0) {
                detachedPart.initialPos = curDisplacePart.localPosition;
                detachedPart.Detach(true);
              } else if (colMag * surfaceDot * massFactor > detachedPart.breakForce) {
                detachedPart.Detach(false);
              }
            }
            //Maybe the parent of this part is what actually detaches, useful for displacing compound colliders that represent single detachable objects
            else if (curDisplacePart.parent.GetComponent<DetachablePart>()) {
              detachedPart = curDisplacePart.parent.GetComponent<DetachablePart>();

              if (!detachedPart.detached) {
                if (colMag * surfaceDot * massFactor > detachedPart.looseForce
                    && detachedPart.looseForce >= 0) {
                  detachedPart.initialPos = curDisplacePart.parent.localPosition;
                  detachedPart.Detach(true);
                } else if (colMag * surfaceDot * massFactor > detachedPart.breakForce) {
                  detachedPart.Detach(false);
                }
              } else if (detachedPart.hinge) {
                detachedPart.displacedAnchor +=
                    curDisplacePart.parent.InverseTransformDirection(clampedTranslation
                                                                     * surfaceDot
                                                                     * Mathf.Min(clampedColMag * 0.01f,
                                                                                 distClamp)
                                                                     * massFactor);
              }
            }

            //Damage suspensions and wheels
            damagedSus = curDisplacePart.GetComponent<Suspension.Suspension>();
            if (damagedSus) {
              if ((!damagedSus.wheel.grounded && this.ignoreGroundedWheels) || !this.ignoreGroundedWheels) {
                curDisplacePart.RotateAround(damagedSus.tr.TransformPoint(damagedSus.damagePivot),
                                             Vector3.ProjectOnPlane(damagePoint - curDisplacePart.position,
                                                                    -translation.normalized),
                                             clampedColMag * surfaceDot * distClamp * 20 * massFactor);

                damagedSus.wheel.damage += clampedColMag * surfaceDot * distClamp * 10 * massFactor;

                if (clampedColMag * surfaceDot * distClamp * 10 * massFactor > damagedSus.jamForce) {
                  damagedSus.jammed = true;
                }

                if (clampedColMag * surfaceDot * distClamp * 10 * massFactor > damagedSus.wheel.detachForce) {
                  damagedSus.wheel.Detach();
                }

                foreach (var curPart in damagedSus.movingParts) {
                  if (curPart.connectObj && !curPart.isHub && !curPart.solidAxle) {
                    if (!curPart.connectObj.GetComponent<SuspensionPart>()) {
                      curPart.connectPoint +=
                          curPart.connectObj.InverseTransformDirection(clampedTranslation
                                                                       * surfaceDot
                                                                       * Mathf.Min(clampedColMag * 0.01f,
                                                                                   distClamp)
                                                                       * massFactor);
                    }
                  }
                }
              }
            }

            //Damage hover wheels
            var damagedHoverWheel = curDisplacePart.GetComponent<HoverWheel>();
            if (damagedHoverWheel) {
              if ((!damagedHoverWheel.grounded && this.ignoreGroundedWheels) || !this.ignoreGroundedWheels) {
                if (clampedColMag * surfaceDot * distClamp * 10 * massFactor
                    > damagedHoverWheel.detachForce) {
                  damagedHoverWheel.Detach();
                }
              }
            }
          }
        }
      }
    }

    //Apply damage to meshes
    void FinalizeDamage() {
      //Apply vertices to actual meshes
      for (var i = 0; i < this.deformMeshes.Length; i++) {
        if (this.damagedMeshes[i]) {
          this.tempMeshes[i].vertices = this.meshVertices[i].verts;

          if (this.calculateNormals) {
            this.tempMeshes[i].RecalculateNormals();
          }

          this.tempMeshes[i].RecalculateBounds();
        }

        this.damagedMeshes[i] = false;
      }

      //Apply vertices to actual mesh colliders
      for (var i = 0; i < this.deformColliders.Length; i++) {
        if (this.damagedCols[i]) {
          this.tempCols[i].vertices = this.colVertices[i].verts;
          this.deformColliders[i].sharedMesh = null;
          this.deformColliders[i].sharedMesh = this.tempCols[i];
        }

        this.damagedCols[i] = false;
      }
    }

    public void Repair() {
      //Fix damaged parts
      for (var i = 0; i < this.damageParts.Length; i++) {
        if (this.damageParts[i].GetComponent<Motor>()) {
          this.damageParts[i].GetComponent<Motor>().health = 1;
        }

        if (this.damageParts[i].GetComponent<Transmission>()) {
          this.damageParts[i].GetComponent<Transmission>().health = 1;
        }
      }

      //Restore deformed meshes
      for (var i = 0; i < this.deformMeshes.Length; i++) {
        for (var j = 0; j < this.meshVertices[i].verts.Length; j++) {
          this.meshVertices[i].verts[j] = this.meshVertices[i].initialVerts[j];
        }

        this.tempMeshes[i].vertices = this.meshVertices[i].verts;
        this.tempMeshes[i].RecalculateNormals();
        this.tempMeshes[i].RecalculateBounds();

        //Fix shattered parts
        var fixedShatter = this.deformMeshes[i].GetComponent<ShatterPart>();
        if (fixedShatter) {
          fixedShatter.shattered = false;

          if (fixedShatter.brokenMaterial) {
            fixedShatter.rend.sharedMaterial = fixedShatter.initialMat;
          } else {
            fixedShatter.rend.enabled = true;
          }
        }
      }

      //Restore deformed mesh colliders
      for (var i = 0; i < this.deformColliders.Length; i++) {
        for (var j = 0; j < this.colVertices[i].verts.Length; j++) {
          this.colVertices[i].verts[j] = this.colVertices[i].initialVerts[j];
        }

        this.tempCols[i].vertices = this.colVertices[i].verts;
        this.deformColliders[i].sharedMesh = null;
        this.deformColliders[i].sharedMesh = this.tempCols[i];
      }

      //Fix displaced parts
      Suspension.Suspension fixedSus;
      Transform curDisplacePart;
      for (var i = 0; i < this.displaceParts.Length; i++) {
        curDisplacePart = this.displaceParts[i];
        curDisplacePart.localPosition = this.initialPartPositions[i];

        if (curDisplacePart.GetComponent<DetachablePart>()) {
          curDisplacePart.GetComponent<DetachablePart>().Reattach();
        } else if (curDisplacePart.parent.GetComponent<DetachablePart>()) {
          curDisplacePart.parent.GetComponent<DetachablePart>().Reattach();
        }

        fixedSus = curDisplacePart.GetComponent<Suspension.Suspension>();
        if (fixedSus) {
          curDisplacePart.localRotation = fixedSus.initialRotation;
          fixedSus.jammed = false;

          foreach (var curPart in fixedSus.movingParts) {
            if (curPart.connectObj && !curPart.isHub && !curPart.solidAxle) {
              if (!curPart.connectObj.GetComponent<SuspensionPart>()) {
                curPart.connectPoint = curPart.initialConnectPoint;
              }
            }
          }
        }
      }

      //Fix wheels
      foreach (var curWheel in this.vp.wheels) {
        curWheel.Reattach();
        curWheel.FixTire();
        curWheel.damage = 0;
      }

      //Fix hover wheels
      foreach (var curHoverWheel in this.vp.hoverWheels) {
        curHoverWheel.Reattach();
      }
    }

    //Draw collisionIgnoreHeight gizmos
    void OnDrawGizmosSelected() {
      var startPoint = this.transform.TransformPoint(Vector3.up * this.collisionIgnoreHeight);
      Gizmos.color = Color.red;
      Gizmos.DrawRay(startPoint, this.transform.forward);
      Gizmos.DrawRay(startPoint, -this.transform.forward);
      Gizmos.DrawRay(startPoint, this.transform.right);
      Gizmos.DrawRay(startPoint, -this.transform.right);
    }

    //Destroy loose parts
    void OnDestroy() {
      foreach (var curPart in this.displaceParts) {
        if (curPart) {
          if (curPart.GetComponent<DetachablePart>() && curPart.parent == null) {
            Destroy(curPart.gameObject);
          } else if (curPart.parent.GetComponent<DetachablePart>() && curPart.parent.parent == null) {
            Destroy(curPart.parent.gameObject);
          }
        }
      }
    }
  }

  //Class for easier mesh data manipulation
  class meshVerts {
    public Vector3[] verts; //Current mesh vertices
    public Vector3[] initialVerts; //Original mesh vertices
  }
}
