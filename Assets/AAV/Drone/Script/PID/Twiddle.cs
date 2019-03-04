using System;

namespace AAV.Drone.Script.PID {
// .....
// basically empty Tuple class to mimic .NET's since no access to that one here
  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="T1"></typeparam>
  /// <typeparam name="T2"></typeparam>
  /// <typeparam name="T3"></typeparam>
  public class Tuple<T1, T2, T3> {
    public T1 _First;
    public T2 _Second;
    public T3 _Third;

    public Tuple() : this(default(T1), default(T2), default(T3)) { }

    public Tuple(T1 t1, T2 t2, T3 t3) {
      this._First = t1;
      this._Second = t2;
      this._Third = t3;
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class Twiddle {
    Func<Double[], Double> _algorithm;
    double[] _parms;
    bool _first_run;
    double _best_err;
    double[] _dp;
    int _iterations;

    public Twiddle(Func<Double[], Double> algorithm, double[] _params) {
      this._algorithm = algorithm;
      this._parms = _params;
      this._first_run = true;
      this._best_err = 0;
      // ??
      this._dp = new double[_params.Length];
      for (var i = 0; i < this._dp.Length; i++) {
        this._dp[i] = 0.2;
      }

      this._iterations = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Tuple<Int32, Double[], Double> Run() {
      if (this._first_run) {
        // ??
        this._best_err = this._algorithm(this._parms);
//			self.best_err_ = self.algorithm_(self.params_)
        this._iterations++;
        this._first_run = false;
        return new Tuple<Int32, Double[], Double>(this._iterations, this._parms, this._best_err);
      }

      for (var i = 0; i < this._parms.Length; i++) {
        // update parameter and run algorithm
        this._parms[i] += this._dp[i];
        var err = this._algorithm(this._parms);

        if (err < this._best_err) {
          // looks good, increase parameters a little
          this._best_err = err;
          this._dp[i] *= 1.1;
        } else {
          // error got worse, decrease the parameter
          this._parms[i] -= 2 * this._dp[i];
          err = this._algorithm(this._parms);

          if (err < this._best_err) {
            this._best_err = err;
            this._dp[i] *= 1.1;
          } else {
            this._parms[i] += this._dp[i];
            this._dp[i] *= 0.9;
          }
        }
      }

      this._iterations++;
      return new Tuple<Int32, Double[], Double>(this._iterations, this._parms, this._best_err);
    }
  }
}
