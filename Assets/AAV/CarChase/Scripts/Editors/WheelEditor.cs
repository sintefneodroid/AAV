#if UNITY_EDITOR
using AAV.CarChase.Scripts.Drivetrain;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(Wheel))]
  [CanEditMultipleObjects]
  public class WheelEditor : Editor {
    bool isPrefab;
    static bool showButtons = true;
    static float radiusMargin;
    static float widthMargin;

    public override void OnInspectorGUI() {
      var boldFoldout = new GUIStyle(EditorStyles.foldout);
      boldFoldout.fontStyle = FontStyle.Bold;
      var targetScript = (Wheel)this.target;
      var allTargets = new Wheel[this.targets.Length];
      this.isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

      for (var i = 0; i < this.targets.Length; i++) {
        Undo.RecordObject(this.targets[i], "Wheel Change");
        allTargets[i] = this.targets[i] as Wheel;
      }

      this.DrawDefaultInspector();

      if (!this.isPrefab && targetScript.gameObject.activeInHierarchy) {
        showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
        EditorGUI.indentLevel++;
        if (showButtons) {
          if (GUILayout.Button("Get Wheel Dimensions")) {
            foreach (var curTarget in allTargets) {
              curTarget.GetWheelDimensions(radiusMargin, widthMargin);
            }
          }

          EditorGUI.indentLevel++;
          radiusMargin = EditorGUILayout.FloatField("Radius Margin", radiusMargin);
          widthMargin = EditorGUILayout.FloatField("Width Margin", widthMargin);
          EditorGUI.indentLevel--;
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
