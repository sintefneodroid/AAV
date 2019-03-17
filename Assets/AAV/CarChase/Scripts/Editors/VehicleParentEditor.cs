#if UNITY_EDITOR
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Hover;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(VehicleParent))]
  [CanEditMultipleObjects]
  public class VehicleParentEditor : Editor {
    bool isPrefab;
    static bool showButtons = true;
    bool wheelMissing;

    public override void OnInspectorGUI() {
      var boldFoldout = new GUIStyle(EditorStyles.foldout);
      boldFoldout.fontStyle = FontStyle.Bold;
      var targetScript = (VehicleParent)this.target;
      var allTargets = new VehicleParent[this.targets.Length];
      this.isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

      for (var i = 0; i < this.targets.Length; i++) {
        Undo.RecordObject(this.targets[i], "Vehicle Parent Change");
        allTargets[i] = this.targets[i] as VehicleParent;
      }

      this.wheelMissing = false;
      if (targetScript.wheelGroups != null) {
        if (targetScript.wheelGroups.Length > 0) {
          if (targetScript.hover) {
            foreach (var curWheel in targetScript.hoverWheels) {
              var wheelfound = false;
              foreach (var curGroup in targetScript.wheelGroups) {
                foreach (var curWheelInstance in curGroup.hoverWheels) {
                  if (curWheel == curWheelInstance) {
                    wheelfound = true;
                  }
                }
              }

              if (!wheelfound) {
                this.wheelMissing = true;
                break;
              }
            }
          } else {
            foreach (var curWheel in targetScript.wheels) {
              var wheelfound = false;
              foreach (var curGroup in targetScript.wheelGroups) {
                foreach (var curWheelInstance in curGroup.wheels) {
                  if (curWheel == curWheelInstance) {
                    wheelfound = true;
                  }
                }
              }

              if (!wheelfound) {
                this.wheelMissing = true;
                break;
              }
            }
          }
        }
      }

      if (this.wheelMissing) {
        EditorGUILayout.HelpBox("If there is at least one wheel group, all wheels must be part of a group.",
                                MessageType.Error);
      }

      this.DrawDefaultInspector();

      if (!this.isPrefab && targetScript.gameObject.activeInHierarchy) {
        showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
        EditorGUI.indentLevel++;
        if (showButtons) {
          if (GUILayout.Button("Get Engine")) {
            foreach (var curTarget in allTargets) {
              curTarget.engine = curTarget.transform.GetComponentInChildren<Motor>();
            }
          }

          if (GUILayout.Button("Get Wheels")) {
            foreach (var curTarget in allTargets) {
              if (curTarget.hover) {
                curTarget.hoverWheels = curTarget.transform.GetComponentsInChildren<HoverWheel>();
              } else {
                curTarget.wheels = curTarget.transform.GetComponentsInChildren<Wheel>();
              }
            }
          }
        }

        EditorGUI.indentLevel--;
      }

      if (GUI.changed) {
        EditorUtility.SetDirty(targetScript);
      }
    }
  }
}
#endif
