#if UNITY_EDITOR
using AAV.CarChase.Scripts.Ground;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(GroundSurfaceInstance))]
  [CanEditMultipleObjects]
  public class GroundSurfaceInstanceEditor : Editor {
    public override void OnInspectorGUI() {
      var surfaceMaster = FindObjectOfType<GroundSurfaceMaster>();
      var targetScript = (GroundSurfaceInstance)this.target;
      var allTargets = new GroundSurfaceInstance[this.targets.Length];

      for (var i = 0; i < this.targets.Length; i++) {
        Undo.RecordObject(this.targets[i], "Ground Surface Change");
        allTargets[i] = this.targets[i] as GroundSurfaceInstance;
      }

      var surfaceNames = new string[surfaceMaster.surfaceTypes.Length];

      for (var i = 0; i < surfaceNames.Length; i++) {
        surfaceNames[i] = surfaceMaster.surfaceTypes[i].name;
      }

      foreach (var curTarget in allTargets) {
        curTarget.surfaceType = EditorGUILayout.Popup("Surface Type", curTarget.surfaceType, surfaceNames);
      }

      if (GUI.changed) {
        EditorUtility.SetDirty(targetScript);
      }
    }
  }
}
#endif
