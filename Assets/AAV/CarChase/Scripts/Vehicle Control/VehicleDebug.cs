using System.Collections;
using AAV.CarChase.Scripts.Damage;
using AAV.CarChase.Scripts.Scene;
using UnityEngine;

namespace AAV.CarChase.Scripts.Vehicle_Control {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Vehicle Controllers/Vehicle Debug", 3)]

  //Class for easily resetting vehicles
  public class VehicleDebug : MonoBehaviour {
    public Vector3 spawnPos;
    public Vector3 spawnRot;

    [Tooltip("Y position below which the vehicle will be reset")]
    public float fallLimit = -10;

    void Update() {
      if (UnityEngine.Input.GetButtonDown("Reset Rotation")) {
        this.StartCoroutine(this.ResetRotation());
      }

      if (UnityEngine.Input.GetButtonDown("Reset Position") || this.transform.position.y < this.fallLimit) {
        this.StartCoroutine(this.ResetPosition());
      }
    }

    IEnumerator ResetRotation() {
      if (this.GetComponent<VehicleDamage>()) {
        this.GetComponent<VehicleDamage>().Repair();
      }

      yield return new WaitForFixedUpdate();
      this.transform.eulerAngles = new Vector3(0, this.transform.eulerAngles.y, 0);
      this.transform.Translate(Vector3.up, Space.World);
      this.GetComponent<Rigidbody>().velocity = Vector3.zero;
      this.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    IEnumerator ResetPosition() {
      if (this.GetComponent<VehicleDamage>()) {
        this.GetComponent<VehicleDamage>().Repair();
      }

      this.transform.position = this.spawnPos;
      yield return new WaitForFixedUpdate();
      this.transform.rotation = Quaternion.LookRotation(this.spawnRot, GlobalControl.worldUpDir);
      this.GetComponent<Rigidbody>().velocity = Vector3.zero;
      this.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }
  }
}
