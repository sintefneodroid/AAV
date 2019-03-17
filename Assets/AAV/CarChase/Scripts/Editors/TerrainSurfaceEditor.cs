#if UNITY_EDITOR
using AAV.CarChase.Scripts.Ground;
using UnityEditor;
using UnityEngine;

namespace AAV.CarChase.Scripts.Editors {
  [CustomEditor(typeof(TerrainSurface))]
  public class TerrainSurfaceEditor : Editor {
    TerrainData terDat;
    TerrainSurface targetScript;
    string[] surfaceNames;

    public override void OnInspectorGUI() {
      var surfaceMaster = FindObjectOfType<GroundSurfaceMaster>();
      this.targetScript = (TerrainSurface)this.target;
      Undo.RecordObject(this.targetScript, "Terrain Surface Change");

      if (this.targetScript.GetComponent<Terrain>().terrainData) {
        this.terDat = this.targetScript.GetComponent<Terrain>().terrainData;
      }

      EditorGUILayout.LabelField("Textures and Surface Types:", EditorStyles.boldLabel);

      this.surfaceNames = new string[surfaceMaster.surfaceTypes.Length];

      for (var i = 0; i < this.surfaceNames.Length; i++) {
        this.surfaceNames[i] = surfaceMaster.surfaceTypes[i].name;
      }

      if (this.targetScript.surfaceTypes.Length > 0) {
        for (var j = 0; j < this.targetScript.surfaceTypes.Length; j++) {
          this.DrawTerrainInfo(this.terDat, j);
        }
      } else {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("<No terrain textures found>");
      }

      if (GUI.changed) {
        EditorUtility.SetDirty(this.targetScript);
      }
    }

    void DrawTerrainInfo(TerrainData ter, int index) {
      EditorGUI.indentLevel = 1;
      this.targetScript.surfaceTypes[index] =
          EditorGUILayout.Popup(this.terDat.splatPrototypes[index].texture.name,
                                this.targetScript.surfaceTypes[index],
                                this.surfaceNames);
      EditorGUI.indentLevel++;
    }
  }
}
#endif
