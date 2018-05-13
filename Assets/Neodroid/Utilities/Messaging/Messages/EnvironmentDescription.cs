﻿using System.Collections.Generic;
using Neodroid.Prototyping.Actors;
using Neodroid.Prototyping.Configurables;
using Neodroid.Utilities.ScriptableObjects;

namespace Neodroid.Utilities.Messaging.Messages {
  /// <summary>
  ///
  /// </summary>
  public class EnvironmentDescription {
    public EnvironmentDescription(
        int max_steps,
        SimulatorConfiguration simulation_configuration,
        Dictionary<string, Actor> actors,
        Dictionary<string, ConfigurableGameObject> configurables,
        float solved_threshold) {
      this.Configurables = configurables;
      this.Actors = actors;
      this.MaxSteps = max_steps;
      this.FrameSkips = simulation_configuration.FrameSkips;
      this.SolvedThreshold = solved_threshold;
      this.ApiVersion = NeodroidInfo._Version;
    }

    /// <summary>
    ///
    /// </summary>
    public string ApiVersion { get; }

    /// <summary>
    ///
    /// </summary>
    public Dictionary<string, Actor> Actors { get; }

    /// <summary>
    ///
    /// </summary>
    public Dictionary<string, ConfigurableGameObject> Configurables { get; }

    /// <summary>
    ///
    /// </summary>
    public int MaxSteps { get; }

    /// <summary>
    ///
    /// </summary>
    public int FrameSkips { get; }

    /// <summary>
    ///
    /// </summary>
    public float SolvedThreshold { get; }
  }
}
