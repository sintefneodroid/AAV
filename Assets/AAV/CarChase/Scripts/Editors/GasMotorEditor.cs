#if UNITY_EDITOR
using AAV.CarChase.Scripts.Drivetrain;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(GasMotor))]
  [CanEditMultipleObjects]
  public class GasMotorEditor : Editor {
    float topSpeed;

    public override void OnInspectorGUI() {
      var targetScript = (GasMotor)this.target;
      DriveForce nextOutput;
      Transmission nextTrans;
      GearboxTransmission nextGearbox;
      ContinuousTransmission nextConTrans;
      Suspension.Suspension nextSus;
      var reachedEnd = false;
      var endOutput = "";

      if (targetScript.outputDrives != null) {
        if (targetScript.outputDrives.Length > 0) {
          this.topSpeed = targetScript.torqueCurve.keys[targetScript.torqueCurve.length - 1].time * 1000;
          nextOutput = targetScript.outputDrives[0];

          while (!reachedEnd) {
            if (nextOutput) {
              if (nextOutput.GetComponent<Transmission>()) {
                nextTrans = nextOutput.GetComponent<Transmission>();

                if (nextTrans is GearboxTransmission) {
                  nextGearbox = (GearboxTransmission)nextTrans;
                  this.topSpeed /= nextGearbox.gears[nextGearbox.gears.Length - 1].ratio;
                } else if (nextTrans is ContinuousTransmission) {
                  nextConTrans = (ContinuousTransmission)nextTrans;
                  this.topSpeed /= nextConTrans.maxRatio;
                }

                if (nextTrans.outputDrives.Length > 0) {
                  nextOutput = nextTrans.outputDrives[0];
                } else {
                  this.topSpeed = -1;
                  reachedEnd = true;
                  endOutput = nextTrans.transform.name;
                }
              } else if (nextOutput.GetComponent<Suspension.Suspension>()) {
                nextSus = nextOutput.GetComponent<Suspension.Suspension>();

                if (nextSus.wheel) {
                  this.topSpeed /= Mathf.PI * 100;
                  this.topSpeed *= nextSus.wheel.tireRadius * 2 * Mathf.PI;
                } else {
                  this.topSpeed = -1;
                }

                reachedEnd = true;
                endOutput = nextSus.transform.name;
              } else {
                this.topSpeed = -1;
                reachedEnd = true;
                endOutput = targetScript.transform.name;
              }
            } else {
              this.topSpeed = -1;
              reachedEnd = true;
              endOutput = targetScript.transform.name;
            }
          }
        } else {
          this.topSpeed = -1;
          endOutput = targetScript.transform.name;
        }
      } else {
        this.topSpeed = -1;
        endOutput = targetScript.transform.name;
      }

      if (this.topSpeed == -1) {
        EditorGUILayout.HelpBox("Motor drive doesn't reach any wheels.  (Ends at " + endOutput + ")",
                                MessageType.Warning);
      } else if (this.targets.Length == 1) {
        EditorGUILayout.LabelField("Top Speed (Estimate): "
                                   + (this.topSpeed * 2.23694f).ToString("0.00")
                                   + " mph || "
                                   + (this.topSpeed * 3.6f).ToString("0.00")
                                   + " km/h",
                                   EditorStyles.boldLabel);
      }

      this.DrawDefaultInspector();
    }
  }
}
#endif
