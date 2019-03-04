using System;
using UnityEngine;

//using Neodroid.Runtime.Utilities.Structs;

//from dynamic_reconfigure.server import Server
//from quad_controller.cfg import position_controller_paramsConfig

namespace AAV.Drone.Script.PID {
  [Serializable]
  public class PositionControllerNode {
    bool _first_pose_received;
     PidController _x_controller;
    PidController _y_controller;
     PidController _z_controller;

    Action<Vector3> _torque_callback;
    Action<double> _thrust_callback;

    // x config
    double x_kp = 0;
    double x_ki = 0;
    double x_ki_max = 0;
    double x_kd = 0;

    // y config
    double y_kp = 0;
    double y_ki = 0;
    double y_ki_max = 0;
    double y_kd = 0;

    // z config
    double z_kp = 0;
    double z_ki = 0;
    double z_ki_max = 0;
    double z_kd = 0;

    public PositionControllerNode() {
      this._first_pose_received = false;

      this._x_controller = new PidController(this.x_kp, this.x_ki, this.x_kd, this.x_ki_max);

      this._y_controller = new PidController(this.y_kp, this.y_ki, this.y_kd, this.y_ki_max);

      this._z_controller = new PidController(this.z_kp, this.z_ki, this.z_kd, this.z_ki_max);
    }

    static double Clamp(double value, double min, double max) {
      if (value > max) {
        value = max;
      } else if (value < min) {
        value = min;
      }

      return value;
    }

    public void SetStartTime(double start_time) {
      this._x_controller.SetStartTime(start_time);
      this._y_controller.SetStartTime(start_time);
      this._z_controller.SetStartTime(start_time);
    }

    public void SetGoal(Vector3 goal) {
      this._x_controller.SetTarget(goal.x);
      this._y_controller.SetTarget(goal.y);
      this._z_controller.SetTarget(goal.z);
    }

    public void UpdatePose(Transform ps) {
      if (!this._first_pose_received) {
        this._first_pose_received = true;
        this.SetGoal(ps.position);
      }

      var rpy_cmd = new Vector3();

      const Double radian = Math.PI / 180;
      double t = Time.time;

      // Control Roll to to move along Y
      var position = ps.position;
      var roll_cmd = this._x_controller.Update(position.x, t);
      roll_cmd = Clamp(roll_cmd, -10.0 * radian, 10.0 * radian);

      // Control Pitch to move along X
      var pitch_cmd = this._y_controller.Update(position.y, t);
      pitch_cmd = Clamp(pitch_cmd, -10.0 * radian, 10.0 * radian);

      // Control Thrust to move along Z
      var thrust = this._z_controller.Update(position.z, t);

      rpy_cmd.x = (float)roll_cmd;
      rpy_cmd.y = (float)pitch_cmd;

      var s = $"r: {roll_cmd}, p: {pitch_cmd}, thrust: {thrust}";
      Debug.Log(s);

      // publish
      this._torque_callback(rpy_cmd);
      this._thrust_callback(thrust);
    }
  }
}
