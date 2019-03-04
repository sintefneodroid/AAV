using System;
using System.Collections.Generic;

namespace AAV.Drone.Script.PID {
  /// <summary>
  /// 
  /// </summary>
  public class HoverTestRun {
    bool _is_started;
    List<double[]> _test_data;
    int _oscillation_count;
    double _max_duration;
    double _start_time;
    double _duration;

    public HoverTestRun() { this.Reset(); }

    /// <summary>
    /// 
    /// </summary>
    public List<Double[]> TestData { get { return this._test_data; } set { this._test_data = value; } }

    /// <summary>
    /// 
    /// </summary>
    public Int32 OscillationCount {
      get { return this._oscillation_count; }
      set { this._oscillation_count = value; }
    }

    /// <summary>
    /// 
    /// </summary>
    public Double Duration { get { return this._duration; } set { this._duration = value; } }

    /*public void AddPose (PoseStamped ps)
  {
    if ( !isStarted )
      return;

    if ( startTime == 0 )
    {
      startTime = ps.header.Stamp.data.toSec ();
      return;
    }

    testData.Add ( new double[] {
      ps.header.Stamp.data.toSec (),
      ps.pose.position.z
    } );
  }*/

    public double ComputeTotalError(double set_point) {
      double total_error = 0;
      foreach (var item in this._test_data) {
        total_error += Math.Abs(set_point - item[1]);
      }

      return total_error;
    }

    public bool IsFinished() {
      if (!this._is_started || this._test_data.Count == 0) {
        return false;
      }

      // test is finished if we've reached max duration
      var duration = this._test_data[this._test_data.Count - 1][0] - this._start_time;
      if (duration > this._max_duration) {
        this._is_started = false;
        return true;
      }

      return false;
    }

    public void Reset() {
      this._is_started = false;
      this._test_data = new List<double[]>();
      this._oscillation_count = 0;
      this._max_duration = 0;
      this._start_time = 0;
      this._duration = 0;
    }

    public void Start() { this._is_started = true; }
  }
}
