using System;
using System.Collections.Generic;
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Stunt {
  [RequireComponent(typeof(VehicleParent))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Stunt/Stunt Detector", 1)]

  //Class for detecting stunts
  public class StuntDetect : MonoBehaviour {
    Transform tr;
    Rigidbody rb;
    VehicleParent vp;

    [NonSerialized] public float score;
    List<Stunt> stunts = new List<Stunt>();
    List<Stunt> doneStunts = new List<Stunt>();
    bool drifting;
    float driftDist;
    float driftScore;
    float endDriftTime; //Time during which drifting counts even if the vehicle is not actually drifting
    float jumpDist;
    float jumpTime;
    Vector3 jumpStart;

    public bool detectDrift = true;
    public bool detectJump = true;
    public bool detectFlips = true;

    string driftString; //String indicating drift distance
    string jumpString; //String indicating jump distance
    string flipString; //String indicating flips
    [NonSerialized] public string stuntString; //String containing all stunts

    public Motor engine;

    void Start() {
      this.tr = this.transform;
      this.rb = this.GetComponent<Rigidbody>();
      this.vp = this.GetComponent<VehicleParent>();
    }

    void FixedUpdate() {
      //Detect drifts
      if (this.detectDrift && !this.vp.crashing) {
        this.DetectDrift();
      } else {
        this.drifting = false;
        this.driftDist = 0;
        this.driftScore = 0;
        this.driftString = "";
      }

      //Detect jumps
      if (this.detectJump && !this.vp.crashing) {
        this.DetectJump();
      } else {
        this.jumpTime = 0;
        this.jumpDist = 0;
        this.jumpString = "";
      }

      //Detect flips
      if (this.detectFlips && !this.vp.crashing) {
        this.DetectFlips();
      } else {
        this.stunts.Clear();
        this.flipString = "";
      }

      //Combine strings into final stunt string
      this.stuntString = this.vp.crashing
                             ? "Crashed"
                             : this.driftString
                               + this.jumpString
                               + (string.IsNullOrEmpty(this.flipString)
                                  || string.IsNullOrEmpty(this.jumpString)
                                      ? ""
                                      : " + ")
                               + this.flipString;
    }

    void DetectDrift() {
      this.endDriftTime = this.vp.groundedWheels > 0
                              ? (Mathf.Abs(this.vp.localVelocity.x) > 5
                                     ? StuntManager.driftConnectDelayStatic
                                     : Mathf.Max(0,
                                                 this.endDriftTime
                                                 - Time.timeScale * TimeMaster.inverseFixedTimeFactor))
                              : 0;
      this.drifting = this.endDriftTime > 0;

      if (this.drifting) {
        this.driftScore += (StuntManager.driftScoreRateStatic * Mathf.Abs(this.vp.localVelocity.x))
                           * Time.timeScale
                           * TimeMaster.inverseFixedTimeFactor;
        this.driftDist += this.vp.velMag * Time.fixedDeltaTime;
        this.driftString = "Drift: " + this.driftDist.ToString("n0") + " m";

        if (this.engine) {
          this.engine.boost += (StuntManager.driftBoostAddStatic * Mathf.Abs(this.vp.localVelocity.x))
                               * Time.timeScale
                               * 0.0002f
                               * TimeMaster.inverseFixedTimeFactor;
        }
      } else {
        this.score += this.driftScore;
        this.driftDist = 0;
        this.driftScore = 0;
        this.driftString = "";
      }
    }

    void DetectJump() {
      if (this.vp.groundedWheels == 0) {
        this.jumpDist = Vector3.Distance(this.jumpStart, this.tr.position);
        this.jumpTime += Time.fixedDeltaTime;
        this.jumpString = "Jump: " + this.jumpDist.ToString("n0") + " m";

        if (this.engine) {
          this.engine.boost += StuntManager.jumpBoostAddStatic
                               * Time.timeScale
                               * 0.01f
                               * TimeMaster.inverseFixedTimeFactor;
        }
      } else {
        this.score += (this.jumpDist + this.jumpTime) * StuntManager.jumpScoreRateStatic;

        if (this.engine) {
          this.engine.boost += (this.jumpDist + this.jumpTime)
                               * StuntManager.jumpBoostAddStatic
                               * Time.timeScale
                               * 0.01f
                               * TimeMaster.inverseFixedTimeFactor;
        }

        this.jumpStart = this.tr.position;
        this.jumpDist = 0;
        this.jumpTime = 0;
        this.jumpString = "";
      }
    }

    void DetectFlips() {
      if (this.vp.groundedWheels == 0) {
        //Check to see if vehicle is performing a stunt and add it to the stunts list
        foreach (var curStunt in StuntManager.stuntsStatic) {
          if (Vector3.Dot(this.vp.localAngularVel.normalized, curStunt.rotationAxis) >= curStunt.precision) {
            var stuntExists = false;

            foreach (var checkStunt in this.stunts) {
              if (curStunt.name == checkStunt.name) {
                stuntExists = true;
                break;
              }
            }

            if (!stuntExists) {
              this.stunts.Add(new Stunt(curStunt));
            }
          }
        }

        //Check the progress of stunts and compile the flip string listing all stunts
        foreach (var curStunt2 in this.stunts) {
          if (Vector3.Dot(this.vp.localAngularVel.normalized, curStunt2.rotationAxis)
              >= curStunt2.precision) {
            curStunt2.progress += this.rb.angularVelocity.magnitude * Time.fixedDeltaTime;
          }

          if (curStunt2.progress * Mathf.Rad2Deg >= curStunt2.angleThreshold) {
            var stuntDoneExists = false;

            foreach (var curDoneStunt in this.doneStunts) {
              if (curDoneStunt == curStunt2) {
                stuntDoneExists = true;
                break;
              }
            }

            if (!stuntDoneExists) {
              this.doneStunts.Add(curStunt2);
            }
          }
        }

        var stuntCount = "";
        this.flipString = "";

        foreach (var curDoneStunt2 in this.doneStunts) {
          stuntCount = curDoneStunt2.progress * Mathf.Rad2Deg >= curDoneStunt2.angleThreshold * 2
                           ? " x"
                             + Mathf.FloorToInt((curDoneStunt2.progress * Mathf.Rad2Deg)
                                                / curDoneStunt2.angleThreshold)
                           : "";
          this.flipString = string.IsNullOrEmpty(this.flipString)
                                ? curDoneStunt2.name + stuntCount
                                : this.flipString + " + " + curDoneStunt2.name + stuntCount;
        }
      } else {
        //Add stunt points to the score
        foreach (var curStunt in this.stunts) {
          this.score += curStunt.progress
                        * Mathf.Rad2Deg
                        * curStunt.scoreRate
                        * Mathf.FloorToInt((curStunt.progress * Mathf.Rad2Deg) / curStunt.angleThreshold)
                        * curStunt.multiplier;

          //Add boost to the engine
          if (this.engine) {
            this.engine.boost += curStunt.progress
                                 * Mathf.Rad2Deg
                                 * curStunt.boostAdd
                                 * curStunt.multiplier
                                 * 0.01f;
          }
        }

        this.stunts.Clear();
        this.doneStunts.Clear();
        this.flipString = "";
      }
    }
  }
}
