using System;

namespace SceneAssets.Excluded.Drone.PID {
  [Serializable]
  public class HoverControllerNode {
    public PidController _Controller;

    public Action<double> _ThrustCallback;

    public HoverControllerNode() {
      // PID params
      double ki_max = 20;
      double kp = 0;
      double ki = 0;
      double kd = 0;

      this._Controller = new PidController(kp, ki, kd, ki_max, 0);
    }

    public void SetStartTime(double start_time) { this._Controller.SetStartTime(start_time); }

    public void SetGoal(double target) { this._Controller.SetTarget(target); }

    public void UpdatePose(UnityEngine.Transform pose) {
      var z_cmd = this._Controller.Update(pose.position.z, UnityEngine.Time.time);

      this._ThrustCallback(z_cmd);
    }
  }
}
