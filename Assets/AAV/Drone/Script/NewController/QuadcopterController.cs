namespace AAV.Drone.Script.NewController
{
  using UnityEngine;
  using System.Collections;

  public class QuadcopterController : MonoBehaviour
  {
    //The propellers
    public GameObject propellerFr;
    public GameObject propellerFl;
    public GameObject propellerBl;
    public GameObject propellerBr;

    //Quadcopter parameters
    [Header("Internal")] public float maxPropellerForce; //100
    public float maxTorque; //1
    public float throttle;

    public float moveFactor; //5

    //PID
    public Vector3 pidPitchGains; //(2, 3, 2)
    public Vector3 pidRollGains; //(2, 0.2, 0.5)
    public Vector3 pidYawGains; //(1, 0, 0)

    //External parameters
    [Header("External")] public float windForce;

    //0 -> 360
    public float forceDir;


    Rigidbody _quadcopterRb;


    //The PID controllers
    PIDController _pidPitch;
    PIDController _pidRoll;
    PIDController _pidYaw;

    //Movement factors
    float _moveForwardBack;
    float _moveLeftRight;
    float _yawDir;

    void Start()
    {
      this._quadcopterRb = gameObject.GetComponent<Rigidbody>();

      this._pidPitch = new PIDController();
      this._pidRoll = new PIDController();
      this._pidYaw = new PIDController();
    }

    void FixedUpdate()
    {
      AddControls();

      AddMotorForce();

      AddExternalForces();
    }

    void AddControls()
    {
      //Change throttle to move up or down
      if (Input.GetKey(KeyCode.UpArrow))
      {
        throttle += 5f;
      }

      if (Input.GetKey(KeyCode.DownArrow))
      {
        throttle -= 5f;
      }

      throttle = Mathf.Clamp(throttle, 0f, 200f);


      //Steering
      //Move forward or reverse
      this._moveForwardBack = 0f;

      if (Input.GetKey(KeyCode.W))
      {
        this._moveForwardBack = 1f;
      }

      if (Input.GetKey(KeyCode.S))
      {
        this._moveForwardBack = -1f;
      }

      //Move left or right
      this._moveLeftRight = 0f;

      if (Input.GetKey(KeyCode.A))
      {
        this._moveLeftRight = -1f;
      }

      if (Input.GetKey(KeyCode.D))
      {
        this._moveLeftRight = 1f;
      }

      //Rotate around the axis
      this._yawDir = 0f;

      if (Input.GetKey(KeyCode.LeftArrow))
      {
        this._yawDir = -1f;
      }

      if (Input.GetKey(KeyCode.RightArrow))
      {
        this._yawDir = 1f;
      }
    }

    void AddMotorForce()
    {
      //Calculate the errors so we can use a PID controller to stabilize
      //Assume no error is if 0 degrees

      //Pitch
      //Returns positive if pitching forward
      float pitchError = GetPitchError();

      //Roll
      //Returns positive if rolling left
      float rollError = GetRollError() * -1f;

      //Adapt the PID variables to the throttle
      Vector3 pidPitchGainsAdapted = throttle > 100f ? this.pidPitchGains * 2f : this.pidPitchGains;

      //Get the output from the PID controllers
      float pidPitchOutput = this._pidPitch.GetFactorFromPIDController(pidPitchGainsAdapted, pitchError);
      float pidRollOutput = this._pidRoll.GetFactorFromPIDController(this.pidRollGains, rollError);

      //Calculate the propeller forces
      //FR
      float propellerForceFr = throttle + (pidPitchOutput + pidRollOutput);

      //Add steering
      propellerForceFr -= this._moveForwardBack * throttle * moveFactor;
      propellerForceFr -= this._moveLeftRight * throttle;


      //FL
      float propellerForceFl = throttle + (pidPitchOutput - pidRollOutput);

      propellerForceFl -= this._moveForwardBack * throttle * moveFactor;
      propellerForceFl += this._moveLeftRight * throttle;


      //BR
      float propellerForceBr = throttle + (-pidPitchOutput + pidRollOutput);

      propellerForceBr += this._moveForwardBack * throttle * moveFactor;
      propellerForceBr -= this._moveLeftRight * throttle;


      //BL
      float propellerForceBl = throttle + (-pidPitchOutput - pidRollOutput);

      propellerForceBl += this._moveForwardBack * throttle * moveFactor;
      propellerForceBl += this._moveLeftRight * throttle;


      //Clamp
      propellerForceFr = Mathf.Clamp(propellerForceFr, 0f, maxPropellerForce);
      propellerForceFl = Mathf.Clamp(propellerForceFl, 0f, maxPropellerForce);
      propellerForceBr = Mathf.Clamp(propellerForceBr, 0f, maxPropellerForce);
      propellerForceBl = Mathf.Clamp(propellerForceBl, 0f, maxPropellerForce);

      //Add the force to the propellers
      AddForceToPropeller(this.propellerFr, propellerForceFr);
      AddForceToPropeller(this.propellerFl, propellerForceFl);
      AddForceToPropeller(this.propellerBr, propellerForceBr);
      AddForceToPropeller(this.propellerBl, propellerForceBl);

      //Yaw
      //Minimize the yaw error (which is already signed):
      float yawError = this._quadcopterRb.angularVelocity.y;

      float pidYawOutput = this._pidYaw.GetFactorFromPIDController(this.pidYawGains, yawError);

      //First we need to add a force (if any)
      this._quadcopterRb.AddTorque(transform.up * this._yawDir * maxTorque * throttle);

      //Then we need to minimize the error
      this._quadcopterRb.AddTorque(transform.up * throttle * pidYawOutput * -1f);
    }

    void AddForceToPropeller(GameObject propellerObj, float propellerForce)
    {
      Vector3 propellerUp = propellerObj.transform.up;

      Vector3 propellerPos = propellerObj.transform.position;

      this._quadcopterRb.AddForceAtPosition(propellerUp * propellerForce, propellerPos);

      //Debug
      //Debug.DrawRay(propellerPos, propellerUp * 1f, Color.red);
    }

    //Pitch is rotation around x-axis
    //Returns positive if pitching forward
    float GetPitchError()
    {
      float xAngle = transform.eulerAngles.x;

      //Make sure the angle is between 0 and 360
      xAngle = WrapAngle(xAngle);

      //This angle going from 0 -> 360 when pitching forward
      //So if angle is > 180 then it should move from 0 to 180 if pitching back
      if (xAngle > 180f && xAngle < 360f)
      {
        xAngle = 360f - xAngle;

        //-1 so we know if we are pitching back or forward
        xAngle *= -1f;
      }

      return xAngle;
    }

    //Roll is rotation around z-axis
    //Returns positive if rolling left
    float GetRollError()
    {
      float zAngle = transform.eulerAngles.z;

      //Make sure the angle is between 0 and 360
      zAngle = WrapAngle(zAngle);

      //This angle going from 0-> 360 when rolling left
      //So if angle is > 180 then it should move from 0 to 180 if rolling right
      if (zAngle > 180f && zAngle < 360f)
      {
        zAngle = 360f - zAngle;

        //-1 so we know if we are rolling left or right
        zAngle *= -1f;
      }

      return zAngle;
    }

    //Wrap between 0 and 360 degrees
    float WrapAngle(float inputAngle)
    {
      //The inner % 360 restricts everything to +/- 360
      //+360 moves negative values to the positive range, and positive ones to > 360
      //the final % 360 caps everything to 0...360
      return ((inputAngle % 360f) + 360f) % 360f;
    }

    //Add external forces to the quadcopter, such as wind
    void AddExternalForces()
    {
      //Important to not use the quadcopters forward
      Vector3 windDir = -Vector3.forward;

      //Rotate it
      windDir = Quaternion.Euler(0, forceDir, 0) * windDir;

      this._quadcopterRb.AddForce(windDir * windForce);

      //Debug
      //Is showing in which direction the wind is coming from
      //center of quadcopter is where it ends and is blowing in the direction of the line
      Debug.DrawRay(transform.position, -windDir * 3f, Color.red);
    }
  }
}