using System;

namespace AAV.PID {
  [Serializable]
  public class HoverControllerNode {
    PidController _controller;

    Action<double> _thrust_callback;

    public HoverControllerNode() {
      // PID params
      double ki_max = 20;
      double kp = 0;
      double ki = 0;
      double kd = 0;

      this._controller = new PidController(kp, ki, kd, ki_max, 0);
    }

    public void SetStartTime(double start_time) { this._controller.SetStartTime(start_time); }

    public void SetGoal(double target) { this._controller.SetTarget(target); }

    public void UpdatePose(UnityEngine.Transform pose) {
      var z_cmd = this._controller.Update(pose.position.z, UnityEngine.Time.time);

      this._thrust_callback(z_cmd);
    }
  }
}
