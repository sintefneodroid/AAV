using System;
using AAV.CarChase.Scripts.Static;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;

namespace AAV.CarChase.Scripts.Drivetrain {
  [RequireComponent(typeof(DriveForce))]

  //Class for transmissions
  public abstract class Transmission : MonoBehaviour {
    [Range(0, 1)] public float strength = 1;
    [NonSerialized] public float health = 1;
    protected VehicleParent vp;
    protected DriveForce targetDrive;
    protected DriveForce newDrive;
    public bool automatic;

    [Tooltip("Apply special drive to wheels for skid steering")]
    public bool skidSteerDrive;

    public DriveForce[] outputDrives;

    [Tooltip("Exponent for torque output on each wheel")]
    public float driveDividePower = 3;

    [NonSerialized] public float maxRPM = -1;

    public virtual void Start() {
      this.vp = (VehicleParent)F.GetTopmostParentComponent<VehicleParent>(this.transform);
      this.targetDrive = this.GetComponent<DriveForce>();
      this.newDrive = this.gameObject.AddComponent<DriveForce>();
    }

    protected void SetOutputDrives(float ratio) {
      //Distribute drive to wheels
      if (this.outputDrives.Length > 0) {
        var enabledDrives = 0;

        //Check for which outputs are enabled
        foreach (var curOutput in this.outputDrives) {
          if (curOutput.active) {
            enabledDrives++;
          }
        }

        var torqueFactor = Mathf.Pow(1f / enabledDrives, this.driveDividePower);
        float tempRPM = 0;

        foreach (var curOutput in this.outputDrives) {
          if (curOutput.active) {
            tempRPM += this.skidSteerDrive ? Mathf.Abs(curOutput.feedbackRPM) : curOutput.feedbackRPM;
            curOutput.SetDrive(this.newDrive, torqueFactor);
          }
        }

        this.targetDrive.feedbackRPM = (tempRPM / enabledDrives) * ratio;
      }
    }

    public void ResetMaxRPM() {
      this.maxRPM = -1; //Setting this to -1 triggers subclasses to recalculate things
    }
  }
}
