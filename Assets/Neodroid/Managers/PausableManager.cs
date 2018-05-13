﻿using System;
using Neodroid.Utilities.ScriptableObjects;
using UnityEngine;

namespace Neodroid.Managers {
  /// <inheritdoc />
  /// <summary>
  /// </summary>
  [AddComponentMenu("Neodroid/Managers/PausableManager")]
  public class PausableManager : NeodroidManager {
    #region Fields

    //[SerializeField] bool _blocked;
    #if UNITY_EDITOR
    /// <summary>
    ///
    /// </summary>
    [Header(
        "Warning! Will block editor requiring a restart, if not terminated while receiving.",
        order = 120)]
    [SerializeField]
    bool _allow_in_editor_blockage;
    #endif

    #endregion

    #region UnityCallbacks

    /// <summary>
    ///
    /// </summary>
    protected new void Awake() {
      base.Awake();
      if (this.Configuration.SimulationType == SimulationType.Frame_dependent_) {
        this.EarlyUpdateEvent += this.PauseSimulation;
        this.UpdateEvent += this.MaybeResume;
      } else if (this.Configuration.SimulationType == SimulationType.Physics_dependent_) {
        #if UNITY_EDITOR
        if (this._allow_in_editor_blockage) {
          this.EarlyFixedUpdateEvent +=
              this.Receive; // Receive blocks the main thread and therefore also the unity editor.
        } else {
          Debug.LogWarning(
              "Physics dependent blocking in editor is not enabled and is therefore not receiving or sending anything.");
        }
        #else
        this.EarlyFixedUpdateEvent += this.Receive;
        #endif
      }
    }

    #endregion

    #region PrivateMethods

    /// <summary>
    ///
    /// </summary>
    void MaybeResume() {
      if (this.TestMotors || (this.CurrentReactions.Length > 0 && this.CurrentReactions[0].Parameters.Step)) {
        this.ResumeSimulation(this.Configuration.TimeScale);
      }
    }

    /// <summary>
    ///
    /// </summary>
    public Boolean IsSimulationPaused { get { return !(this.SimulationTime > 0); } }

    /// <summary>
    ///
    /// </summary>
    void PauseSimulation() { this.SimulationTime = 0; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="simulation_time_scale"></param>
    void ResumeSimulation(float simulation_time_scale) {
      this.SimulationTime = simulation_time_scale > 0 ? simulation_time_scale : 1;
    }

    /// <summary>
    ///
    /// </summary>
    void Receive() {
      var reaction = this._Message_Server.Receive(TimeSpan.Zero);
      this.SetReactionFromExternalSource(reaction);
    }

    #endregion
  }
}
