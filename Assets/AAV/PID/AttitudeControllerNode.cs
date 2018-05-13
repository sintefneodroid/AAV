using System;
using UnityEngine;

namespace SceneAssets.Excluded.Drone.PID {
  [Serializable]
  public class AttitudeControllerNode {
    public Action<Neodroid.Utilities.Structs.DoubleVector3> _TorqueCallback;
    public Action<double> _ThrustCallback;

    public PidController _RollController;
    public PidController _PitchController;
    public PidController _YawController;
    Quaternion _last_imu;
    double _z_thrust;

    // Roll controller
    double roll_kp = 0;
    double roll_ki = 0;
    double roll_ki_max = 0;
    double roll_kd = 0;
    
    // Pitch controller
    double pitch_kp = 0;
    double pitch_ki = 0;
    double pitch_ki_max = 0;
    double pitch_kd = 0;
    
    // Yaw controller
    double yaw_kp = 0;
    double yaw_ki = 0;
    double yaw_ki_max = 0;
    double yaw_kd = 0;
    
    public AttitudeControllerNode() {
      //this._prev_time = new RosTime ( new Messages.TimeData ( 0, 0 ) );


      this._RollController = new PidController(this.roll_kp, this.roll_ki, this.roll_kd, this.roll_ki_max);


      this._PitchController = new PidController(this.pitch_kp, this.pitch_ki, this.pitch_kd, this.pitch_ki_max);


      this._YawController = new PidController(this.yaw_kp, this.yaw_ki, this.yaw_kd, this.yaw_ki_max);

      //this._last_imu = new Imu ();
      this._z_thrust = 0;
    }

    public void Init() {
      //this._prev_time = ROS.GetTime ();
      //double t = this._prev_time.data.toSec ();
      var t = 0;
      this._RollController.SetStartTime(t);
      this._PitchController.SetStartTime(t);
      this._YawController.SetStartTime(t);
    }

    public void SetStartTime(double start_time) {
      this._PitchController.SetStartTime(start_time);
      this._RollController.SetStartTime(start_time);
      this._YawController.SetStartTime(start_time);
    }

    void UpdateAttitude(Neodroid.Utilities.Structs.DoubleVector3 att_vector) {
      this._RollController.SetTarget(att_vector.x);
      this._PitchController.SetTarget(att_vector.y);
      this._YawController.SetTarget(att_vector.z);
    }

    void UpdateThrust(double thrust) { this._z_thrust = thrust; }

    public void UpdateImu(Quaternion quad) {
      this._last_imu = quad;
      this.Update();
    }

    void Update() {
      var q_temp = this._last_imu;
      var euler = q_temp.eulerAngles;
      var roll_cmd = this._RollController.Update(euler.x, Time.time);
      var pitch_cmd = this._PitchController.Update(euler.y, Time.time);
      var yaw_cmd = this._YawController.Update(euler.z, Time.time);

      var s = $"pry: {euler.x},{euler.y},{euler.z} orientation: {q_temp.x},{q_temp.y},{q_temp.z},{q_temp.w}";
      Debug.Log(s);
      s = $"pry in degrees: {euler.x * Mathf.Rad2Deg},{euler.y * Mathf.Rad2Deg},{euler.z * Mathf.Rad2Deg}";
      Debug.Log(s);

      var v = new Neodroid.Utilities.Structs.DoubleVector3(roll_cmd, pitch_cmd, yaw_cmd);

      this._TorqueCallback(v);
      this._ThrustCallback(this._z_thrust);
    }
  }
}
