#if UNITY_EDITOR
using AAV.CarChase.Scripts.Hover;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(HoverWheel))]
  [CanEditMultipleObjects]
  public class HoverWheelEditor : Editor {
    bool isPrefab;
    static bool showButtons = true;

    public override void OnInspectorGUI() {
      var boldFoldout = new GUIStyle(EditorStyles.foldout);
      boldFoldout.fontStyle = FontStyle.Bold;
      var targetScript = (HoverWheel)this.target;
      var allTargets = new HoverWheel[this.targets.Length];
      this.isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

      for (var i = 0; i < this.targets.Length; i++) {
        Undo.RecordObject(this.targets[i], "Hover Wheel Change");
        allTargets[i] = this.targets[i] as HoverWheel;
      }

      this.DrawDefaultInspector();

      if (!this.isPrefab && targetScript.gameObject.activeInHierarchy) {
        showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
        EditorGUI.indentLevel++;
        if (showButtons) {
          if (GUILayout.Button("Get Visual Wheel")) {
            foreach (var curTarget in allTargets) {
              if (curTarget.transform.childCount > 0) {
                curTarget.visualWheel = curTarget.transform.GetChild(0);
              } else {
                Debug.LogWarning("No visual wheel found.", this);
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
