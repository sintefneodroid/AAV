#if UNITY_EDITOR
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Suspension;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(PropertyToggleSetter))]
  [CanEditMultipleObjects]
  public class PropertyToggleSetterEditor : Editor {
    bool isPrefab;
    static bool showButtons = true;

    public override void OnInspectorGUI() {
      var boldFoldout = new GUIStyle(EditorStyles.foldout);
      boldFoldout.fontStyle = FontStyle.Bold;
      var targetScript = (PropertyToggleSetter)this.target;
      var allTargets = new PropertyToggleSetter[this.targets.Length];
      this.isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

      for (var i = 0; i < this.targets.Length; i++) {
        Undo.RecordObject(this.targets[i], "Property Toggle Setter Change");
        allTargets[i] = this.targets[i] as PropertyToggleSetter;
      }

      this.DrawDefaultInspector();

      if (!this.isPrefab && targetScript.gameObject.activeInHierarchy) {
        showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
        EditorGUI.indentLevel++;
        if (showButtons) {
          if (GUILayout.Button("Get Variables")) {
            foreach (var curTarget in allTargets) {
              curTarget.steerer = curTarget.GetComponentInChildren<SteeringControl>();
              curTarget.transmission = curTarget.GetComponentInChildren<Transmission>();
              curTarget.suspensionProperties = curTarget.GetComponentsInChildren<SuspensionPropertyToggle>();
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
