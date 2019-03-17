#if UNITY_EDITOR
using AAV.CarChase.Scripts.Drivetrain;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(GearboxTransmission))]
  [CanEditMultipleObjects]
  public class GearboxTransmissionEditor : Editor {
    bool isPrefab;
    static bool showButtons = true;

    public override void OnInspectorGUI() {
      var boldFoldout = new GUIStyle(EditorStyles.foldout);
      boldFoldout.fontStyle = FontStyle.Bold;
      var targetScript = (GearboxTransmission)this.target;
      var allTargets = new GearboxTransmission[this.targets.Length];
      this.isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

      for (var i = 0; i < this.targets.Length; i++) {
        Undo.RecordObject(this.targets[i], "Transmission Change");
        allTargets[i] = this.targets[i] as GearboxTransmission;
      }

      this.DrawDefaultInspector();

      if (!this.isPrefab && targetScript.gameObject.activeInHierarchy) {
        showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
        EditorGUI.indentLevel++;
        if (showButtons) {
          if (GUILayout.Button("Calculate RPM Ranges")) {
            foreach (var curTarget in allTargets) {
              curTarget.CalculateRpmRanges();
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
