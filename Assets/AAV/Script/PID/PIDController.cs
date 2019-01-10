using UnityEngine;

namespace AAV.PID {
  /// <summary>
  ///
  /// </summary>
  [System.Serializable]
  public class PidController {
    /// <summary>
    ///
    /// </summary>
    [SerializeField] double _Kp;
    /// <summary>
    ///
    /// </summary>
    [SerializeField] double _Ki;
    /// <summary>
    ///
    /// </summary>
    [SerializeField] double _Kd;
    double _error_sum;
    /// <summary>
    ///
    /// </summary>
    public double _SetPoint;
    /// <summary>
    ///
    /// </summary>
    public double _MaxWindup;
    double _start_time;
    double _last_timestamp;
    double _last_error;
    double _last_windup;

    public PidController(
        double kp = 0,
        double ki = 0,
        double kd = 0,
        double max_windup = 20,
        double start_time = 0) {
      this._Kp = kp;
      this._Ki = ki;
      this._Kd = kd;
      this._error_sum = 0;
      this._SetPoint = 0;
      this._MaxWindup = max_windup;
      this._start_time = start_time;

      this._last_timestamp = 0;
      this._last_error = 0;
    }

    /// <summary>
    ///
    /// </summary>
    public System.Double LastWindup { get { return this._last_windup; } set { this._last_windup = value; } }

    public System.Double StartTime { get { return this._start_time; } set { this._start_time = value; } }

    public void Reset() {
      this._Kp = 0;
      this._Ki = 0;
      this._Kd = 0;
      this._SetPoint = 0;
      this._error_sum = 0;
      this._last_timestamp = 0;
      this._last_error = 0;
      this._last_windup = 0;
    }

    public void SetTarget(double target) { this._SetPoint = target; }

    public void SetKp(double kp) { this._Kp = kp; }

    public void SetKi(double ki) { this._Ki = ki; }

    public void SetKd(double kd) { this._Kd = kd; }

    public void SetMaxWindup(double max) { this._MaxWindup = max; }

    public void SetStartTime(double time) { this._start_time = time; }

    public double Update(double measured_value, double timestamp) {
      var delta_time = timestamp - this._last_timestamp;
      if (System.Math.Abs(delta_time) < float.Epsilon) {
        Debug.Log("delta time was 0");
        return 0;
      }

      var error = this._SetPoint - measured_value;
      var delta_error = error - this._last_error;
      this._last_timestamp = timestamp;
      this._last_error = error;

      this._error_sum += error * delta_time;
      if (this._error_sum > this._MaxWindup) {
        this._error_sum = this._MaxWindup;
      } else if (this._error_sum < -this._MaxWindup) {
        this._error_sum = -this._MaxWindup;
      }

      var p = this._Kp * error;
      var i = this._Ki * this._error_sum;
      var d = this._Kd * (delta_error / delta_time);

      return p + i + d;
    }
  }
}
