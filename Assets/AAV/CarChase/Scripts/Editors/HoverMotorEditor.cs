#if UNITY_EDITOR
using AAV.CarChase.Scripts.Hover;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(HoverMotor))]
  [CanEditMultipleObjects]
  public class HoverMotorEditor : Editor {
    bool isPrefab;
    static bool showButtons = true;
    float topSpeed;

    public override void OnInspectorGUI() {
      var boldFoldout = new GUIStyle(EditorStyles.foldout);
      boldFoldout.fontStyle = FontStyle.Bold;
      var targetScript = (HoverMotor)this.target;
      var allTargets = new HoverMotor[this.targets.Length];
      this.isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

      for (var i = 0; i < this.targets.Length; i++) {
        Undo.RecordObject(this.targets[i], "Hover Motor Change");
        allTargets[i] = this.targets[i] as HoverMotor;
      }

      this.topSpeed = targetScript.forceCurve.keys[targetScript.forceCurve.keys.Length - 1].time;

      if (targetScript.wheels != null) {
        if (targetScript.wheels.Length == 0) {
          EditorGUILayout.HelpBox("No wheels are assigned.", MessageType.Warning);
        } else if (this.targets.Length == 1) {
          EditorGUILayout.LabelField("Top Speed (Estimate): "
                                     + (this.topSpeed * 2.23694f).ToString("0.00")
                                     + " mph || "
                                     + (this.topSpeed * 3.6f).ToString("0.00")
                                     + " km/h",
                                     EditorStyles.boldLabel);
        }
      } else {
        EditorGUILayout.HelpBox("No wheels are assigned.", MessageType.Warning);
      }

      this.DrawDefaultInspector();

      if (!this.isPrefab && targetScript.gameObject.activeInHierarchy) {
        showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
        EditorGUI.indentLevel++;
        if (showButtons) {
          if (GUILayout.Button("Get Wheels")) {
            foreach (var curTarget in allTargets) {
              curTarget.wheels = F.GetTopmostParentComponent<VehicleParent>(curTarget.transform).transform
                                  .GetComponentsInChildren<HoverWheel>();
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
