using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Drivetrain {
  [AddComponentMenu("RVP/Drivetrain/Drive Force", 3)]

  //The class for RPMs and torque sent through the drivetrain
  public class DriveForce : MonoBehaviour {
    [NonSerialized] public float rpm;
    [NonSerialized] public float torque;
    [NonSerialized] public AnimationCurve curve; //Torque curve
    [NonSerialized] public float feedbackRPM; //RPM sent back through the drivetrain
    [NonSerialized] public bool active = true;

    public void SetDrive(DriveForce from) {
      this.rpm = from.rpm;
      this.torque = from.torque;
      this.curve = from.curve;
    }

    //Same as previous, but with torqueFactor multiplier for torque
    public void SetDrive(DriveForce from, float torqueFactor) {
      this.rpm = from.rpm;
      this.torque = from.torque * torqueFactor;
      this.curve = from.curve;
    }
  }
}
