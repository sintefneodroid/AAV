﻿#if UNITY_EDITOR
using Neodroid.Utilities.Segmentation;
using Neodroid.Utilities.Structs;
using UnityEditor;
using UnityEngine;

namespace Neodroid.Editor.Windows {
  public class SegmentationWindow : EditorWindow {
    [SerializeField] ColorByInstance[] _colors_by_instance;

    [SerializeField] ColorByTag[] _colors_by_tag;
    Texture _icon;

    Vector2 _scroll_position;

    [MenuItem(EditorWindowMenuPath._WindowMenuPath + "SegmentationWindow")]
    [MenuItem(EditorWindowMenuPath._ToolMenuPath + "SegmentationWindow")]
    public static void ShowWindow() {
      GetWindow(typeof(SegmentationWindow)); //Show existing window instance. If one doesn't exist, make one.
    }

    void OnEnable() {
      this._icon = (Texture2D)AssetDatabase.LoadAssetAtPath(
          "Assets/Neodroid/Gizmos/Icons/color_wheel.png",
          typeof(Texture2D));
      this.titleContent = new GUIContent("Neo:Seg", this._icon, "Window for segmentation");
    }

    void OnGUI() {
      GUILayout.Label("Segmentation Colors", EditorStyles.boldLabel);
      var serialised_object = new SerializedObject(this);
      this._scroll_position = EditorGUILayout.BeginScrollView(this._scroll_position);
      EditorGUILayout.BeginVertical("Box");
      GUILayout.Label("By Tag");
      var material_changers_by_tag = FindObjectsOfType<ChangeMaterialOnRenderByTag>();
      foreach (var material_changer_by_tag in material_changers_by_tag) {
        this._colors_by_tag = material_changer_by_tag.ColorsByTag;
        if (this._colors_by_tag != null) {
          var tag_colors_property = serialised_object.FindProperty("_segmentation_colors_by_tag");
          EditorGUILayout.PropertyField(
              tag_colors_property,
              new GUIContent(material_changer_by_tag.name),
              true); // True means show children
          material_changer_by_tag._Replace_Untagged_Color = EditorGUILayout.Toggle(
              "  -  Replace untagged colors",
              material_changer_by_tag._Replace_Untagged_Color);
          material_changer_by_tag._Untagged_Color = EditorGUILayout.ColorField(
              "  -  Untagged color",
              material_changer_by_tag._Untagged_Color);
        }
      }

      EditorGUILayout.EndVertical();

      /*var material_changer = FindObjectOfType<ChangeMaterialOnRenderByTag> ();
    if(material_changer){
      _segmentation_colors_by_game_object = material_changer.SegmentationColors;
      SerializedProperty game_object_colors_property = serialised_object.FindProperty ("_segmentation_colors_by_game_object");
      EditorGUILayout.PropertyField(tag_colors_property, true); // True means show children
    }*/
      EditorGUILayout.BeginVertical("Box");
      GUILayout.Label("By Instance (Not changable, only for inspection) ");
      var material_changers_by_instance = FindObjectsOfType<ChangeMaterialOnRenderByInstance>();
      foreach (var material_changer_by_instance in material_changers_by_instance) {
        this._colors_by_instance = material_changer_by_instance.InstanceColors;
        if (this._colors_by_instance != null) {
          var instance_colors_property = serialised_object.FindProperty("_segmentation_colors_by_instance");
          EditorGUILayout.PropertyField(
              instance_colors_property,
              new GUIContent(material_changer_by_instance.name),
              true); // True means show children
        }
      }

      EditorGUILayout.EndVertical();
      EditorGUILayout.EndScrollView();
      serialised_object.ApplyModifiedProperties(); // Remember to apply modified properties
    }
  }
}
#endif
