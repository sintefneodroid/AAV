using UnityEngine;

namespace AAV.Drone2.Scripts {
    enum CarDriveType {
        Front_wheel_drive_,
        Rear_wheel_drive_,
        Four_wheel_drive_
    }

    enum SpeedType {
        Mph_,
        Kph_
    }

    public class CarController : MonoBehaviour {
        [SerializeField] CarDriveType m_CarDriveType = CarDriveType.Four_wheel_drive_;
        [SerializeField] WheelCollider[] m_WheelColliders = new WheelCollider[4];
        [SerializeField] GameObject[] m_WheelMeshes = new GameObject[4];
        [SerializeField] Vector3 m_CentreOfMassOffset;
        [SerializeField] float m_MaximumSteerAngle;
        [Range(0, 1)] [SerializeField] float m_SteerHelper; // 0 is raw physics , 1 the car will grip in the direction it is facing
        [Range(0, 1)] [SerializeField] float m_TractionControl; // 0 is no traction control, 1 is full interference
        [SerializeField] float m_FullTorqueOverAllWheels;
        [SerializeField] float m_ReverseTorque;
        [SerializeField] float m_MaxHandbrakeTorque;
        [SerializeField] float m_Downforce = 100f;
        [SerializeField] float m_Topspeed = 200;
        [SerializeField] int NoOfGears = 5;
        [SerializeField] float m_RevRangeBoundary = 1f;
        [SerializeField] float m_SlipLimit;
        [SerializeField] float m_BrakeTorque;

        Quaternion[] _m_wheel_mesh_local_rotations;
        Vector3 _m_prevpos, _m_pos;
        float _m_steer_angle;
        int _m_gear_num;
        float _m_gear_factor;
        float _m_old_rotation;
        float _m_current_torque;
        Rigidbody _m_rigidbody;
        const float _k_reversing_threshold = 0.01f;
        SpeedType _m_speed_type = SpeedType.Kph_;
        public const float _M_MaxRevs = 20000f;

        public bool Skidding { get; private set; }
        public float BrakeInput { get; private set; }
        public float CurrentSteerAngle { get { return this._m_steer_angle; } }
        public float CurrentSpeed { get { return this._m_rigidbody.velocity.magnitude * 2.23693629f; } }
        public float MaxSpeed { get { return this.m_Topspeed; } }
        public float Revs { get; private set; }
        public float AccelInput { get; private set; }

        // Use this for initialization
        void Start() {
            this._m_wheel_mesh_local_rotations = new Quaternion[4];
            for (int i = 0; i < 4; i++) {
                this._m_wheel_mesh_local_rotations[i] = this.m_WheelMeshes[i].transform.localRotation;
            }
            this.m_WheelColliders[0].attachedRigidbody.centerOfMass = this.m_CentreOfMassOffset;

            this.m_MaxHandbrakeTorque = float.MaxValue;

            this._m_rigidbody = this.GetComponent<Rigidbody>();
            this._m_current_torque = this.m_FullTorqueOverAllWheels - (this.m_TractionControl * this.m_FullTorqueOverAllWheels);
        }

        public void Move(float steering, float accel, float footbrake, float handbrake) {
            for (int i = 0; i < 4; i++) {
                Quaternion quat;
                Vector3 position;
                this.m_WheelColliders[i].GetWorldPose(out position, out quat);
                this.m_WheelMeshes[i].transform.position = position;
                this.m_WheelMeshes[i].transform.rotation = quat;
            }

            //clamp input values
            steering = Mathf.Clamp(steering, -1, 1);
            this.AccelInput = accel = Mathf.Clamp(accel, 0, 1);
            this.BrakeInput = footbrake = -1 * Mathf.Clamp(footbrake, -1, 0);
            handbrake = Mathf.Clamp(handbrake, 0, 1);

            //Set the steer on the front wheels.
            //Assuming that wheels 0 and 1 are the front wheels.
            this._m_steer_angle = steering * this.m_MaximumSteerAngle;
            this.m_WheelColliders[0].steerAngle = this._m_steer_angle;
            this.m_WheelColliders[1].steerAngle = this._m_steer_angle;

            this.SteerHelper();
            this.ApplyDrive(accel, footbrake);
            this.CapSpeed();

            //Set the handbrake.
            //Assuming that wheels 2 and 3 are the rear wheels.
            if (handbrake > 0f) {
                var hb_torque = handbrake * this.m_MaxHandbrakeTorque;
                this.m_WheelColliders[2].brakeTorque = hb_torque;
                this.m_WheelColliders[3].brakeTorque = hb_torque;
            }

            this.CalculateRevs();
            this.GearChanging();

            this.AddDownForce();
            this.TractionControl();
        }

