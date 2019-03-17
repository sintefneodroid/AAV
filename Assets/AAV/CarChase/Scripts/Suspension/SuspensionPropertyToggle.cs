using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Suspension {
  [RequireComponent(typeof(Suspension))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Suspension/Suspension Property", 2)]

  //Class for changing the properties of the suspension
  public class SuspensionPropertyToggle : MonoBehaviour {
    public SuspensionToggledProperty[] properties;
    Suspension sus;

    void Start() { this.sus = this.GetComponent<Suspension>(); }

    //Toggle a property in the properties array at index
    public void ToggleProperty(int index) {
      if (this.properties.Length - 1 >= index) {
        this.properties[index].toggled = !this.properties[index].toggled;

        if (this.sus) {
          this.sus.UpdateProperties();
        }
      }
    }

    //Set a property in the properties array at index to the value
    public void SetProperty(int index, bool value) {
      if (this.properties.Length - 1 >= index) {
        this.properties[index].toggled = value;

        if (this.sus) {
          this.sus.UpdateProperties();
        }
      }
    }
  }

  //Class for a single property
  [Serializable]
  public class SuspensionToggledProperty {
    public enum Properties {
      steerEnable,
      steerInvert,
      driveEnable,
      driveInvert,
      ebrakeEnable,
      skidSteerBrake
    } //The type of property
    //steerEnable = enable steering
    //steerInvert = invert steering
    //driveEnable = enable driving
    //driveInvert = invert drive
    //ebrakeEnable = can ebrake
    //skidSteerBrake = brake is specially adjusted for skid steering

    public Properties property; //The property
    public bool toggled; //Is it enabled?
  }
}
