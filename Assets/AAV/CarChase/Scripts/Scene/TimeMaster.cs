using UnityEngine;
using UnityEngine.Audio;

namespace AAV.CarChase.Scripts.Scene {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Scene Controllers/Time Master", 1)]

  //Class for managing time
  public class TimeMaster : MonoBehaviour {
    float initialFixedTime; //Intial Time.fixedDeltaTime

    [Tooltip("Master audio mixer")] public AudioMixer masterMixer;
    public bool destroyOnLoad;

    public static float
        fixedTimeFactor; //Multiplier for certain variables to change consistently over varying time steps

    public static float inverseFixedTimeFactor;

    void Awake() {
      this.initialFixedTime = Time.fixedDeltaTime;

      if (!this.destroyOnLoad) {
        DontDestroyOnLoad(this.gameObject);
      }
    }

    void Update() {
      //Set the pitch of all audio to the time scale
      if (this.masterMixer) {
        this.masterMixer.SetFloat("MasterPitch", Time.timeScale);
      }
    }

    void FixedUpdate() {
      //Set the fixed update rate based on time scale
      Time.fixedDeltaTime = Time.timeScale * this.initialFixedTime;
      fixedTimeFactor = 0.01f / this.initialFixedTime;
      inverseFixedTimeFactor = 1 / fixedTimeFactor;
    }
  }
}
