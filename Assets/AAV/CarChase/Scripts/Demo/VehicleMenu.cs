using AAV.CarChase.Scripts.AI;
using AAV.CarChase.Scripts.Camera;
using AAV.CarChase.Scripts.Drivetrain;
using AAV.CarChase.Scripts.Scene;
using AAV.CarChase.Scripts.Stunt;
using AAV.CarChase.Scripts.Suspension;
using AAV.CarChase.Scripts.Vehicle_Control;
using UnityEngine;
using UnityEngine.UI;

namespace AAV.CarChase.Scripts.Demo {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Demo Scripts/Vehicle Menu", 0)]

  //Class for the menu and HUD in the demo
  public class VehicleMenu : MonoBehaviour {
    public CameraControl cam;
    public Vector3 spawnPoint;
    public Vector3 spawnRot;
    public GameObject[] vehicles;
    public GameObject chaseVehicle;
    public GameObject chaseVehicleDamage;
    float chaseCarSpawnTime;
    GameObject newVehicle;
    bool autoShift;
    bool assist;
    bool stuntMode;
    public Toggle autoShiftToggle;
    public Toggle assistToggle;
    public Toggle stuntToggle;
    public Text speedText;
    public Text gearText;
    public Slider rpmMeter;
    public Slider boostMeter;
    public Text propertySetterText;
    public Text stuntText;
    public Text scoreText;
    public Toggle camToggle;
    VehicleParent vp;
    Motor engine;
    Transmission trans;
    GearboxTransmission gearbox;
    ContinuousTransmission varTrans;
    StuntDetect stunter;
    float stuntEndTime = -1;
    PropertyToggleSetter propertySetter;

    void Update() {
      this.autoShift = this.autoShiftToggle.isOn;
      this.assist = this.assistToggle.isOn;
      this.stuntMode = this.stuntToggle.isOn;
      this.cam.stayFlat = this.camToggle.isOn;
      this.chaseCarSpawnTime = Mathf.Max(0, this.chaseCarSpawnTime - Time.deltaTime);

      if (this.vp) {
        this.speedText.text = (this.vp.velMag * 2.23694f).ToString("0") + " MPH";

        if (this.trans) {
          if (this.gearbox) {
            this.gearText.text = "Gear: "
                                 + (this.gearbox.currentGear == 0
                                        ? "R"
                                        : (this.gearbox.currentGear == 1
                                               ? "N"
                                               : (this.gearbox.currentGear - 1).ToString()));
          } else if (this.varTrans) {
            this.gearText.text = "Ratio: " + this.varTrans.currentRatio.ToString("0.00");
          }
        }

        if (this.engine) {
          this.rpmMeter.value = this.engine.targetPitch;

          if (this.engine.maxBoost > 0) {
            this.boostMeter.value = this.engine.boost / this.engine.maxBoost;
          }
        }

        if (this.stuntMode && this.stunter) {
          this.stuntEndTime = string.IsNullOrEmpty(this.stunter.stuntString)
                                  ? Mathf.Max(0, this.stuntEndTime - Time.deltaTime)
                                  : 2;

          if (this.stuntEndTime == 0) {
            this.stuntText.text = "";
          } else if (!string.IsNullOrEmpty(this.stunter.stuntString)) {
            this.stuntText.text = this.stunter.stuntString;
          }

          this.scoreText.text = "Score: " + this.stunter.score.ToString("n0");
        }

        if (this.propertySetter) {
          this.propertySetterText.text = this.propertySetter.currentPreset == 0
                                             ? "Normal Steering"
                                             : (this.propertySetter.currentPreset == 1
                                                    ? "Skid Steering"
                                                    : "Crab Steering");
        }
      }
    }

    public void SpawnVehicle(int vehicle) {
      this.newVehicle = Instantiate(this.vehicles[vehicle],
                                    this.spawnPoint,
                                    Quaternion.LookRotation(this.spawnRot, GlobalControl.worldUpDir));
      this.cam.target = this.newVehicle.transform;
      this.cam.Initialize();
      this.vp = this.newVehicle.GetComponent<VehicleParent>();

      this.trans = this.newVehicle.GetComponentInChildren<Transmission>();
      if (this.trans) {
        this.trans.automatic = this.autoShift;
        this.newVehicle.GetComponent<VehicleParent>().brakeIsReverse = this.autoShift;

        if (this.trans is GearboxTransmission) {
          this.gearbox = this.trans as GearboxTransmission;
        } else if (this.trans is ContinuousTransmission) {
          this.varTrans = this.trans as ContinuousTransmission;

          if (!this.autoShift) {
            this.vp.brakeIsReverse = true;
          }
        }
      }

      if (this.newVehicle.GetComponent<VehicleAssist>()) {
        this.newVehicle.GetComponent<VehicleAssist>().enabled = this.assist;
      }

      if (this.newVehicle.GetComponent<FlipControl>() && this.newVehicle.GetComponent<StuntDetect>()) {
        this.newVehicle.GetComponent<FlipControl>().flipPower =
            this.stuntMode && this.assist ? new Vector3(10, 10, -10) : Vector3.zero;
        this.newVehicle.GetComponent<FlipControl>().rotationCorrection =
            this.stuntMode ? Vector3.zero : (this.assist ? new Vector3(5, 1, 10) : Vector3.zero);
        this.newVehicle.GetComponent<FlipControl>().stopFlip = this.assist;
        this.stunter = this.newVehicle.GetComponent<StuntDetect>();
      }

      this.engine = this.newVehicle.GetComponentInChildren<Motor>();
      this.propertySetter = this.newVehicle.GetComponent<PropertyToggleSetter>();

      this.stuntText.gameObject.SetActive(this.stuntMode);
      this.scoreText.gameObject.SetActive(this.stuntMode);
    }

    public void SpawnChaseVehicle() {
      if (this.chaseCarSpawnTime == 0) {
        this.chaseCarSpawnTime = 1;
        var chaseCar = Instantiate(this.chaseVehicle,
                                   this.spawnPoint,
                                   Quaternion.LookRotation(this.spawnRot, GlobalControl.worldUpDir));
        chaseCar.GetComponent<FollowAI>().target = this.newVehicle.transform;
      }
    }

    public void SpawnChaseVehicleDamage() {
      if (this.chaseCarSpawnTime == 0) {
        this.chaseCarSpawnTime = 1;
        var chaseCar = Instantiate(this.chaseVehicleDamage,
                                   this.spawnPoint,
                                   Quaternion.LookRotation(this.spawnRot, GlobalControl.worldUpDir));
        chaseCar.GetComponent<FollowAI>().target = this.newVehicle.transform;
      }
    }
  }
}