        public void UpdateCarData() {
            var speed = (int)this.CurrentSpeed;
            var gear = this._m_gear_num;
            var engine_max_rotation_speed = _M_MaxRevs;
            var engine_rotation_speed = this.Revs;
        }

        void GearChanging() {
            float f = Mathf.Abs(this.CurrentSpeed / this.MaxSpeed);
            float upgearlimit = (1 / (float)this.NoOfGears) * (this._m_gear_num + 1);
            float downgearlimit = (1 / (float)this.NoOfGears) * this._m_gear_num;

            if (this._m_gear_num > 0 && f < downgearlimit) {
                this._m_gear_num--;
            }

            if (f > upgearlimit && (this._m_gear_num < (this.NoOfGears - 1))) {
                this._m_gear_num++;
            }
        }

        // simple function to add a curved bias towards 1 for a value in the 0-1 range
        static float CurveFactor(float factor) {
            return 1 - (1 - factor) * (1 - factor);
        }

        // unclamped version of Lerp, to allow value to exceed the from-to range
        static float ULerp(float from, float to, float value) {
            return (1.0f - value) * from + value * to;
        }

        void CalculateGearFactor() {
            float f = (1 / (float)this.NoOfGears);
            // gear factor is a normalized representation of the current speed within the current gear's range of speeds.
            // We smooth towards the 'target' gear factor, so that revs don't instantly snap up or down when changing gear.
            var target_gear_factor = Mathf.InverseLerp(f * this._m_gear_num, f * (this._m_gear_num + 1), Mathf.Abs(this.CurrentSpeed / this.MaxSpeed));
            this._m_gear_factor = Mathf.Lerp(this._m_gear_factor, target_gear_factor, Time.deltaTime * 5f);
        }

        void CalculateRevs() {
            // calculate engine revs (for display / sound)
            // (this is done in retrospect - revs are not used in force/power calculations)
            this.CalculateGearFactor();
            var gear_num_factor = this._m_gear_num / (float)this.NoOfGears;
            var revs_range_min = ULerp(0f, this.m_RevRangeBoundary, CurveFactor(gear_num_factor));
            var revs_range_max = ULerp(this.m_RevRangeBoundary, 1f, gear_num_factor);
            this.Revs = ULerp(revs_range_min, revs_range_max, this._m_gear_factor);
        }

        void CapSpeed() {
            float speed = this._m_rigidbody.velocity.magnitude;
            switch (this._m_speed_type) {
                case SpeedType.Mph_:

                    speed *= 2.23693629f;
                    if (speed > this.m_Topspeed)
                        this._m_rigidbody.velocity = (this.m_Topspeed / 2.23693629f) * this._m_rigidbody.velocity.normalized;
                    break;

                case SpeedType.Kph_:
                    speed *= 3.6f;
                    if (speed > this.m_Topspeed)
                        this._m_rigidbody.velocity = (this.m_Topspeed / 3.6f) * this._m_rigidbody.velocity.normalized;
                    break;
            }
        }

