using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AAV.CarChase.Scripts.Demo {
  [AddComponentMenu("RVP/Demo Scripts/Performance Stats", 1)]

  //Class for displaying the framerate
  public class PerformanceStats : MonoBehaviour {
    public Text fpsText;
    float fpsUpdateTime;
    int frames;

    void Update() {
      this.fpsUpdateTime = Mathf.Max(0, this.fpsUpdateTime - Time.deltaTime);

      if (this.fpsUpdateTime == 0) {
        this.fpsText.text = "FPS: " + this.frames;
        this.fpsUpdateTime = 1;
        this.frames = 0;
      } else {
        this.frames++;
      }
    }

    public void Restart() {
      SceneManager.LoadScene(SceneManager.GetActiveScene().name);
      Time.timeScale = 1;
    }
  }
}
