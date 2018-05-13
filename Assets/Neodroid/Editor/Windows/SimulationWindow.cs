﻿#if UNITY_EDITOR
using System.Collections.Generic;
using Neodroid.Environments;
using Neodroid.Managers;
using Neodroid.Prototyping.Actors;
using Neodroid.Prototyping.Configurables;
using Neodroid.Prototyping.Displayers;
using Neodroid.Prototyping.Evaluation;
using Neodroid.Prototyping.Motors;
using Neodroid.Prototyping.Observers;
using Neodroid.Utilities;
using Neodroid.Utilities.Enums;
using Neodroid.Utilities.GameObjects;
using Neodroid.Utilities.Interfaces;
using Neodroid.Utilities.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Neodroid.Editor.Windows {
  /// <inheritdoc />
  /// <summary>
  /// </summary>
  public class SimulationWindow : EditorWindow {
    int _logo_image_size = 120;
    PrototypingEnvironment[] _environments;
    Texture _icon;
    Texture _neodroid_icon;
    Vector2 _scroll_position;
    bool[] _show_environment_properties = new bool[1];

    /// <summary>
    ///
    /// </summary>
    NeodroidManager _simulation_manager;

    PlayerControls.PlayerReactions _player_reactions;

    /// <summary>
    ///
    /// </summary>
    [SerializeField]
    System.Boolean _refresh_enabled;

    /// <summary>
    ///
    /// </summary>
    [MenuItem(EditorWindowMenuPath._WindowMenuPath + "SimulationWindow")]
    [MenuItem(EditorWindowMenuPath._ToolMenuPath + "SimulationWindow")]
    public static void ShowWindow() {
      GetWindow(typeof(SimulationWindow)); //Show existing window instance. If one doesn't exist, make one.
      //window.Show();
    }

    /// <summary>
    ///
    /// </summary>
    void OnEnable() {
      this._icon = (Texture2D)AssetDatabase.LoadAssetAtPath(
          "Assets/Neodroid/Gizmos/Icons/world.png",
          typeof(Texture2D));
      this._neodroid_icon = (Texture)AssetDatabase.LoadAssetAtPath(
          "Assets/Neodroid/Gizmos/Icons/neodroid_favicon_cut.png",
          typeof(Texture));
      this.titleContent = new GUIContent("Neo:Sim", this._icon, "Window for configuring simulation");
      this.Setup();
    }

    /// <summary>
    ///
    /// </summary>
    void Setup() {
      if (this._environments != null) {
        this._show_environment_properties = new bool[this._environments.Length];
      }
    }



    /// <summary>
    ///
    /// </summary>
    void OnGUI() {
      var serialised_object = new SerializedObject(this);
      this._simulation_manager = FindObjectOfType<PausableManager>();
      if (this._simulation_manager) {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label(
            this._neodroid_icon,
            GUILayout.Width(this._logo_image_size),
            GUILayout.Height(this._logo_image_size));

        EditorGUILayout.BeginVertical();
        EditorGUILayout.ObjectField(this._simulation_manager, typeof(NeodroidManager), true);

        this._simulation_manager.Configuration = (SimulatorConfiguration)EditorGUILayout.ObjectField(this._simulation_manager.Configuration, typeof(SimulatorConfiguration), true);

        this._simulation_manager.Configuration.FrameSkips = EditorGUILayout.IntField(
            "Frame Skips",
            this._simulation_manager.Configuration.FrameSkips);
        this._simulation_manager.Configuration.ResetIterations = EditorGUILayout.IntField(
            "Reset Iterations",
            this._simulation_manager.Configuration.ResetIterations);
        this._simulation_manager.Configuration.SimulationType =
            (SimulationType)EditorGUILayout.EnumPopup(
                "Simulation Type",
                this._simulation_manager.Configuration.SimulationType);
        this._simulation_manager.TestMotors = EditorGUILayout.Toggle(
            "Test Motors",
            this._simulation_manager.TestMotors);

        this._player_reactions = FindObjectOfType<PlayerControls.PlayerReactions>();
        EditorGUILayout.ObjectField(this._player_reactions, typeof(PlayerControls.PlayerReactions), true);

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        this._environments = Utilities.Unsorted.NeodroidUtilities.FindAllObjectsOfTypeInScene<PrototypingEnvironment>();
        if (this._show_environment_properties.Length != this._environments.Length) {
          this.Setup();
        }

        this._scroll_position = EditorGUILayout.BeginScrollView(this._scroll_position);

        EditorGUILayout.BeginVertical("Box");
        GUILayout.Label($"Environments - Active(?), Inactive(?), Total({this._environments.Length})");
        if (this._show_environment_properties != null) {
          for (var i = 0; i < this._show_environment_properties.Length; i++) {
            if (this._environments[i].isActiveAndEnabled) {
              this._show_environment_properties[i] = EditorGUILayout.Foldout(
                  this._show_environment_properties[i],
                  $"{this._environments[i].Identifier}");
              if (this._show_environment_properties[i]) {
                var actors = this._environments[i].Actors;
                var observers = this._environments[i].Observers;
                var configurables = this._environments[i].Configurables;
                var resetables = this._environments[i].Resetables;
                var listeners = this._environments[i].Listeners;
                var displayers = this._environments[i].Displayers;

                EditorGUILayout.BeginVertical("Box");
                this._environments[i].enabled = EditorGUILayout.BeginToggleGroup(
                    this._environments[i].Identifier,
                    this._environments[i].enabled && this._environments[i].gameObject.activeSelf);
                EditorGUILayout.ObjectField(this._environments[i], typeof(PrototypingEnvironment), true);
                this._environments[i].CoordinateSystem = (CoordinateSystem)EditorGUILayout.EnumPopup(
                    "Coordinate system",
                    this._environments[i].CoordinateSystem);
                EditorGUI.BeginDisabledGroup(
                    this._environments[i].CoordinateSystem != CoordinateSystem.Relative_to_reference_point_);
                this._environments[i].CoordinateReferencePoint = (Transform)EditorGUILayout.ObjectField(
                    "Reference point",
                    this._environments[i].CoordinateReferencePoint,
                    typeof(Transform),
                    true);
                EditorGUI.EndDisabledGroup();
                this._environments[i].ObjectiveFunction = (ObjectiveFunction)EditorGUILayout.ObjectField(
                    "Objective function",
                    this._environments[i].ObjectiveFunction,
                    typeof(ObjectiveFunction),
                    true);
                this._environments[i].EpisodeLength = EditorGUILayout.IntField(
                    "Episode Length",
                    this._environments[i].EpisodeLength);

                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Actors");
                foreach (var actor in actors) {
                  if (actor.Value != null) {
                    var motors = actor.Value.Motors;

                    EditorGUILayout.BeginVertical("Box");
                    actor.Value.enabled = EditorGUILayout.BeginToggleGroup(
                        actor.Key,
                        actor.Value.enabled && actor.Value.gameObject.activeSelf);
                    EditorGUILayout.ObjectField(actor.Value, typeof(Actor), true);

                    EditorGUILayout.BeginVertical("Box");
                    GUILayout.Label("Motors");
                    foreach (var motor in motors) {
                      if (motor.Value != null) {
                        EditorGUILayout.BeginVertical("Box");
                        motor.Value.enabled = EditorGUILayout.BeginToggleGroup(
                            motor.Key,
                            motor.Value.enabled && motor.Value.gameObject.activeSelf);
                        EditorGUILayout.ObjectField(motor.Value, typeof(Motor), true);
                        EditorGUILayout.EndToggleGroup();

                        EditorGUILayout.EndVertical();
                      }
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndToggleGroup();

                    EditorGUILayout.EndVertical();
                  }
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Observers");
                foreach (var observer in observers) {
                  if (observer.Value != null) {
                    EditorGUILayout.BeginVertical("Box");
                    observer.Value.enabled = EditorGUILayout.BeginToggleGroup(
                        observer.Key,
                        observer.Value.enabled && observer.Value.gameObject.activeSelf);
                    EditorGUILayout.ObjectField(observer.Value, typeof(Observer), true);
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.EndVertical();
                  }
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Configurables");
                foreach (var configurable in configurables) {
                  if (configurable.Value != null) {
                    EditorGUILayout.BeginVertical("Box");
                    configurable.Value.enabled = EditorGUILayout.BeginToggleGroup(
                        configurable.Key,
                        configurable.Value.enabled && configurable.Value.gameObject.activeSelf);
                    EditorGUILayout.ObjectField(configurable.Value, typeof(ConfigurableGameObject), true);
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.EndVertical();
                  }
                }

                EditorGUILayout.EndVertical();



                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Displayers");
                foreach (var displayer in displayers) {
                  if (displayer.Value != null) {
                    EditorGUILayout.BeginVertical("Box");
                    displayer.Value.enabled = EditorGUILayout.BeginToggleGroup(
                        displayer.Key,
                        displayer.Value.enabled && displayer.Value.gameObject.activeSelf);
                    EditorGUILayout.ObjectField(displayer.Value, typeof(Displayer), true);
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.EndVertical();
                  }
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Internals");
                foreach (var resetable in resetables) {
                  if (resetable.Value != null) {
                    EditorGUILayout.BeginVertical("Box");
                    resetable.Value.enabled = EditorGUILayout.BeginToggleGroup(
                        resetable.Key,
                        resetable.Value.enabled && resetable.Value.gameObject.activeSelf);
                    EditorGUILayout.ObjectField(resetable.Value, typeof(Prototyping.Internals.Resetable), true);
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.EndVertical();
                  }

                }

                foreach (var listener in listeners) {
                  if (listener.Value != null) {
                    EditorGUILayout.BeginVertical("Box");
                    listener.Value.enabled = EditorGUILayout.BeginToggleGroup(
                        listener.Key,
                        listener.Value.enabled && listener.Value.gameObject.activeSelf);
                    EditorGUILayout.ObjectField(
                        listener.Value,
                        typeof(Prototyping.Internals.EnvironmentListener),
                        true);
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.EndVertical();
                  }
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.EndToggleGroup();
                EditorGUILayout.EndVertical();
              }
            }
          }

          EditorGUILayout.EndVertical();

          EditorGUILayout.BeginVertical("Box");
          GUILayout.Label("Disabled environments");
          for (var i = 0; i < this._show_environment_properties.Length; i++) {
            if (!this._environments[i].isActiveAndEnabled) {
              EditorGUILayout.ObjectField(this._environments[i], typeof(NeodroidEnvironment),true);
            }
          }

          EditorGUILayout.EndVertical();

          EditorGUILayout.EndScrollView();
          serialised_object.ApplyModifiedProperties();

          /*
          if (GUILayout.Button("Refresh")) {
            this.Refresh();
          }

          EditorGUI.BeginDisabledGroup(!Application.isPlaying);

          if (GUILayout.Button("Step")) {
            this._simulation_manager.ReactAndCollectStates(
                new Reaction(
                    new ReactionParameters(true, true, episode_count:true),
                    null,
                    null,
                    null,
                    null,
                    ""));
          }

          if (GUILayout.Button("Reset")) {
            this._simulation_manager.ReactAndCollectStates(
                new Reaction(
                    new ReactionParameters(true, false, true, episode_count:true),
                    null,
                    null,
                    null,
                    null,
                    ""));
          }

          EditorGUI.EndDisabledGroup();
          */
        }
      }
    }

    /// <summary>
    ///
    /// </summary>
    void Refresh() {

      if(this._simulation_manager) {
        this._simulation_manager.Clear();
      }

      var prototyping_game_objects = FindObjectsOfType<PrototypingGameObject>();
      foreach (var obj in prototyping_game_objects) {
        obj.RefreshAwake();
      }

      foreach (var obj in prototyping_game_objects) {
        obj.RefreshStart();
      }
    }

    /// <summary>
    ///
    /// </summary>
    void OnValidate() {

      if(EditorApplication.isPlaying || !this._refresh_enabled) {
        return;
      }

      this.Refresh();
    }

    /// <summary>
    ///
    /// </summary>
    void OnHierarchyChange() {
      this.Refresh();
    }

    /// <summary>
    ///
    /// </summary>
    public void OnInspectorUpdate() {
      this.Repaint();
      if (GUI.changed) {
        this.Refresh();
      }
    }
  }
}
#endif
