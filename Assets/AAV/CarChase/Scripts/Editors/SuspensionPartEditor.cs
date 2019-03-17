#if UNITY_EDITOR
using AAV.CarChase.Scripts.Suspension;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(SuspensionPart))]
  [CanEditMultipleObjects]
  public class SuspensionPartEditor : Editor {
    static bool showHandles = true;

    public override void OnInspectorGUI() {
      showHandles = EditorGUILayout.Toggle("Show Handles", showHandles);
      SceneView.RepaintAll();

      this.DrawDefaultInspector();
    }

    public void OnSceneGUI() {
      var targetScript = (SuspensionPart)this.target;
      Undo.RecordObject(targetScript, "Suspension Part Change");

      if (showHandles && targetScript.gameObject.activeInHierarchy) {
        if (targetScript.connectObj
            && !targetScript.isHub
            && !targetScript.solidAxle
            && Tools.current == Tool.Move) {
          targetScript.connectPoint =
              targetScript.connectObj.InverseTransformPoint(Handles.PositionHandle(targetScript
                                                                                   .connectObj
                                                                                   .TransformPoint(targetScript
                                                                                                       .connectPoint),
                                                                                   Tools.pivotRotation
                                                                                   == PivotRotation.Local
                                                                                       ? targetScript
                                                                                         .connectObj.rotation
                                                                                       : Quaternion
                                                                                           .identity));
        }
      }

      if (GUI.changed) {
        EditorUtility.SetDirty(targetScript);
      }
    }
  }
}
#endif
