#if UNITY_EDITOR
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(Suspension.Suspension))]
  [CanEditMultipleObjects]
  public class SuspensionEditor : Editor {
    bool isPrefab;
    static bool showButtons = true;

    public override void OnInspectorGUI() {
      var boldFoldout = new GUIStyle(EditorStyles.foldout);
      boldFoldout.fontStyle = FontStyle.Bold;
      var targetScript = (Suspension.Suspension)this.target;
      var allTargets = new Suspension.Suspension[this.targets.Length];
      this.isPrefab = PrefabUtility.GetPrefabType(targetScript) == PrefabType.Prefab;

      for (var i = 0; i < this.targets.Length; i++) {
        Undo.RecordObject(this.targets[i], "Suspension Change");
        allTargets[i] = this.targets[i] as Suspension.Suspension;
      }

      if (!targetScript.wheel) {
        EditorGUILayout.HelpBox("Wheel must be assigned.", MessageType.Error);
      }

      this.DrawDefaultInspector();

      if (!this.isPrefab && targetScript.gameObject.activeInHierarchy) {
        showButtons = EditorGUILayout.Foldout(showButtons, "Quick Actions", boldFoldout);
        EditorGUI.indentLevel++;
        if (showButtons) {
          if (GUILayout.Button("Get Wheel")) {
            foreach (var curTarget in allTargets) {
              curTarget.wheel = curTarget.transform.GetComponentInChildren<Wheel>();
            }
          }

          if (GUILayout.Button("Get Opposite Wheel")) {
            foreach (var curTarget in allTargets) {
              var vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(curTarget.transform);
              Suspension.Suspension closestOne = null;
              var closeDist = Mathf.Infinity;

              foreach (var curWheel in vp.wheels) {
                var curDist =
                    Vector2.Distance(new Vector2(curTarget.transform.localPosition.y,
                                                 curTarget.transform.localPosition.z),
                                     new Vector2(curWheel.transform.parent.localPosition.y,
                                                 curWheel.transform.parent.localPosition.z));
                if (Mathf.Sign(curTarget.transform.localPosition.x)
                    != Mathf.Sign(curWheel.transform.parent.localPosition.x)
                    && curDist < closeDist) {
                  closeDist = curDist;
                  closestOne = curWheel.transform.parent.GetComponent<Suspension.Suspension>();
                }
              }

              curTarget.oppositeWheel = closestOne;
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
