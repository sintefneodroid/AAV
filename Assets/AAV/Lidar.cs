using System.Collections.Generic;
using UnityEngine;

namespace SceneAssets.Excluded.Drone {
  public class Lidar : MonoBehaviour {
    static Transform _lidar_parent;
    public GameObject _Layer;
    public float _LineWidth = 0.05f;
    public int _RayCount = 100;
    public int _LayerCount = 15;

    List<LineRenderer> _layers;
    List<List<Vector3>> _lidar_points;
    [SerializeField] bool _is_ray;
    bool _is_layer;

    public System.Boolean IsRay { get { return this._is_ray; } set { this._is_ray = value; } }

    void Start() {
      if (_lidar_parent == null) {
        _lidar_parent = new GameObject("LidarParent").transform;
      }

      this._is_ray = false;
      this._is_layer = true;

      this._layers = new List<LineRenderer>();
      this._lidar_points = new List<List<Vector3>>();

      if (this._is_layer) {
        for (var j = 0; j < this._LayerCount; j++) {
          var get_layer = Instantiate(this._Layer);
          get_layer.transform.SetParent(_lidar_parent);
          this._layers.Add(get_layer.GetComponent<LineRenderer>());
          this._layers[j].positionCount = this._RayCount;
          //this._layers[j].SetWidth(this._LineWidth, this._LineWidth);
          this._layers[j].widthCurve = new AnimationCurve(
              new Keyframe(0.0f, this._LineWidth),
              new Keyframe(1.0f, this._LineWidth));
          
          this._lidar_points.Add(new List<Vector3>(new Vector3[this._RayCount]));
        }
      }
    }

    void FixedUpdate() { this.SenseDistance(); }

    public void SenseDistance() {
//		lidar_points.Clear();

      const System.Single angle_range = Mathf.PI;

      for (var j = 0; j < this._LayerCount; j++) {
        var layer_points = this._lidar_points[j];

        for (var i = 0; i < this._RayCount; i++) {
          RaycastHit hit;

          var angle = -angle_range / 2 + i * (angle_range) / (this._RayCount - 1);

          var raycast_dir = this.transform.TransformPoint(
                                (j * 1.0f + 2.5f) * Mathf.Cos(angle),
                                -1.0f,
                                (j * 1.0f + 2.5f) * Mathf.Sin(angle))
                            - this.transform.position;
          Physics.Raycast(this.transform.position, raycast_dir, out hit);

          var base_angle = this.transform.eulerAngles.y * Mathf.Deg2Rad;
          var raycast_render = this.transform.TransformPoint(
                                   (j * 1.0f + 2.5f) * Mathf.Cos(base_angle + angle),
                                   -1.0f,
                                   (j * 1.0f + 2.5f) * Mathf.Sin(base_angle + angle))
                               - this.transform.position;

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

    public List<List<Vector3>> SendLidarData() { return this._lidar_points; }
  }
}
