using System.Collections.Generic;
using UnityEngine;

namespace AAV.Drone.Script {
  /// <summary>
  ///
  /// </summary>
  public class Lidar : MonoBehaviour {
    static Transform _lidar_parent;
    [SerializeField] GameObject layer;
    [SerializeField] float lineWidth = 0.05f;
    [SerializeField] int rayCount = 100;
    [SerializeField] int layerCount = 15;

    List<LineRenderer> _layers;
    List<List<Vector3>> _lidar_points;
    [SerializeField] bool isRay;
    bool _is_layer;

    /// <summary>
    ///
    /// </summary>
    public System.Boolean IsRay { get { return this.isRay; } set { this.isRay = value; } }

    void Start() {
      if (_lidar_parent == null) {
        _lidar_parent = new GameObject("LidarParent").transform;
      }

      this.isRay = false;
      this._is_layer = true;

      this._layers = new List<LineRenderer>();
      this._lidar_points = new List<List<Vector3>>();

      if (this._is_layer) {
        for (var j = 0; j < this.layerCount; j++) {
          var get_layer = Instantiate(this.layer, _lidar_parent, true);
          this._layers.Add(get_layer.GetComponent<LineRenderer>());
          this._layers[j].positionCount = this.rayCount;
          //this._layers[j].SetWidth(this._LineWidth, this._LineWidth);
          this._layers[j].widthCurve = new AnimationCurve(
              new Keyframe(0.0f, this.lineWidth),
              new Keyframe(1.0f, this.lineWidth));

          this._lidar_points.Add(new List<Vector3>(new Vector3[this.rayCount]));
        }
      }
    }

    void FixedUpdate() { this.SenseDistance(); }

    public void SenseDistance() {
//		lidar_points.Clear();

      const System.Single angle_range = Mathf.PI;

      for (var j = 0; j < this.layerCount; j++) {
        var layer_points = this._lidar_points[j];

        for (var i = 0; i < this.rayCount; i++) {
          RaycastHit hit;

          var angle = -angle_range / 2 + i * (angle_range) / (this.rayCount - 1);

          var position = this.transform.position;
          var raycast_dir =  this.transform.TransformPoint(
                                (j * 1.0f + 2.5f) * Mathf.Cos(angle),
                                -1.0f,
                                (j * 1.0f + 2.5f) * Mathf.Sin(angle))
                            - position;
          Physics.Raycast(position, raycast_dir, out hit);

          var base_angle = this.transform.eulerAngles.y * Mathf.Deg2Rad;
          var raycast_render = this.transform.TransformPoint(
                                   (j * 1.0f + 2.5f) * Mathf.Cos(base_angle + angle),
                                   -1.0f,
                                   (j * 1.0f + 2.5f) * Mathf.Sin(base_angle + angle))
                               - position;

          if (hit.collider) {
            var vector_point = this.transform.TransformPoint(raycast_render.normalized * hit.distance * 4);
            if (this._is_layer) {
              layer_points[i] = vector_point;
            }
          } else {
            var vector_point = this.transform.TransformPoint(raycast_render.normalized * hit.distance * 1000);
            if (this._is_layer) {
              layer_points[i] = vector_point;
            }
          }
        }

        if (this._is_layer) {
          var line_renderer2 = this._layers[j];
          line_renderer2.SetPositions(layer_points.ToArray());
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<List<Vector3>> SendLidarData() { return this._lidar_points; }
  }
}
