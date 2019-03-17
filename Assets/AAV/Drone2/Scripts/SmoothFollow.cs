using UnityEngine;

namespace AAV.Drone2.Scripts {
    public class SmoothFollow : MonoBehaviour {
        public bool shouldRotate = true;

        // The target we are following
        public Transform target;

        float _wanted_rotation_angle;
        float _current_rotation_angle;

        Quaternion _current_rotation;

        Vector3 _offset_postion, _final_position;

        void Start() {
            this._offset_postion = new Vector3(0, 5, -10);
        }

        void LateUpdate() {
            if (!this.target) {
                return;
            }

            // Calculate the current rotation angles
            this._wanted_rotation_angle = this.target.eulerAngles.y;
            this._current_rotation_angle = this.transform.eulerAngles.y;

            // Damp the rotation around the y-axis
            this._current_rotation_angle = Mathf.LerpAngle(this._current_rotation_angle, this._wanted_rotation_angle, 0.3f);

            // Convert the angle into a rotation
            this._current_rotation = Quaternion.Euler(0, this._current_rotation_angle, 0);

            // Set the position of the camera on the x-z plane to:
            // distance meters behind the target
            this.transform.position = this.target.position + (this._current_rotation * this._offset_postion);

            // Always look at the target
            this.transform.LookAt(this.target);
        }
    }
}