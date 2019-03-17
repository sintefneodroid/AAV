using System;
using UnityEngine;

namespace AAV.CarChase.Scripts.Damage {
  [RequireComponent(typeof(Renderer))]
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/Damage/Shatter Part", 2)]

  //Class for parts that shatter
  public class ShatterPart : MonoBehaviour {
    [NonSerialized] public Renderer rend;
    [NonSerialized] public bool shattered;
    public float breakForce = 5;

    [Tooltip("Transform used for maintaining seams when deformed after shattering")]
    public Transform seamKeeper;

    [NonSerialized] public Material initialMat;
    public Material brokenMaterial;
    public ParticleSystem shatterParticles;
    public AudioSource shatterSnd;

    void Start() {
      this.rend = this.GetComponent<Renderer>();
      if (this.rend) {
        this.initialMat = this.rend.sharedMaterial;
      }
    }

    public void Shatter() {
      if (!this.shattered) {
        this.shattered = true;

        if (this.shatterParticles) {
          this.shatterParticles.Play();
        }

        if (this.brokenMaterial) {
          this.rend.sharedMaterial = this.brokenMaterial;
        } else {
          this.rend.enabled = false;
        }

        if (this.shatterSnd) {
          this.shatterSnd.Play();
        }
      }
    }
  }
}
