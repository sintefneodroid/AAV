using System;
//using Neodroid.Runtime.Utilities.Structs;
using UnityEngine;

namespace AAV.PID {
  [Serializable]
  public class AttitudeControllerNode {
    Action<Vector3> _torque_callback;
    Action<double> _thrust_callback;

    PidController _roll_controller;
    PidController _pitch_controller;
    PidController _yaw_controller;
    Quaternion _last_imu;
    double _z_thrust;

    // Roll controller
    double _roll_kp = 0;
    double _roll_ki = 0;
    double _roll_ki_max = 0;
    double _roll_kd = 0;

    // Pitch controller
    double _pitch_kp = 0;
    double _pitch_ki = 0;
    double _pitch_ki_max = 0;
    double _pitch_kd = 0;

    // Yaw controller
    double _yaw_kp = 0;
    double _yaw_ki = 0;
    double _yaw_ki_max = 0;
    double _yaw_kd = 0;

    public AttitudeControllerNode() {
      //this._prev_time = new RosTime ( new Messages.TimeData ( 0, 0 ) );


      this._roll_controller = new PidController(this._roll_kp, this._roll_ki, this._roll_kd, this._roll_ki_max);


      this._pitch_controller = new PidController(this._pitch_kp, this._pitch_ki, this._pitch_kd, this._pitch_ki_max);


      this._yaw_controller = new PidController(this._yaw_kp, this._yaw_ki, this._yaw_kd, this._yaw_ki_max);

      //this._last_imu = new Imu ();
      this._z_thrust = 0;
    }

    public void Init() {
      //this._prev_time = ROS.GetTime ();
      //double t = this._prev_time.data.toSec ();
      var t = 0;
      this._roll_controller.SetStartTime(t);
      this._pitch_controller.SetStartTime(t);
      this._yaw_controller.SetStartTime(t);
    }

    public void SetStartTime(double start_time) {
      this._pitch_controller.SetStartTime(start_time);
      this._roll_controller.SetStartTime(start_time);
      this._yaw_controller.SetStartTime(start_time);
    }

    void UpdateAttitude(Vector3 att_vector) {
      this._roll_controller.SetTarget(att_vector.x);
      this._pitch_controller.SetTarget(att_vector.y);
      this._yaw_controller.SetTarget(att_vector.z);
    }

    void UpdateThrust(double thrust) { this._z_thrust = thrust; }

    public void UpdateImu(Quaternion quad) {
      this._last_imu = quad;
      this.Update();
    }

    void Update() {
      var q_temp = this._last_imu;
      var euler = q_temp.eulerAngles;
      var roll_cmd = this._roll_controller.Update(euler.x, Time.time);
      var pitch_cmd = this._pitch_controller.Update(euler.y, Time.time);
      var yaw_cmd = this._yaw_controller.Update(euler.z, Time.time);

      var s = $"pry: {euler.x},{euler.y},{euler.z} orientation: {q_temp.x},{q_temp.y},{q_temp.z},{q_temp.w}";
      Debug.Log(s);
      s = $"pry in degrees: {euler.x * Mathf.Rad2Deg},{euler.y * Mathf.Rad2Deg},{euler.z * Mathf.Rad2Deg}";
      Debug.Log(s);

      var v = new Vector3((float)roll_cmd, (float)pitch_cmd, (float)yaw_cmd);

      this._torque_callback(v);
      this._thrust_callback(this._z_thrust);
    }
  }
}
