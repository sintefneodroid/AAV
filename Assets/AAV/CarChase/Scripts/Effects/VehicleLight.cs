using System;
using AAV.CarChase.Scripts.Damage;
using UnityEngine;

namespace AAV.CarChase.Scripts.Effects {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Effects/Vehicle Light", 3)]

  //Class for individual vehicle lights
  public class VehicleLight : MonoBehaviour {
    Renderer rend;
    ShatterPart shatter;
    public bool on;

    [Tooltip(
        "Example: a brake light would be half on when the night lights are on, and fully on when the brakes are pressed")]
    public bool halfOn;

    public Light targetLight;

    [Tooltip(
        "A light shared with another vehicle light, will turn off if one of the lights break, then the unbroken light will turn on its target light")]
    public Light sharedLight;

    [Tooltip("Vehicle light that the shared light is shared with")]
    public VehicleLight sharer;

    public Material onMaterial;
    Material offMaterial;

    [NonSerialized] public bool shattered;

    void Start() {
      this.rend = this.GetComponent<Renderer>();
      if (this.rend) {
        this.offMaterial = this.rend.sharedMaterial;
      }

      this.shatter = this.GetComponent<ShatterPart>();
    }

    void Update() {
      if (this.shatter) {
        this.shattered = this.shatter.shattered;
      }

      //Configure shared light
      if (this.sharedLight && this.sharer) {
        this.sharedLight.enabled = this.on && this.sharer.on && !this.shattered && !this.sharer.shattered;
      }

      //Configure target light
      if (this.targetLight) {
        if (this.sharedLight && this.sharer) {
          this.targetLight.enabled = !this.shattered && this.on && !this.sharedLight.enabled;
        }
      }

      //Shatter logic
      if (this.rend) {
        if (this.shattered) {
          if (this.shatter.brokenMaterial) {
            this.rend.sharedMaterial = this.shatter.brokenMaterial;
          } else {
            this.rend.sharedMaterial = this.on || this.halfOn ? this.onMaterial : this.offMaterial;
          }
        } else {
          this.rend.sharedMaterial = this.on || this.halfOn ? this.onMaterial : this.offMaterial;
        }
      }
    }
  }
}