        void ApplyDrive(float accel, float footbrake) {
            float thrust_torque;
            switch (this.m_CarDriveType) {
                case CarDriveType.Four_wheel_drive_:
                    thrust_torque = accel * (this._m_current_torque / 4f);
                    for (int i = 0; i < 4; i++) {
                        this.m_WheelColliders[i].motorTorque = thrust_torque;
                    }
                    break;

                case CarDriveType.Front_wheel_drive_:
                    thrust_torque = accel * (this._m_current_torque / 2f);
                    this.m_WheelColliders[0].motorTorque = this.m_WheelColliders[1].motorTorque = thrust_torque;
                    break;

                case CarDriveType.Rear_wheel_drive_:
                    thrust_torque = accel * (this._m_current_torque / 2f);
                    this.m_WheelColliders[2].motorTorque = this.m_WheelColliders[3].motorTorque = thrust_torque;
                    break;
            }

            for (int i = 0; i < 4; i++) {
                if (this.CurrentSpeed > 5 && Vector3.Angle(this.transform.forward, this._m_rigidbody.velocity) < 50f) {
                    this.m_WheelColliders[i].brakeTorque = this.m_BrakeTorque * footbrake;
                } else if (footbrake > 0) {
                    this.m_WheelColliders[i].brakeTorque = 0f;
                    this.m_WheelColliders[i].motorTorque = -this.m_ReverseTorque * footbrake;
                }
            }
        }

        void SteerHelper() {
            for (int i = 0; i < 4; i++) {
                WheelHit wheelhit;
                this.m_WheelColliders[i].GetGroundHit(out wheelhit);
                if (wheelhit.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }

            // this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
            if (Mathf.Abs(this._m_old_rotation - this.transform.eulerAngles.y) < 10f) {
                var turnadjust = (this.transform.eulerAngles.y - this._m_old_rotation) * this.m_SteerHelper;
                Quaternion vel_rotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
                this._m_rigidbody.velocity = vel_rotation * this._m_rigidbody.velocity;
            }
            this._m_old_rotation = this.transform.eulerAngles.y;
        }

        // this is used to add more grip in relation to speed
        void AddDownForce() {
            this.m_WheelColliders[0].attachedRigidbody.AddForce(-this.transform.up * this.m_Downforce *
                                                         this.m_WheelColliders[0].attachedRigidbody.velocity.magnitude);
        }

        // crude traction control that reduces the power to wheel if the car is wheel spinning too much
        void TractionControl() {
            WheelHit wheel_hit;
            switch (this.m_CarDriveType) {
                case CarDriveType.Four_wheel_drive_:
                    // loop through all wheels
                    for (int i = 0; i < 4; i++) {
                        this.m_WheelColliders[i].GetGroundHit(out wheel_hit);

                        this.AdjustTorque(wheel_hit.forwardSlip);
                    }
                    break;

                case CarDriveType.Rear_wheel_drive_:
                    this.m_WheelColliders[2].GetGroundHit(out wheel_hit);
                    this.AdjustTorque(wheel_hit.forwardSlip);

                    this.m_WheelColliders[3].GetGroundHit(out wheel_hit);
                    this.AdjustTorque(wheel_hit.forwardSlip);
                    break;

                case CarDriveType.Front_wheel_drive_:
                    this.m_WheelColliders[0].GetGroundHit(out wheel_hit);
                    this.AdjustTorque(wheel_hit.forwardSlip);

                    this.m_WheelColliders[1].GetGroundHit(out wheel_hit);
                    this.AdjustTorque(wheel_hit.forwardSlip);
                    break;
            }
        }

        void AdjustTorque(float forward_slip) {
            if (forward_slip >= this.m_SlipLimit && this._m_current_torque >= 0) {
                this._m_current_torque -= 10 * this.m_TractionControl;
            } else {
                this._m_current_torque += 10 * this.m_TractionControl;
                if (this._m_current_torque > this.m_FullTorqueOverAllWheels) {
                    this._m_current_torque = this.m_FullTorqueOverAllWheels;
                }
            }
        }
    }
}
