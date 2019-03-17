using System;
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Suspension {
  [AddComponentMenu("RVP/Suspension/Suspension Property Setter", 3)]

  //Class for cycling through suspension properties
  public class PropertyToggleSetter : MonoBehaviour {
    [Tooltip("Steering Controller")] public SteeringControl steerer;
    public Transmission transmission;

    [Tooltip("Suspensions with properties to be toggled")]
    public SuspensionPropertyToggle[] suspensionProperties;

    public PropertyTogglePreset[] presets;
    public int currentPreset;

    [Tooltip("Input manager button which increments the preset")]
    public string changeButton;

    void Update() {
      if (!string.IsNullOrEmpty(this.changeButton)) {
        if (UnityEngine.Input.GetButtonDown(this.changeButton)) {
          this.ChangePreset(this.currentPreset + 1);
        }
      }
    }

    //Change the current preset
    public void ChangePreset(int preset) {
      this.currentPreset = preset % (this.presets.Length);

      if (this.steerer) {
        this.steerer.limitSteer = this.presets[this.currentPreset].limitSteer;
      }

      if (this.transmission) {
        this.transmission.skidSteerDrive = this.presets[this.currentPreset].skidSteerTransmission;
      }

      for (var i = 0; i < this.suspensionProperties.Length; i++) {
        for (var j = 0; j < this.suspensionProperties[i].properties.Length; j++) {
          this.suspensionProperties[i].SetProperty(j, this.presets[this.currentPreset].wheels[i].preset[j]);
        }
      }
    }
  }

  //Preset class
  [Serializable]
  public class PropertyTogglePreset {
    [Tooltip("Limit the steering range of wheels based on SteeringControl's curve?")]
    public bool limitSteer = true;

    [Tooltip("Transmission is adjusted for skid steering?")]
    public bool skidSteerTransmission;

    [Tooltip("Must be equal to the number of wheels")]
    public IndividualPreset[] wheels;
  }

  //Class for toggling the properties of SuspensionPropertyToggle instances
  [Serializable]
  public class IndividualPreset {
    [Tooltip("Must be equal to the SuspensionPropertyToggle properties array length")]
    public bool[] preset;
  }
}
