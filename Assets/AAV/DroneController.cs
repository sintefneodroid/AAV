using UnityEngine;

namespace AAV {
  public class DroneController : MonoBehaviour {
    [SerializeField] float _add_force_factor = 9.82f;

    const float _mph_to_ms = 2.23693629205f;

    public static DroneController _ActiveController;

    /// <summary>
    /// 
    /// </summary>
    public bool MotorsEnabled { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public Vector3 Force { get { return this._force; } }
    /// <summary>
    /// 
    /// </summary>
    public Vector3 Torque { get { return this._torque; } }
    /// <summary>
    /// 
    /// </summary>
    public Vector3 Position { get; protected set; }
    /// <summary>
    /// 
    /// </summary>
    public Quaternion Rotation { get; protected set; }
    /// <summary>
    /// 
    /// </summary>
    public Vector3 AngularVelocity { get; protected set; }
    /// <summary>
    /// 
    /// </summary>
    public Vector3 LinearVelocity { get; protected set; }
    /// <summary>
    /// 
    /// </summary>
    public Vector3 LinearAcceleration { get; protected set; }
    public bool UseGravity { get; set; }
    public bool ConstrainForceX { get; set; }
    public bool ConstrainForceY { get; set; }
    public bool ConstrainForceZ { get; set; }
    public bool ConstrainTorqueX { get; set; }
    public bool ConstrainTorqueY { get; set; }
    public bool ConstrainTorqueZ { get; set; }

    public Transform[] Rotors { get { return this._rotors; } set { this._rotors = value; } }

    public Transform _FrontLeftRotor;
    public Transform _FrontRightRotor;
    public Transform _RearLeftRotor;
    public Transform _RearRightRotor;

    public Camera _DroneCam1;
    //public PathFollower _Pather;
    //public SimpleQuadController _InputCtrl;

    public bool _ClampForce = true;
    public bool _ClampTorque = true;
    public float _MaxForce = 100;
    public float _MaxTorqueDegrees = 17;
    public float _MaxTorqueRadians;
    public ForceMode _ForceMode = ForceMode.Force;
    public ForceMode _TorqueMode = ForceMode.Force;

    public Texture2D[] _AxisArrows;
    public Color[] _AxisColors;
    public float _ArrowScreenSize = 100f;
    public bool _DrawArrows;
    public bool _DrawArrowsAlways;
    public bool _ShowLegend;

    public bool _RotateWithTorque;

    public bool _SpinRotors = true;
    public float _MaxRotorRpm = 3600;
    [SerializeField] float _cur_rotor_speed;
    public bool _ShowTelemetry;

    // recording vars
    public float _PathRecordFrequency = 3;
    [System.NonSerialized] public bool _IsRecordingPath;
    float _next_node_time;

    // patrol vars
    public float _MaxPatrolSpeed = 30;
    public float _PatrolWaitTime = 3;
    public float _ParolAccelTime = 3;
    public float _GimbalSweepVAngle = 45;

    // target follow vars
    public float _FollowDistance = 2;
    public float _FollowHeight = 2;
    public float _MaxFollowSpeed = 15;
    public float _FollowAccelTime = 2;

    Rigidbody _rb;
    public BoxCollider _BoxCollider;
    Transform[] _rotors;
    Vector3 _force;
    Vector3 _torque;
    Vector3 _last_velocity;
    Ray _ray;

    RaycastHit _ray_hit;

    [SerializeField] float _cur_speed;

    byte[] _camera_data;
    bool _reset_flag;
    bool _set_pose_flag;
    bool _use_twist;

    Vector3 _pose_position;
    Quaternion _pose_orientation;
    Texture2D _dot;
    
    [SerializeField] float _stability = 0.3f;
    [SerializeField] float _stability_speed = 2.0f;

    void Awake() {
      if (_ActiveController == null) {
        _ActiveController = this;
      }

      this._rb = this.GetComponent<Rigidbody>();
      this._rotors = new[] {
          this._FrontLeftRotor,
          this._FrontRightRotor,
          this._RearLeftRotor,
          this._RearRightRotor
      };
      this.MotorsEnabled = false;

      this.UseGravity = this._rb.useGravity;
//		UpdateConstraints ();
      this._rb.maxAngularVelocity = Mathf.Infinity;
      //this._InputCtrl = this.GetComponent<SimpleQuadController> ();
//		dot = new Texture2D ( 1, 1 );
//		dot.SetPixel ( 0, 0, Color.white );
//		dot.Apply ();
//		Debug.Log ( "it: " + rb.inertiaTensor + " itr: " + rb.inertiaTensorRotation );
//		rb.ResetInertiaTensor ();
//		rb.inertiaTensorRotation = Quaternion.identity;
//		Debug.Log ( "2 it: " + rb.inertiaTensor + " itr: " + rb.inertiaTensorRotation );
//		gameObject.SetActive ( false );
//		gameObject.SetActive ( true );
    }

    void Update() {
      if (this._reset_flag) {
        this.ResetOrientation();
        this._reset_flag = false;
      }

      this.CheckSetPose();

      this.Position = this.transform.position;
      this.Rotation = this.transform.rotation;

      if (this._IsRecordingPath && Time.time > this._next_node_time) {
        this._next_node_time = Time.time + this._PathRecordFrequency;
      }
    }

    void LateUpdate() {
      if (this._reset_flag) {
        this.ResetOrientation();
        this._reset_flag = false;
      }

      this.CheckSetPose();

      var left_ctrl = Application.platform == RuntimePlatform.OSXPlayer
                          ? KeyCode.LeftCommand
                          : KeyCode.LeftControl;
      var right_ctrl = Application.platform == RuntimePlatform.OSXPlayer
                           ? KeyCode.RightCommand
                           : KeyCode.RightControl;
      if (Input.GetKeyDown(KeyCode.Q) && (Input.GetKey(left_ctrl) || Input.GetKeyDown(right_ctrl))) {
        Application.Quit();
      }

      if (Input.GetKeyDown(KeyCode.L)) {
        this._ShowLegend = !this._ShowLegend;
      }

      if (this._RotateWithTorque) {
        float z_angle;
        var up = this.transform.up;
        if (up.y >= 0) {
          z_angle = this.transform.localEulerAngles.z;
        } else {
          z_angle = -this.transform.localEulerAngles.z;
        }

        while (z_angle > 180) {
          z_angle -= 360;
        }

        while (z_angle < -360) {
          z_angle += 360;
        }

        this.transform.Rotate(Vector3.up * -z_angle * Time.deltaTime, Space.World);
      }

      if (this._SpinRotors) {
        var rps = this._MaxRotorRpm / 60f;
        var deg_per_sec = rps * 360f;
        this._cur_rotor_speed = deg_per_sec;

//			if ( inputCtrl.active )
//			{
//				curRotorSpeed = degPerSec;
//			} else
//			{
//				if ( useTwist )
//				{
//					curRotorSpeed = Mathf.InverseLerp ( Physics.gravity.y, -Physics.gravity.y, rb.velocity.y ) * degPerSec;
//					//				curRotorSpeed = 0.5f * degPerSec * ( rb.velocity.y + Physics.gravity.y ) / -Physics.gravity.y / rb.mass;
//				} else
//				{
//					curRotorSpeed = 0.5f * degPerSec * force.y / -Physics.gravity.y / rb.mass;
//				}
//				
//			}

        // use forward for now because rotors are rotated -90
        var rot = Vector3.forward * this._cur_rotor_speed * Time.deltaTime;
        this._FrontLeftRotor.Rotate(rot);
        this._FrontRightRotor.Rotate(-rot);
        this._RearLeftRotor.Rotate(-rot);
        this._RearRightRotor.Rotate(rot);
      }
    }

    void FixedUpdate() {
      this._rb.AddRelativeForce(Vector3.up * this._add_force_factor);
      
      var predicted_up = Quaternion.AngleAxis(
                             this._rb.angularVelocity.magnitude
                             * Mathf.Rad2Deg
                             * this._stability
                             / this._stability_speed,
                             this._rb.angularVelocity)
                         * this.transform.up;
      var torque_vector = Vector3.Cross(predicted_up, Vector3.up);
      this._rb.AddTorque(torque_vector * this._stability_speed * this._stability_speed);
    }

/*	void FixedUpdate ()
	{
		if ( resetFlag )
		{
			ResetOrientation ();
			resetFlag = false;
		}
		CheckSetPose ();

		rb.useGravity = UseGravity;
		CheckConstraints ();

		if ( MotorsEnabled )
		{
			if ( useTwist )
			{
				// just set linear and angular velocities, ignoring forces
				rb.velocity = LinearVelocity;
//				rb.velocity = clampMaxSpeed ? Vector3.ClampMagnitude ( LinearVelocity, maxSpeedMS ) : LinearVelocity;
				// new: flip angular velocity to generate CCW rotations
//				Vector3 angVel = -AngularVelocity;
//				if ( ConstrainTorqueX )
//					angVel.z = 0;
//				if ( ConstrainTorqueY )
//					angVel.x = 0;
//				if ( ConstrainTorqueZ )
//					angVel.y = 0;
//				rb.angularVelocity = angVel;
				rb.angularVelocity = -AngularVelocity;

			} else
			{

				// add force
				if ( clampForce )
					force = Vector3.ClampMagnitude ( force, maxForce );
				rb.AddRelativeForce ( force, forceMode );
				
				// add torque. but first clamp it
				if ( maxTorqueDegrees != 0 )
					maxTorqueRadians = maxTorqueDegrees * Mathf.Deg2Rad;

				if ( clampTorque )
					torque = Vector3.ClampMagnitude ( torque, maxTorqueRadians );
//				rb.AddRelativeTorque ( newTorque, torqueMode );
				rb.AddRelativeTorque ( -torque, torqueMode );

				// update acceleration
				LinearAcceleration = ( rb.velocity - lastVelocity ) / Time.deltaTime;
				lastVelocity = rb.velocity;
				LinearVelocity = rb.velocity;
				// new: flip angular velocity to match flipped torque
//				rb.angularVelocity = angVel;
//				AngularVelocity = -angVel;
				AngularVelocity = -rb.angularVelocity;
			}
		}
		curSpeed = rb.velocity.magnitude;
	}*/

    /*
    void OnGUI() {
      var info = "";
      Vector2 size;
      Rect r;
      var label = GUI.skin.label;
      var clipping = label.clipping;
      label.clipping = TextClipping.Overflow;
      var wrap = label.wordWrap;
      label.wordWrap = false;
      var font_size = label.fontSize;
      label.fontSize = (int)(22f * Screen.height / 1080);

      
      if (this._ShowTelemetry) {
        info = @"Force: "
               + this.Force.ToRos().ToString()
               + "\nTorque: "
               + this.Torque.ToRos().ToString()
               + "\nPosition: "
               + this.Position.ToRos().ToString()
               + "\nRPY: "
               + (-this.Rotation.eulerAngles).ToRos().ToString()
               + "\nLinear Vel.: "
               + this.LinearVelocity.ToRos().ToString()
               + "\nAngular Vel.: "
               + this.AngularVelocity.ToRos().ToString()
               + "\nGravity "
               + (this.UseGravity ? "on" : "off")
               + "\nLocal input "
               + (this._InputCtrl.localInput ? "on" : "off");
        if (this.ConstrainForceX) {
          info += "\nX Movement constrained";
        }

        if (this.ConstrainForceY) {
          info += "\nY Movement constrained";
        }

        if (this.ConstrainForceZ) {
          info += "\nZ Movement constrained";
        }

        if (this.ConstrainTorqueX) {
          info += "\nX Rotation constrained";
        }

        if (this.ConstrainTorqueY) {
          info += "\nY Rotation constrained";
        }

        if (this.ConstrainTorqueZ) {
          info += "\nZ Rotation constrained";
        }

//			GUIStyle label = GUI.skin.label;
//			TextClipping clipping = label.clipping;
//			label.clipping = TextClipping.Overflow;
//			bool wrap = label.wordWrap;
//			label.wordWrap = false;
//			int fontSize = label.fontSize;
//			label.fontSize = (int) ( 22f * Screen.height / 1080 );

        size = label.CalcSize(new GUIContent(info));
        r = new Rect(10, 10, size.x + 10, size.y);
        GUI.Box(r, "");
        GUI.Box(r, "");
        r.x += 5;

        GUILayout.BeginArea(r);
        GUILayout.Label(info);
        GUILayout.EndArea();
      } // telemetry

      // show axis arrows
      if (this._DrawArrows) {
        var show_movement = this.ConstrainForceX
                            || this.ConstrainForceY
                            || this.ConstrainForceZ
                            || this._DrawArrowsAlways;
        var show_rotation = this.ConstrainTorqueX
                            || this.ConstrainTorqueY
                            || this.ConstrainTorqueZ
                            || this._DrawArrowsAlways;
        // x arrow
        var cam = Camera.main;
        var screen_ratio = 1f * Screen.height / 1080;
        var tex_size = new Vector2(48, 8) * screen_ratio;
        var tex_size2 = new Vector2(16, 16) * screen_ratio;
        var arrow_mag = tex_size.magnitude + 14;
        var pos = this.transform.position;
        Vector2 screen_pos = cam.WorldToScreenPoint(pos);
        screen_pos.y = Screen.height - screen_pos.y;
        Vector2 top = cam.WorldToScreenPoint(pos + this.Up * 0.5f);
        top.y = Screen.height - top.y;
        Vector2 tip = cam.WorldToScreenPoint(pos + this.XAxis * 0.75f);
        tip.y = Screen.height - tip.y;
        var to_tip = (tip - screen_pos).normalized;
        var tex_rect = new Rect(screen_pos - tex_size, tex_size * 2);
//			Rect texRect2 = new Rect ( screenPos + ( top - screenPos ).normalized * arrowMag - texSize2, texSize2 * 2 );
        var tex_rect2 = new Rect(screen_pos + to_tip * arrow_mag - tex_size2, tex_size2 * 2);
        var angle = Vector2.Angle(Vector2.right, to_tip);
        if (tip.y > screen_pos.y) {
          angle = -angle;
        }

        GUIUtility.RotateAroundPivot(-angle, screen_pos);
        GUI.color = this._AxisColors[0];
        if (show_movement && !this.ConstrainForceX) {
          GUI.DrawTexture(tex_rect, this._AxisArrows[0]);
        }

        GUIUtility.RotateAroundPivot(angle, screen_pos);
        if (show_rotation && !this.ConstrainTorqueX) {
          GUI.DrawTexture(tex_rect2, this._AxisArrows[1]);
        }
//			GUI.DrawTexture ( new Rect ( tip.x - 2, tip.y - 2, 4, 4 ), dot );

        // y arrow
        tip = cam.WorldToScreenPoint(pos + this.YAxis * 0.75f);
        tip.y = Screen.height - tip.y;
        to_tip = (tip - screen_pos).normalized;
        angle = Vector2.Angle(Vector2.right, to_tip);
        if (tip.y > screen_pos.y) {
          angle = -angle;
        }

        GUIUtility.RotateAroundPivot(-angle, screen_pos);
        GUI.color = this._AxisColors[1];
        if (show_movement && !this.ConstrainForceY) {
          GUI.DrawTexture(tex_rect, this._AxisArrows[0]);
        }

        GUIUtility.RotateAroundPivot(angle, screen_pos);
        tex_rect2.position = screen_pos + to_tip * arrow_mag - tex_size2;
        if (show_rotation && !this.ConstrainTorqueY) {
          GUI.DrawTexture(tex_rect2, this._AxisArrows[1]);
        }
//			GUI.DrawTexture ( new Rect ( tip.x - 2, tip.y - 2, 4, 4 ), dot );

        // z arrow
        tip = cam.WorldToScreenPoint(pos + this.Up * 0.5f);
        tip.y = Screen.height - tip.y;
        to_tip = (tip - screen_pos).normalized;
        angle = Vector2.Angle(Vector2.right, to_tip);
        if (tip.y > screen_pos.y) {
          angle = -angle;
        }

        GUIUtility.RotateAroundPivot(-angle, screen_pos);
        GUI.color = this._AxisColors[2];
        if (show_movement && !this.ConstrainForceZ) {
          GUI.DrawTexture(tex_rect, this._AxisArrows[0]);
        }

        GUIUtility.RotateAroundPivot(angle, screen_pos);
        tex_rect2.position = screen_pos + to_tip * arrow_mag - tex_size2;
        if (show_rotation && !this.ConstrainTorqueZ) {
          GUI.DrawTexture(tex_rect2, this._AxisArrows[1]);
        }

//			GUI.DrawTexture ( new Rect ( tip.x - 2, tip.y - 2, 4, 4 ), dot );
//			GUI.color = Color.black;
//			GUI.DrawTexture ( new Rect ( screenPos.x - 2, screenPos.y - 2, 4, 4 ), dot );
      } // axis arrows

      GUI.color = Color.white;

      // show controls legend
      if (this._ShowLegend) {
        info = @"F10: Legend on/off
H: Control on/off
WSAD/Arrows: Move around
Space/C: Thrust up/down
Q/E: Turn around
G: Reset Quad orientation
Scroll wheel: zoom in/out
RMB (drag): Rotate camera
RMB: Reset camera
MMB: Toggle patrol/follow
F5: Cycle quality settings
/: Path display on/off
Esc: Reload menu
Ctrl-Q: Quit";

        label.alignment = TextAnchor.MiddleLeft;
        size = label.CalcSize(new GUIContent(info));
        r = new Rect(Screen.width - size.x - 20, 150, size.x + 10, size.y);
        GUI.Box(r, "");
        GUI.Box(r, "");
        r.x += 5;

        GUILayout.BeginArea(r);
        GUILayout.Label(info);
        GUILayout.EndArea();
      } else {
        label.alignment = TextAnchor.MiddleLeft;
        info = "F10: Legend on/off";

        size = label.CalcSize(new GUIContent(info));
        r = new Rect(Screen.width - size.x - 20, 150, size.x + 10, size.y);
        GUI.Box(r, "");
        GUI.Box(r, "");
        r.x += 5;

        GUILayout.BeginArea(r);
        GUILayout.Label(info);
        GUILayout.EndArea();
      }

      label.clipping = clipping;
      label.wordWrap = wrap;
      label.fontSize = font_size;
    }
    */

    Vector3 FixEuler(Vector3 euler) {
      euler.x = this.FixAngle(euler.x);
      euler.y = this.FixAngle(euler.y);
      euler.z = this.FixAngle(euler.z);
      return euler;
    }

    float FixAngle(float angle) {
      if (angle > 180f) {
        angle -= 360f;
      }

      if (angle < -180f) {
        angle += 360f;
      }

      return angle;
    }

    public void TriggerReset() { this._reset_flag = true; }

    public void ResetOrientation() {
      Debug.Log("reset");
      this.transform.rotation = Quaternion.identity;
      this._force = Vector3.zero;
      this._torque = Vector3.zero;
      this._rb.velocity = Vector3.zero;
      this._rb.angularVelocity = Vector3.zero;
      this.LinearAcceleration = Vector3.zero;
      this.LinearVelocity = Vector3.zero;
      this.AngularVelocity = Vector3.zero;
      this._rb.isKinematic = true;
      this._rb.isKinematic = false;
    }

    void CheckSetPose() {
      if (this._set_pose_flag) {
        Debug.Log("setpose");
        this.transform.position = this._pose_position;
        this.transform.rotation = this._pose_orientation;
        this._force = Vector3.zero;
        this._torque = Vector3.zero;
        this._rb.velocity = Vector3.zero;
        this._rb.angularVelocity = Vector3.zero;
        this.LinearAcceleration = Vector3.zero;
        this._set_pose_flag = false;
      }
    }

    public void SetPositionAndOrientation(
        Vector3 pos,
        Quaternion orientation,
        bool convert_from_ros = false) {
      this._set_pose_flag = true;
      if (convert_from_ros) {
        this._pose_position = pos;
        this._pose_orientation = orientation;
      } else {
        this._pose_position = pos;
        this._pose_orientation = orientation;
      }
    }


    void CheckConstraints() {
      var c = RigidbodyConstraints.None;
      if (this.ConstrainForceX) {
        c |= RigidbodyConstraints.FreezePositionZ;
      }

      if (this.ConstrainForceY) {
        c |= RigidbodyConstraints.FreezePositionX;
      }

      if (this.ConstrainForceZ) {
        c |= RigidbodyConstraints.FreezePositionY;
      }

      if (this.ConstrainTorqueX) {
        c |= RigidbodyConstraints.FreezeRotationZ;
      }

      if (this.ConstrainTorqueY) {
        c |= RigidbodyConstraints.FreezeRotationX;
      }

      if (this.ConstrainTorqueZ) {
        c |= RigidbodyConstraints.FreezeRotationY;
      }

      this._rb.constraints = c;
    }

    public void UpdateConstraints() {
      this.ConstrainForceX = (this._rb.constraints & RigidbodyConstraints.FreezePositionZ) != 0;
      this.ConstrainForceY = (this._rb.constraints & RigidbodyConstraints.FreezePositionX) != 0;
      this.ConstrainForceZ = (this._rb.constraints & RigidbodyConstraints.FreezePositionY) != 0;
      this.ConstrainTorqueX = (this._rb.constraints & RigidbodyConstraints.FreezeRotationZ) != 0;
      this.ConstrainTorqueY = (this._rb.constraints & RigidbodyConstraints.FreezeRotationX) != 0;
      this.ConstrainTorqueZ = (this._rb.constraints & RigidbodyConstraints.FreezeRotationY) != 0;
    }
  }
}
